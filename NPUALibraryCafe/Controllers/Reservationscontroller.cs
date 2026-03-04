using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.Models;
using System.Security.Claims;

namespace NPUALibraryCafe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public ReservationsController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private string GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        private string GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // GET /api/Reservations/tables?startTime=...&endTime=...
        [HttpGet("tables")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTableAvailability(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            var allTables = await _context.CafeTables.ToListAsync();

            var reservedTableIds = await _context.Reservations
                .Where(r =>
                    r.Status != "Cancelled" &&
                    r.Status != "Expired" &&
                    r.StartTime < endTime &&
                    r.EndTime > startTime)
                .Select(r => r.TableId)
                .ToListAsync();

            var result = allTables.Select(t => new
            {
                id = t.Id,
                tableNumber = t.TableNumber,
                capacity = t.Capacity,
                available = !reservedTableIds.Contains(t.Id)
            });

            return Ok(result);
        }

        // GET /api/Reservations/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyReservations()
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .Where(r => r.UserEmail == email)
                .OrderByDescending(r => r.StartTime)
                .Select(r => new
                {
                    id = r.Id,
                    tableId = r.TableId,
                    tableName = r.Table != null ? r.Table.TableNumber : "",
                    startTime = r.StartTime,
                    endTime = r.EndTime,
                    status = r.Status,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // GET /api/Reservations/all - Admin only
        [HttpGet("all")]
        [Authorize]
        public async Task<IActionResult> GetAllReservations()
        {
            if (GetUserRole() != "admin") return Forbid();

            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .OrderByDescending(r => r.StartTime)
                .Select(r => new
                {
                    id = r.Id,
                    userEmail = r.UserEmail,
                    userName = r.UserName,
                    tableNumber = r.Table != null ? r.Table.TableNumber : "",
                    startTime = r.StartTime,
                    endTime = r.EndTime,
                    status = r.Status,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // POST /api/Reservations
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto)
        {
            var email = GetUserEmail();
            var name = GetUserName();
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            if (dto.StartTime <= DateTime.Now)
                return BadRequest(new { error = "Start time must be in the future" });

            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new { error = "End time must be after start time" });

            var table = await _context.CafeTables.FindAsync(dto.TableId);
            if (table == null)
                return BadRequest(new { error = "Table not found" });

            // Check availability
            var conflict = await _context.Reservations
                .AnyAsync(r =>
                    r.TableId == dto.TableId &&
                    r.Status != "Cancelled" &&
                    r.Status != "Expired" &&
                    r.StartTime < dto.EndTime &&
                    r.EndTime > dto.StartTime);

            if (conflict)
                return BadRequest(new { error = "This table is already reserved for that time. Please choose another table or time." });

            var reservation = new Reservation
            {
                TableId = dto.TableId,
                UserEmail = email,
                UserName = name,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Reservations.Add(reservation);

            // Mark table as reserved
            table.IsReserved = true;

            await _context.SaveChangesAsync();
            
           

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

        // PUT /api/Reservations/{id}/confirm
        [HttpPut("{id}/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            var email = GetUserEmail();
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.UserEmail == email);

            if (reservation == null) return NotFound(new { error = "Reservation not found" });
            if (reservation.Status == "Cancelled") return BadRequest(new { error = "Reservation is cancelled" });
            if (reservation.Status == "Expired") return BadRequest(new { error = "Reservation has expired" });

            reservation.Status = "Confirmed";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reservation confirmed! See you there!" });
        }

        // DELETE /api/Reservations/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var email = GetUserEmail();
            var role = GetUserRole();

            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.Id == id &&
                    (r.UserEmail == email || role == "admin"));

            if (reservation == null) return NotFound(new { error = "Reservation not found" });
            if (reservation.Status == "Cancelled") return BadRequest(new { error = "Already cancelled" });

            reservation.Status = "Cancelled";

            // Free the table if no other active reservations
            if (reservation.Table != null)
            {
                var otherActive = await _context.Reservations
                    .AnyAsync(r => r.TableId == reservation.TableId &&
                        r.Id != reservation.Id &&
                        r.Status != "Cancelled" && r.Status != "Expired");
                if (!otherActive)
                    reservation.Table.IsReserved = false;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Reservation cancelled" });
        }

        // POST /api/Reservations/check-reminders
        [HttpPost("check-reminders")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAndSendReminders()
        {
            var now = DateTime.Now;
            var reminderWindow = now.AddMinutes(15);

            // Send 15-min reminders (find by user_email in notifications)
            var upcoming = await _context.Reservations
                .Include(r => r.Table)
                .Where(r =>
                    r.Status == "Active" &&
                    r.StartTime <= reminderWindow &&
                    r.StartTime > now)
                .ToListAsync();

            // Auto-expire ended reservations
            var toExpire = await _context.Reservations
                .Include(r => r.Table)
                .Where(r =>
                    (r.Status == "Active" || r.Status == "Confirmed") &&
                    r.EndTime <= now)
                .ToListAsync();

            foreach (var res in toExpire)
            {
                res.Status = "Expired";
                if (res.Table != null)
                {
                    var otherActive = await _context.Reservations
                        .AnyAsync(r => r.TableId == res.TableId &&
                            r.Id != res.Id &&
                            r.Status != "Cancelled" && r.Status != "Expired");
                    if (!otherActive)
                        res.Table.IsReserved = false;
                }
            }

            if (upcoming.Any() || toExpire.Any())
                await _context.SaveChangesAsync();

            return Ok(new { remindersChecked = upcoming.Count, expired = toExpire.Count });
        }
    }

    public class CreateReservationDto
    {
        public int TableId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Notes { get; set; }
    }
}