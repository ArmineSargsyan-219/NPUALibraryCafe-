using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public ReservationsController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst("userId")?.Value, out int id) ? id : 0;

        private string GetUserRole() =>
            User.FindFirst("role")?.Value ?? "";

        // GET /api/Reservations/available?date=2026-02-17&startTime=14:00&endTime=16:00
        // Check which seats are available for a given time slot
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSeats(
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            // Get all reserved seats during this time period
            var reservedSeats = await _context.Reservations
                .Where(r =>
                    r.Status != "Cancelled" &&
                    r.Status != "Expired" &&
                    r.Starttime < endTime &&
                    r.Endtime > startTime)
                .Include(r => r.Reservationseats)
                .SelectMany(r => r.Reservationseats.Select(s => s.Seatid))
                .ToListAsync();

            return Ok(new { reservedSeats });
        }

        // GET /api/Reservations/my - Get my reservations
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReservations()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var reservations = await _context.Reservations
                .Where(r => r.Userid == userId)
                .Include(r => r.Reservationseats)
                .OrderByDescending(r => r.Starttime)
                .Select(r => new
                {
                    id = r.Reservationid,
                    type = r.Reservationtype,
                    startTime = r.Starttime,
                    endTime = r.Endtime,
                    status = r.Status,
                    seats = r.Reservationseats.Select(s => s.Seatid).ToList(),
                    notes = r.Notes,
                    createdAt = r.Createdat
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // GET /api/Reservations/all - Admin only: Get all reservations
        [HttpGet("all")]
        public async Task<IActionResult> GetAllReservations()
        {
            var role = GetUserRole();
            if (role != "admin") return Forbid();

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Reservationseats)
                .OrderByDescending(r => r.Starttime)
                .Select(r => new
                {
                    id = r.Reservationid,
                    user = r.User.Fullname,
                    userEmail = r.User.Email,
                    type = r.Reservationtype,
                    startTime = r.Starttime,
                    endTime = r.Endtime,
                    status = r.Status,
                    seats = r.Reservationseats.Select(s => s.Seatid).ToList(),
                    notes = r.Notes,
                    createdAt = r.Createdat
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // POST /api/Reservations - Create a reservation
        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            // Validate time
            if (dto.StartTime <= DateTime.Now)
                return BadRequest(new { error = "Start time must be in the future" });

            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new { error = "End time must be after start time" });

            // Validate duration based on type
            var duration = dto.EndTime - dto.StartTime;
            if (dto.ReservationType == "solo" && duration.TotalHours > 3)
                return BadRequest(new { error = "Solo reservations cannot exceed 3 hours" });

            if (dto.ReservationType == "group" && duration.TotalHours > 2)
                return BadRequest(new { error = "Group reservations cannot exceed 2 hours" });

            if (dto.Seats == null || dto.Seats.Count == 0)
                return BadRequest(new { error = "Please select at least one seat" });

            // Validate seat count for solo
            if (dto.ReservationType == "solo" && dto.Seats.Count > 1)
                return BadRequest(new { error = "Solo reservations can only have 1 seat" });

            // Check seat availability
            var conflictingSeats = await _context.Reservations
                .Where(r =>
                    r.Status != "Cancelled" &&
                    r.Status != "Expired" &&
                    r.Starttime < dto.EndTime &&
                    r.Endtime > dto.StartTime)
                .Include(r => r.Reservationseats)
                .SelectMany(r => r.Reservationseats.Select(s => s.Seatid))
                .ToListAsync();

            var unavailable = dto.Seats.Intersect(conflictingSeats).ToList();
            if (unavailable.Any())
                return BadRequest(new { error = $"Seats already reserved: {string.Join(", ", unavailable)}" });

            // Create reservation
            var reservation = new Reservation
            {
                Userid = userId,
                Reservationtype = dto.ReservationType,
                Starttime = dto.StartTime,
                Endtime = dto.EndTime,
                Status = "Active",
                Notes = dto.Notes,
                Createdat = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Add seats
            foreach (var seatId in dto.Seats)
            {
                _context.Reservationseats.Add(new Reservationseat
                {
                    Reservationid = reservation.Reservationid,
                    Seatid = seatId
                });
            }
            await _context.SaveChangesAsync();

            // Send confirmation notification
            await NotificationsController.CreateNotification(
                _context, userId,
                "Reservation Created! ✅",
                $"Your {dto.ReservationType} reservation is confirmed for {dto.StartTime:MMM dd} at {dto.StartTime:HH:mm}. You will receive a reminder 15 minutes before.",
                "reservation_confirmed",
                reservation.Reservationid
            );

            return Ok(new
            {
                message = "Reservation created successfully",
                reservationId = reservation.Reservationid,
                startTime = reservation.Starttime,
                endTime = reservation.Endtime,
                seats = dto.Seats,
                type = dto.ReservationType
            });
        }

        // PUT /api/Reservations/{id}/confirm - Confirm reservation (when reminded)
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            var userId = GetUserId();
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Reservationid == id && r.Userid == userId);

            if (reservation == null) return NotFound(new { error = "Reservation not found" });
            if (reservation.Status == "Cancelled") return BadRequest(new { error = "Reservation is cancelled" });
            if (reservation.Status == "Expired") return BadRequest(new { error = "Reservation has expired" });

            reservation.Status = "Confirmed";
            reservation.Confirmedat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Reservation confirmed! See you there! ✅" });
        }

        // DELETE /api/Reservations/{id} - Cancel reservation
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Reservationid == id &&
                    (r.Userid == userId || role == "admin"));

            if (reservation == null) return NotFound(new { error = "Reservation not found" });
            if (reservation.Status == "Cancelled") return BadRequest(new { error = "Already cancelled" });

            reservation.Status = "Cancelled";
            reservation.Cancelledat = DateTime.Now;
            await _context.SaveChangesAsync();

            // Notify user if admin cancelled it
            if (role == "admin" && reservation.Userid != userId)
            {
                await NotificationsController.CreateNotification(
                    _context, reservation.Userid,
                    "Reservation Cancelled",
                    $"Your reservation on {reservation.Starttime:MMM dd} at {reservation.Starttime:HH:mm} has been cancelled by admin.",
                    "reservation_cancelled",
                    reservation.Reservationid
                );
            }

            return Ok(new { message = "Reservation cancelled" });
        }

        // POST /api/Reservations/check-reminders - Called by background job or frontend polling
        // Sends reminders for reservations starting in 15 minutes
        [HttpPost("check-reminders")]
        [AllowAnonymous] // Will be called by frontend timer
        public async Task<IActionResult> CheckAndSendReminders()
        {
            var now = DateTime.Now;
            var reminderTime = now.AddMinutes(15);

            // Find reservations starting in next 15 minutes that haven't been notified yet
            var upcoming = await _context.Reservations
                .Where(r =>
                    r.Status == "Active" &&
                    r.Starttime <= reminderTime &&
                    r.Starttime > now &&
                    r.Notificationsentat == null)
                .ToListAsync();

            foreach (var res in upcoming)
            {
                // Send notification
                await NotificationsController.CreateNotification(
                    _context, res.Userid,
                    "⏰ Reservation Starting Soon!",
                    $"Your reservation starts at {res.Starttime:HH:mm}. Please confirm to keep your seat!",
                    "reservation_reminder",
                    res.Reservationid
                );

                res.Notificationsentat = DateTime.Now;
            }

            // Auto-expire reservations that started but weren't confirmed
            var toExpire = await _context.Reservations
                .Where(r =>
                    r.Status == "Active" &&
                    r.Starttime <= now &&
                    r.Notificationsentat != null)
                .ToListAsync();

            foreach (var res in toExpire)
            {
                res.Status = "Expired";
                await NotificationsController.CreateNotification(
                    _context, res.Userid,
                    "Reservation Expired ❌",
                    $"Your reservation at {res.Starttime:HH:mm} was automatically cancelled because it was not confirmed.",
                    "reservation_cancelled",
                    res.Reservationid
                );
            }

            if (upcoming.Any() || toExpire.Any())
                await _context.SaveChangesAsync();

            return Ok(new
            {
                remindersSet = upcoming.Count,
                expired = toExpire.Count
            });
        }
    }

    // DTOs
    public class CreateReservationDto
    {
        public string ReservationType { get; set; } = "solo"; // 'solo' or 'group'
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Seats { get; set; } = new();
        public string? Notes { get; set; }
    }
}