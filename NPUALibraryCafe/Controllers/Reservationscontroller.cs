using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPUALibraryCafe.DTOs.Reservations;
using System.Security.Claims;

namespace NPUALibraryCafe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationRepository _reservationRepository;

    public ReservationsController(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    private string GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? "";
    private string GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "";
    private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    [HttpGet("tables")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTableAvailability(
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] DateTimeOffset endTime)
    {
        var startUtc = startTime.UtcDateTime;
        var endUtc = endTime.UtcDateTime;

        var allTables = await _reservationRepository.GetAllTablesAsync();
        var reservedIds = await _reservationRepository.GetReservedTableIdsAsync(startUtc, endUtc);

        return Ok(allTables.Select(t => new TableAvailabilityDto
        {
            Id = t.Id,
            TableNumber = t.TableNumber,
            Capacity = t.Capacity,
            Available = !reservedIds.Contains(t.Id)
        }));
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReservations()
    {
        var email = GetUserEmail();
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var reservations = await _reservationRepository.GetByUserEmailAsync(email);
        return Ok(reservations.Select(r => new ReservationResponseDto
        {
            Id = r.Id,
            TableId = r.TableId,
            TableName = r.Table?.TableNumber ?? "",
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }));
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAllReservations()
    {
        if (GetUserRole() != "admin") return Forbid();

        var reservations = await _reservationRepository.GetAllAsync();
        return Ok(reservations.Select(r => new ReservationDetailDto
        {
            Id = r.Id,
            TableId = r.TableId,
            TableName = r.Table?.TableNumber ?? "",
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UserEmail = r.UserEmail,
            UserName = r.UserName
        }));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto)
    {
        System.Diagnostics.Debug.WriteLine($">>> Received StartTime: {dto.StartTime} | LocalDateTime: {dto.StartTime.LocalDateTime} | Offset: {dto.StartTime.Offset}");

        var email = GetUserEmail();
        var name = GetUserName();
        if (string.IsNullOrEmpty(email)) return Unauthorized();

        var startUtc = dto.StartTime.UtcDateTime;
        var endUtc = dto.EndTime.UtcDateTime;

        if (startUtc <= DateTime.UtcNow)
            return BadRequest(new { error = "Start time must be in the future" });
        if (endUtc <= startUtc)
            return BadRequest(new { error = "End time must be after start time" });
        

        var tables = await _reservationRepository.GetAllTablesAsync();
        var table = tables.FirstOrDefault(t => t.Id == dto.TableId);
        if (table == null) return BadRequest(new { error = "Table not found" });

        var conflict = await _reservationRepository.HasConflictAsync(dto.TableId, startUtc, endUtc);
        if (conflict)
            return BadRequest(new { error = "This table is already reserved for that time. Please choose another table or time." });

        var reservation = new Reservation
        {
            TableId = dto.TableId,
            UserEmail = email,
            UserName = name,
            StartTime = startUtc,
            EndTime = endUtc,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        table.IsReserved = true;
        await _reservationRepository.AddAsync(reservation);

        return Ok(new
        {
            message = "Table reserved successfully",
            reservationId = reservation.Id,
            tableId = dto.TableId,
            tableName = table.TableNumber,
            startTime = reservation.StartTime,
            endTime = reservation.EndTime
        });
    }

    [HttpPut("{id}/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmReservation(int id)
    {
        var email = GetUserEmail();
        var reservation = await _reservationRepository.GetByIdAsync(id);

        if (reservation == null || reservation.UserEmail != email)
            return NotFound(new { error = "Reservation not found" });
        if (reservation.Status == "Cancelled")
            return BadRequest(new { error = "Reservation is cancelled" });
        if (reservation.Status == "Expired")
            return BadRequest(new { error = "Reservation has expired" });

        reservation.Status = "Confirmed";
        await _reservationRepository.UpdateAsync(reservation);

        return Ok(new { message = "Reservation confirmed! See you there!" });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var email = GetUserEmail();
        var role = GetUserRole();
        var reservation = await _reservationRepository.GetByIdAsync(id);

        if (reservation == null ||
            (reservation.UserEmail != email && role != "admin"))
            return NotFound(new { error = "Reservation not found" });

        if (reservation.Status == "Cancelled")
            return BadRequest(new { error = "Already cancelled" });

        reservation.Status = "Cancelled";

        if (reservation.Table != null)
        {
            var hasOther = await _reservationRepository
                .HasOtherActiveReservationsAsync(reservation.TableId, reservation.Id);
            if (!hasOther) reservation.Table.IsReserved = false;
        }

        await _reservationRepository.UpdateAsync(reservation);
        return Ok(new { message = "Reservation cancelled" });
    }

    [HttpPost("check-reminders")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckAndSendReminders()
    {
        var now = DateTime.UtcNow;
        var upcoming = await _reservationRepository.GetUpcomingAsync(now, now.AddMinutes(15));
        var toExpire = await _reservationRepository.GetExpiredAsync(now);

        foreach (var res in toExpire)
        {
            res.Status = "Expired";
            if (res.Table != null)
            {
                var hasOther = await _reservationRepository
                    .HasOtherActiveReservationsAsync(res.TableId, res.Id);
                if (!hasOther) res.Table.IsReserved = false;
            }
            await _reservationRepository.UpdateAsync(res);
        }

        return Ok(new { remindersChecked = upcoming.Count(), expired = toExpire.Count() });
    }
}