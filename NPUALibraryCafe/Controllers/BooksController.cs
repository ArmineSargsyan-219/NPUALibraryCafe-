using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPUALibraryCafe.DTOs.Books;

namespace NPUALibraryCafe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _bookRepository;

    public BooksController(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

    private string GetUserRole() =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetBooks()
    {
        var books = await _bookRepository.GetAllAsync();
        return Ok(books.Select(b => new
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
            pdfUrl = b.Pdfurl,
            imagepath = b.Imagepath
        }));
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks([FromQuery] string query)
    {
        var books = await _bookRepository.SearchAsync(query);
        return Ok(books.Select(b => new
        {
            bookId = b.Bookid,
            title = b.Title,
            author = b.Author,
            category = b.Category,
            shelfNumber = b.Shelfnumber,
            physicalCopies = b.Physicalcopies,
            availableCopies = b.Availablecopies,
            pdfAvailable = b.Pdfavailable,
            pdfUrl = b.Pdfurl,
            imagepath = b.Imagepath
        }));
    }

    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var books = await _bookRepository.GetByCategoryAsync(category);
        return Ok(books.Select(b => new
        {
            bookId = b.Bookid,
            title = b.Title,
            author = b.Author,
            category = b.Category,
            shelfNumber = b.Shelfnumber,
            availableCopies = b.Availablecopies,
            pdfAvailable = b.Pdfavailable
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBook(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);
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

    [HttpPut("{id}/shelf")]
    [Authorize]
    public async Task<IActionResult> UpdateShelf(int id, [FromBody] UpdateShelfDto dto)
    {
        if (GetUserRole() != "librarian" && GetUserRole() != "admin") return Forbid();

        var book = await _bookRepository.GetByIdAsync(id);
        if (book == null) return NotFound(new { error = "Book not found" });

        book.Shelfnumber = dto.ShelfNumber;
        await _bookRepository.UpdateAsync(book);
        return Ok(new { message = "Shelf number updated", bookId = id, shelfNumber = dto.ShelfNumber });
    }

    [HttpPut("{id}/copies")]
    [Authorize]
    public async Task<IActionResult> UpdateCopies(int id, [FromBody] UpdateCopiesDto dto)
    {
        if (GetUserRole() != "librarian" && GetUserRole() != "admin") return Forbid();

        var book = await _bookRepository.GetByIdAsync(id);
        if (book == null) return NotFound(new { error = "Book not found" });

        book.Physicalcopies = dto.PhysicalCopies;
        book.Availablecopies = dto.AvailableCopies;
        await _bookRepository.UpdateAsync(book);
        return Ok(new { message = "Copies updated", bookId = id });
    }

    [HttpPut("{id}/pdf")]
    [Authorize]
    public async Task<IActionResult> UpdatePdf(int id, [FromBody] UpdatePdfDto dto)
    {
        if (GetUserRole() != "librarian" && GetUserRole() != "admin") return Forbid();

        var book = await _bookRepository.GetByIdAsync(id);
        if (book == null) return NotFound(new { error = "Book not found" });

        book.Pdfurl = dto.PdfUrl;
        book.Pdfavailable = !string.IsNullOrEmpty(dto.PdfUrl);
        await _bookRepository.UpdateAsync(book);
        return Ok(new { message = "PDF link updated", bookId = id });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddBook([FromBody] AddBookDto dto)
    {
        if (GetUserRole() != "librarian" && GetUserRole() != "admin") return Forbid();

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

        await _bookRepository.AddAsync(book);
        return Ok(new { message = "Book added successfully", bookId = book.Bookid });
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetReviews(int id)
    {
        var reviews = await _bookRepository.GetReviewsAsync(id);
        return Ok(reviews.Select(r => new
        {
            reviewId = r.Reviewid,
            userName = r.User.Fullname,
            rating = r.Rating,
            comment = r.Comment,
            createdAt = r.Createdat
        }));
    }

    [HttpPost("{id}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int id, [FromBody] AddReviewDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        await _bookRepository.AddReviewAsync(new Bookreview
        {
            Bookid = id,
            Userid = userId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            Createdat = DateTime.Now
        });
        return Ok(new { message = "Review added successfully" });
    }
}