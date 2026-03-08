using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NPUALibraryCafe.Models;
using NPUALibraryCafe.Controllers;

namespace NPUALibraryCafe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BorrowingsController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;
        public BorrowingsController(LibraryCafeDbContext context) { _context = context; }

        private int GetUserId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        private string GetUserRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // POST /api/Borrowings/request/{bookId} — user requests a book (adds as 'requested' status)
        [HttpPost("request/{bookId}")]
        public async Task<IActionResult> RequestBook(int bookId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();
            try
            {
                var books = await _context.Database
                    .SqlQueryRaw<BookInfoRow>(
                        "SELECT bookid, availablecopies, title, author FROM books WHERE bookid = {0}", bookId)
                    .ToListAsync();
                var book = books.FirstOrDefault();
                if (book == null) return NotFound(new { error = "Գիրքը չի գտնվել" });
                if (book.Availablecopies <= 0) return BadRequest(new { error = "Հասանելի օրինակ չկա" });

                // Check no active borrow
                var existing = await _context.Database
                    .SqlQueryRaw<CountRow>(
                        "SELECT COUNT(*)::int AS Count FROM borrowed_books WHERE user_id={0} AND book_id={1} AND status IN ('requested','borrowed')",
                        userId, bookId)
                    .ToListAsync();
                if (existing.FirstOrDefault()?.Count > 0)
                    return BadRequest(new { error = "Արդեն ունեք ակտիվ հայտ այս գրքի համար" });

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO borrowed_books (user_id, book_id, book_title, book_author, status, borrowed_at, due_date) VALUES ({0},{1},{2},{3},'requested',NOW(),NOW() + INTERVAL '14 days')",
                    userId, bookId, book.Title, book.Author);

                await NotificationsController.CreateNotification(_context, userId,
                    "📚 Հայտն ուղարկված է",
                    $"Ձեր հայտը «{book.Title}» գրքի համար ուղարկված է գրադարանավարին:",
                    "borrow_requested", bookId);

                return Ok(new { message = "Հայտն ուղարկված է" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // GET /api/Borrowings/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBorrowings()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<BorrowRow>(
                        "SELECT id, book_id, book_title, book_author, status, borrowed_at, due_date, returned_at FROM borrowed_books WHERE user_id={0} ORDER BY borrowed_at DESC",
                        userId)
                    .ToListAsync();
                return Ok(rows);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // GET /api/Borrowings/all — librarian
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "library_worker" && role != "admin") return Forbid();
            try
            {
                List<BorrowDetailRow> rows;
                if (string.IsNullOrEmpty(status))
                {
                    rows = await _context.Database
                        .SqlQueryRaw<BorrowDetailRow>(
                            @"SELECT bb.id, bb.book_id, bb.user_id, bb.book_title, bb.book_author,
                                     bb.status, bb.borrowed_at, bb.due_date, bb.returned_at,
                                     u.name AS user_name, u.email AS user_email, u.phone AS user_phone
                              FROM borrowed_books bb
                              JOIN users u ON u.id = bb.user_id
                              ORDER BY bb.borrowed_at DESC")
                        .ToListAsync();
                }
                else
                {
                    rows = await _context.Database
                        .SqlQueryRaw<BorrowDetailRow>(
                            @"SELECT bb.id, bb.book_id, bb.user_id, bb.book_title, bb.book_author,
                                     bb.status, bb.borrowed_at, bb.due_date, bb.returned_at,
                                     u.name AS user_name, u.email AS user_email, u.phone AS user_phone
                              FROM borrowed_books bb
                              JOIN users u ON u.id = bb.user_id
                              WHERE bb.status = {0}
                              ORDER BY bb.borrowed_at DESC", status)
                        .ToListAsync();
                }
                return Ok(rows);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // GET /api/Borrowings/overdue — librarian
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdue()
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "library_worker" && role != "admin") return Forbid();
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<BorrowDetailRow>(
                        @"SELECT bb.id, bb.book_id, bb.user_id, bb.book_title, bb.book_author,
                                 bb.status, bb.borrowed_at, bb.due_date, bb.returned_at,
                                 u.name AS user_name, u.email AS user_email, u.phone AS user_phone
                          FROM borrowed_books bb
                          JOIN users u ON u.id = bb.user_id
                          WHERE bb.status = 'borrowed' AND bb.due_date < NOW()
                          ORDER BY bb.due_date ASC")
                    .ToListAsync();
                return Ok(rows);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT /api/Borrowings/{id}/approve
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "library_worker" && role != "admin") return Forbid();
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<BorrowUserRow>("SELECT user_id, book_title FROM borrowed_books WHERE id={0}", id)
                    .ToListAsync();
                var row = rows.FirstOrDefault();
                if (row == null) return NotFound();

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE borrowed_books SET status='borrowed', borrowed_at=NOW(), due_date=NOW() + INTERVAL '14 days' WHERE id={0}", id);

                // Decrease available copies
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE books SET availablecopies = availablecopies - 1 WHERE bookid = (SELECT book_id FROM borrowed_books WHERE id={0})", id);

                await NotificationsController.CreateNotification(_context, row.User_id,
                    "✅ Հայտը հաստատված է",
                    $"«{row.Book_title}» գիրքը հաստատված է: Կարող եք գալ վերցնել: Վերադարձի ժամկետ՝ 14 օր:",
                    "borrow_approved", id);

                return Ok(new { message = "Հաստատված է" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT /api/Borrowings/{id}/return
        [HttpPut("{id}/return")]
        public async Task<IActionResult> MarkReturned(int id)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "library_worker" && role != "admin") return Forbid();
            try
            {
                var libUserId = GetUserId();
                var rows = await _context.Database
                    .SqlQueryRaw<BorrowUserRow>("SELECT user_id, book_title FROM borrowed_books WHERE id={0}", id)
                    .ToListAsync();
                var row = rows.FirstOrDefault();
                if (row == null) return NotFound();

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE borrowed_books SET status='returned', returned_at=NOW() WHERE id={0}", id);

                // Insert into returned_books
                await _context.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO returned_books (borrowed_book_id, user_id, book_id, book_title, book_author, status, returned_at, processed_by, processed_at, created_at)
                      SELECT id, user_id, book_id, book_title, book_author, 'returned', NOW(), {0}, NOW(), NOW()
                      FROM borrowed_books WHERE id={1}", libUserId, id);

                // Restore available copies
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE books SET availablecopies = availablecopies + 1 WHERE bookid = (SELECT book_id FROM borrowed_books WHERE id={0})", id);

                await NotificationsController.CreateNotification(_context, row.User_id,
                    "📚 Գիրքը ընդունված է",
                    $"«{row.Book_title}» գիրքը հաջողությամբ վերադարձված է: Շնորհակալություն!",
                    "borrow_returned", id);

                return Ok(new { message = "Վերադարձված է" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT /api/Borrowings/{id}/reject
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "library_worker" && role != "admin") return Forbid();
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<BorrowUserRow>("SELECT user_id, book_title FROM borrowed_books WHERE id={0}", id)
                    .ToListAsync();
                var row = rows.FirstOrDefault();
                if (row == null) return NotFound();

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM borrowed_books WHERE id={0}", id);

                await NotificationsController.CreateNotification(_context, row.User_id,
                    "❌ Հայտը մերժված է",
                    $"«{row.Book_title}» գրքի հայտը մերժված է: {dto.Reason}",
                    "borrow_rejected", id);

                return Ok(new { message = "Մերժված է" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // POST /api/Borrowings/check-overdue
        [HttpPost("check-overdue")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckOverdue()
        {
            try
            {
                var overdue = await _context.Database
                    .SqlQueryRaw<BorrowUserRow>(
                        "SELECT user_id, book_title FROM borrowed_books WHERE status='borrowed' AND due_date < NOW()")
                    .ToListAsync();

                foreach (var row in overdue)
                    await NotificationsController.CreateNotification(_context, row.User_id,
                        "⚠️ Վերադարձի ժամկետը լրացել է",
                        $"«{row.Book_title}» գրքի վերադարձի ժամկետը լրացել է: Խնդրում ենք անհապաղ վերադարձնել:",
                        "borrow_overdue", 0);

                return Ok(new { notified = overdue.Count });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }

    public class BookInfoRow { public int Bookid { get; set; } public int Availablecopies { get; set; } public string Title { get; set; } = ""; public string Author { get; set; } = ""; }
    public class CountRow { public int Count { get; set; } }
    public class BorrowUserRow { public int User_id { get; set; } public string Book_title { get; set; } = ""; }
    public class BorrowRow { public int Id { get; set; } public int Book_id { get; set; } public string Book_title { get; set; } = ""; public string Book_author { get; set; } = ""; public string Status { get; set; } = ""; public DateTime Borrowed_at { get; set; } public DateTime? Due_date { get; set; } public DateTime? Returned_at { get; set; } }
    public class BorrowDetailRow { public int Id { get; set; } public int Book_id { get; set; } public int User_id { get; set; } public string Book_title { get; set; } = ""; public string Book_author { get; set; } = ""; public string Status { get; set; } = ""; public DateTime Borrowed_at { get; set; } public DateTime? Due_date { get; set; } public DateTime? Returned_at { get; set; } public string User_name { get; set; } = ""; public string User_email { get; set; } = ""; public string? User_phone { get; set; } }
    public class RejectDto { public string? Reason { get; set; } }
}