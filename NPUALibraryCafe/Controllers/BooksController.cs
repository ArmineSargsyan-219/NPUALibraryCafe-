using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public BooksController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst("userId")?.Value, out int id) ? id : 0;

        private string GetUserRole() =>
            User.FindFirst("role")?.Value ?? "";

        // GET /api/Books - All books with shelf info
        [HttpGet]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _context.Books
                .Select(b => new
                {
                    bookId = b.Bookid,
                    title = b.Title,
                    author = b.Author,
                    category = b.Category,
                    isbn = b.Isbn,
                    shelfNumber = b.Shelfnumber,
                    physicalCopies = b.Physicalcopies,
                    availableCopies = b.Availablecopies,
                    pdfAvailable = b.Pdfavailable,
                    pdfUrl = b.Pdfurl
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET /api/Books/search?query=1984 - Search with shelf info
        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string query)
        {
            var books = await _context.Books
                .Where(b => b.Title.ToLower().Contains(query.ToLower()) ||
                            b.Author.ToLower().Contains(query.ToLower()))
                .Select(b => new
                {
                    bookId = b.Bookid,
                    title = b.Title,
                    author = b.Author,
                    category = b.Category,
                    shelfNumber = b.Shelfnumber,
                    physicalCopies = b.Physicalcopies,
                    availableCopies = b.Availablecopies,
                    pdfAvailable = b.Pdfavailable,
                    pdfUrl = b.Pdfurl
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET /api/Books/category/{category}
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var books = await _context.Books
                .Where(b => b.Category.ToLower() == category.ToLower())
                .Select(b => new
                {
                    bookId = b.Bookid,
                    title = b.Title,
                    author = b.Author,
                    category = b.Category,
                    shelfNumber = b.Shelfnumber,
                    availableCopies = b.Availablecopies,
                    pdfAvailable = b.Pdfavailable
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET /api/Books/{id} - Single book
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            return Ok(new
            {
                bookId = book.Bookid,
                title = book.Title,
                author = book.Author,
                category = book.Category,
                isbn = book.Isbn,
                shelfNumber = book.Shelfnumber,
                physicalCopies = book.Physicalcopies,
                availableCopies = book.Availablecopies,
                pdfAvailable = book.Pdfavailable,
                pdfUrl = book.Pdfurl
            });
        }

        // PUT /api/Books/{id}/shelf - Library Worker: Update shelf number
        [HttpPut("{id}/shelf")]
        [Authorize]
        public async Task<IActionResult> UpdateShelf(int id, [FromBody] UpdateShelfDto dto)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "admin")
                return Forbid();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound(new { error = "Book not found" });

            book.Shelfnumber = dto.ShelfNumber;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shelf number updated", bookId = id, shelfNumber = dto.ShelfNumber });
        }

        // PUT /api/Books/{id}/copies - Library Worker: Update physical copies
        [HttpPut("{id}/copies")]
        [Authorize]
        public async Task<IActionResult> UpdateCopies(int id, [FromBody] UpdateCopiesDto dto)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "admin")
                return Forbid();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound(new { error = "Book not found" });

            book.Physicalcopies = dto.PhysicalCopies;
            book.Availablecopies = dto.AvailableCopies;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Copies updated", bookId = id });
        }

        // PUT /api/Books/{id}/pdf - Library Worker: Add PDF link
        [HttpPut("{id}/pdf")]
        [Authorize]
        public async Task<IActionResult> UpdatePdf(int id, [FromBody] UpdatePdfDto dto)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "admin")
                return Forbid();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound(new { error = "Book not found" });

            book.Pdfurl = dto.PdfUrl;
            book.Pdfavailable = !string.IsNullOrEmpty(dto.PdfUrl);
            await _context.SaveChangesAsync();

            return Ok(new { message = "PDF link updated", bookId = id });
        }

        // POST /api/Books - Admin/Librarian: Add new book
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddBook([FromBody] AddBookDto dto)
        {
            var role = GetUserRole();
            if (role != "librarian" && role != "admin")
                return Forbid();

            var book = new Book
            {
                Title = dto.Title,
                Author = dto.Author,
                Isbn = dto.Isbn ?? "",
                Category = dto.Category,
                Bookshelf = dto.ShelfNumber ?? "",
                Shelfnumber = dto.ShelfNumber,
                Physicalcopies = dto.PhysicalCopies,
                Availablecopies = dto.PhysicalCopies,
                Pdfavailable = false
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book added successfully", bookId = book.Bookid });
        }

        // POST /api/Books/{id}/borrow - User borrows a book
        [HttpPost("{id}/borrow")]
        [Authorize]
        public async Task<IActionResult> BorrowBook(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound(new { error = "Book not found" });
            if (book.Availablecopies <= 0)
                return BadRequest(new { error = "No available copies" });

            var dueDate = DateTime.Now.AddDays(14);
            var borrowing = new Borrowing
            {
                Userid = userId,
                Bookid = id,
                Borrowdate = DateTime.Now,
                Duedate = dueDate,
                Returndate = null
            };

            _context.Borrowings.Add(borrowing);
            book.Availablecopies--;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Book borrowed successfully",
                borrowing = new
                {
                    borrowingId = borrowing.Borrowingid,
                    bookTitle = book.Title,
                    shelfNumber = book.Shelfnumber,
                    borrowDate = borrowing.Borrowdate,
                    dueDate = borrowing.Duedate
                }
            });
        }

        // GET /api/Books/borrowed - User's borrowed books
        [HttpGet("borrowed")]
        [Authorize]
        public async Task<IActionResult> GetBorrowedBooks()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var borrowings = await _context.Borrowings
                .Where(b => b.Userid == userId && b.Returndate == null)
                .Include(b => b.Book)
                .Select(b => new
                {
                    borrowingId = b.Borrowingid,
                    bookId = b.Bookid,
                    title = b.Book.Title,
                    author = b.Book.Author,
                    shelfNumber = b.Book.Shelfnumber,
                    borrowDate = b.Borrowdate,
                    dueDate = b.Duedate,
                    isOverdue = b.Duedate < DateTime.Now
                })
                .ToListAsync();

            return Ok(borrowings);
        }

        // PUT /api/Books/borrowed/{id}/return - Return a book
        [HttpPut("borrowed/{id}/return")]
        [Authorize]
        public async Task<IActionResult> ReturnBook(int id)
        {
            var userId = GetUserId();
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Borrowingid == id && b.Userid == userId);

            if (borrowing == null) return NotFound(new { error = "Borrowing record not found" });
            if (borrowing.Returndate != null) return BadRequest(new { error = "Book already returned" });

            borrowing.Returndate = DateTime.Now;
            borrowing.Book.Availablecopies++;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book returned successfully" });
        }

        // GET /api/Books/{id}/reviews - Get reviews
        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviews(int id)
        {
            var reviews = await _context.Bookreviews
                .Where(r => r.Bookid == id)
                .Include(r => r.User)
                .Select(r => new
                {
                    reviewId = r.Reviewid,
                    userName = r.User.Fullname,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.Createdat
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // POST /api/Books/{id}/reviews - Add review
        [HttpPost("{id}/reviews")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] AddReviewDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var review = new Bookreview
            {
                Bookid = id,
                Userid = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                Createdat = DateTime.Now
            };

            _context.Bookreviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Review added successfully" });
        }
    }

    // DTOs
    public class UpdateShelfDto { public string ShelfNumber { get; set; } = ""; }
    public class UpdateCopiesDto { public int PhysicalCopies { get; set; } public int AvailableCopies { get; set; } }
    public class UpdatePdfDto { public string? PdfUrl { get; set; } }
    public class AddBookDto
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string? Isbn { get; set; }
        public string Category { get; set; } = "";
        public string? ShelfNumber { get; set; }
        public int PhysicalCopies { get; set; } = 1;
    }
    public class AddReviewDto { public int Rating { get; set; } public string? Comment { get; set; } }
}