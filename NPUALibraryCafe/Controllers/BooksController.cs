using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public BooksController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        // GET: api/Books
        [HttpGet]
        public async Task<ActionResult> GetAllBooks()
        {
            try
            {
                var books = await _context.Books
                    .OrderBy(b => b.Title)
                    .ToListAsync();

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve books", details = ex.Message });
            }
        }

        // GET: api/Books/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetBookById(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);

                if (book == null)
                {
                    return NotFound(new { error = "Book not found" });
                }

                return Ok(book);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve book", details = ex.Message });
            }
        }

        // GET: api/Books/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult> GetBooksByCategory(string category)
        {
            try
            {
                var books = await _context.Books
                    .Where(b => b.Category.ToLower() == category.ToLower())
                    .OrderBy(b => b.Title)
                    .ToListAsync();

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve books", details = ex.Message });
            }
        }

        // GET: api/Books/search?query=searchterm
        [HttpGet("search")]
        public async Task<ActionResult> SearchBooks([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { error = "Search query cannot be empty" });
                }

                var books = await _context.Books
                    .Where(b => b.Title.ToLower().Contains(query.ToLower()) ||
                               b.Author.ToLower().Contains(query.ToLower()))
                    .OrderBy(b => b.Title)
                    .ToListAsync();

                return Ok(books);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", details = ex.Message });
            }
        }

        // POST: api/Books/{bookId}/borrow
        [Authorize]
        [HttpPost("{bookId}/borrow")]
        public async Task<ActionResult> BorrowBook(int bookId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                {
                    return NotFound(new { error = "Book not found" });
                }

                var existingBorrowing = await _context.Borrowings
                    .Where(b => b.Bookid == bookId && b.Returndate == null)
                    .FirstOrDefaultAsync();

                if (existingBorrowing != null)
                {
                    return BadRequest(new { error = "Book is already borrowed" });
                }

                var borrowing = new Borrowing
                {
                    Userid = userId,
                    Bookid = bookId,
                    Borrowdate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Duedate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                    Returndate = null
                };

                _context.Borrowings.Add(borrowing);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Book borrowed successfully",
                    borrowing = new
                    {
                        id = borrowing.Borrowid,
                        bookId = borrowing.Bookid,
                        borrowDate = borrowing.Borrowdate,
                        dueDate = borrowing.Duedate
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to borrow book", details = ex.Message });
            }
        }

        // GET: api/Books/borrowed
        [Authorize]
        [HttpGet("borrowed")]
        public async Task<ActionResult> GetBorrowedBooks()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var borrowedBooks = await _context.Borrowings
                    .Include(b => b.Book)
                    .Where(b => b.Userid == userId && b.Returndate == null)
                    .OrderByDescending(b => b.Borrowdate)
                    .Select(b => new
                    {
                        borrowingId = b.Borrowid,
                        bookId = b.Bookid,
                        title = b.Book.Title,
                        author = b.Book.Author,
                        borrowDate = b.Borrowdate,
                        dueDate = b.Duedate,
                        isOverdue = b.Duedate < DateOnly.FromDateTime(DateTime.UtcNow)
                    })
                    .ToListAsync();

                return Ok(borrowedBooks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve borrowed books", details = ex.Message });
            }
        }

        // PUT: api/Books/borrowed/{borrowingId}/return
        [Authorize]
        [HttpPut("borrowed/{borrowingId}/return")]
        public async Task<ActionResult> ReturnBook(int borrowingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var borrowing = await _context.Borrowings
                    .FirstOrDefaultAsync(b => b.Borrowid == borrowingId && b.Userid == userId);

                if (borrowing == null)
                {
                    return NotFound(new { error = "Borrowing record not found" });
                }

                if (borrowing.Returndate != null)
                {
                    return BadRequest(new { error = "Book already returned" });
                }

                borrowing.Returndate = DateOnly.FromDateTime(DateTime.UtcNow);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Book returned successfully",
                    returnDate = borrowing.Returndate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to return book", details = ex.Message });
            }
        }

        // GET: api/Books/{bookId}/reviews
        [HttpGet("{bookId}/reviews")]
        public async Task<ActionResult> GetBookReviews(int bookId)
        {
            try
            {
                var reviews = await _context.Bookreviews
                    .Include(r => r.User)
                    .Where(r => r.Bookid == bookId)
                    .OrderByDescending(r => r.Reviewid)
                    .Select(r => new
                    {
                        reviewId = r.Reviewid,
                        userId = r.Userid,
                        userName = r.User.Fullname,
                        rating = r.Rating,
                        comment = r.Comment
                    })
                    .ToListAsync();

                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve reviews", details = ex.Message });
            }
        }

        // POST: api/Books/{bookId}/reviews
        [Authorize]
        [HttpPost("{bookId}/reviews")]
        public async Task<ActionResult> AddBookReview(int bookId, [FromBody] AddReviewDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var book = await _context.Books.FindAsync(bookId);
                if (book == null)
                {
                    return NotFound(new { error = "Book not found" });
                }

                var existingReview = await _context.Bookreviews
                    .FirstOrDefaultAsync(r => r.Userid == userId && r.Bookid == bookId);

                if (existingReview != null)
                {
                    return BadRequest(new { error = "You have already reviewed this book" });
                }

                var review = new Bookreview
                {
                    Userid = userId,
                    Bookid = bookId,
                    Rating = dto.Rating,
                    Comment = dto.Comment
                };

                _context.Bookreviews.Add(review);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Review added successfully",
                    reviewId = review.Reviewid
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add review", details = ex.Message });
            }
        }
    }

    // DTO for adding review
    public class AddReviewDto
    {
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}