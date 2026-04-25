using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPUALibraryCafe.DTOs.Borrowings;
using System.Security.Claims;

namespace NPUALibraryCafe.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BorrowingsController : ControllerBase
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IBookRepository _bookRepository;
    private readonly INotificationRepository _notificationRepository;

    public BorrowingsController(
        IBorrowingRepository borrowingRepository,
        IBookRepository bookRepository,
        INotificationRepository notificationRepository)
    {
        _borrowingRepository = borrowingRepository;
        _bookRepository = bookRepository;
        _notificationRepository = notificationRepository;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    private string GetUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    private bool IsLibraryStaff() =>
        GetUserRole() is "librarian" or "library_worker" or "admin";

    [HttpPost("request/{bookId}")]
    public async Task<IActionResult> RequestBook(int bookId)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null) return NotFound(new { error = "Գիրքը չի գտնվել" });
        if (book.Availablecopies <= 0) return BadRequest(new { error = "Հասանելի օրինակ չկա" });

        var hasActive = await _borrowingRepository.HasActiveBorrowingAsync(userId, bookId);
        if (hasActive) return BadRequest(new { error = "Արդեն ունեք ակտիվ հայտ այս գրքի համար" });

        await _borrowingRepository.AddAsync(new Borrowing
        {
            Userid = userId,
            Bookid = bookId,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            Status = "requested",
            Borrowdate = DateTime.Now,
            Duedate = DateTime.Now.AddDays(14)
        });

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = userId,
            Title = "📚 Հայտն ուղարկված է",
            Message = $"Ձեր հայտը «{book.Title}» գրքի համար ուղարկված է գրադարանավարին:",
            Type = "borrow_requested",
            Relatedid = bookId,
            Createdat = DateTime.Now
        });

        return Ok(new { message = "Հայտն ուղարկված է" });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBorrowings()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var borrowings = await _borrowingRepository.GetByUserIdAsync(userId);
        return Ok(borrowings.Select(b => new BorrowingResponseDto
        {
            Id = b.Borrowingid,
            BookId = b.Bookid,
            BookTitle = b.BookTitle ?? "",
            BookAuthor = b.BookAuthor ?? "",
            Status = b.Status ?? "",
            BorrowedAt = b.Borrowdate,
            DueDate = b.Duedate,
            ReturnedAt = b.Returndate
        }));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        if (!IsLibraryStaff()) return Forbid();

        var borrowings = await _borrowingRepository.GetAllAsync(status);
        return Ok(borrowings.Select(b => new BorrowingDetailDto
        {
            Id = b.Borrowingid,
            BookId = b.Bookid,
            BookTitle = b.BookTitle ?? "",
            BookAuthor = b.BookAuthor ?? "",
            Status = b.Status ?? "",
            BorrowedAt = b.Borrowdate,
            DueDate = b.Duedate,
            ReturnedAt = b.Returndate,
            UserId = b.Userid,
            UserName = b.User?.Fullname ?? "",
            UserEmail = b.User?.Email ?? "",
            UserPhone = b.User?.Phone
        }));
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        if (!IsLibraryStaff()) return Forbid();

        var borrowings = await _borrowingRepository.GetOverdueAsync();
        return Ok(borrowings.Select(b => new BorrowingDetailDto
        {
            Id = b.Borrowingid,
            BookId = b.Bookid,
            BookTitle = b.BookTitle ?? "",
            BookAuthor = b.BookAuthor ?? "",
            Status = b.Status ?? "",
            BorrowedAt = b.Borrowdate,
            DueDate = b.Duedate,
            ReturnedAt = b.Returndate,
            UserId = b.Userid,
            UserName = b.User?.Fullname ?? "",
            UserEmail = b.User?.Email ?? "",
            UserPhone = b.User?.Phone
        }));
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        if (!IsLibraryStaff()) return Forbid();

        var borrowing = await _borrowingRepository.GetByIdAsync(id);
        if (borrowing == null) return NotFound();

        borrowing.Status = "borrowed";
        borrowing.Borrowdate = DateTime.Now;
        borrowing.Duedate = DateTime.Now.AddDays(14);
        await _borrowingRepository.UpdateAsync(borrowing);

        borrowing.Book.Availablecopies--;
        await _bookRepository.UpdateAsync(borrowing.Book);

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = borrowing.Userid,
            Title = "✅ Հայտը հաստատված է",
            Message = $"«{borrowing.BookTitle}» գիրքը հաստատված է: Կարող եք գալ վերցնել: Վերադարձի ժամկետ՝ 14 օր:",
            Type = "borrow_approved",
            Relatedid = id,
            Createdat = DateTime.Now
        });

        return Ok(new { message = "Հաստատված է" });
    }

    [HttpPut("{id}/return")]
    public async Task<IActionResult> MarkReturned(int id)
    {
        if (!IsLibraryStaff()) return Forbid();

        var borrowing = await _borrowingRepository.GetByIdAsync(id);
        if (borrowing == null) return NotFound();

        borrowing.Status = "returned";
        borrowing.Returndate = DateTime.Now;
        await _borrowingRepository.UpdateAsync(borrowing);

        borrowing.Book.Availablecopies++;
        await _bookRepository.UpdateAsync(borrowing.Book);

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = borrowing.Userid,
            Title = "📚 Գիրքը ընդունված է",
            Message = $"«{borrowing.BookTitle}» գիրքը հաջողությամբ վերադարձված է: Շնորհակալություն!",
            Type = "borrow_returned",
            Relatedid = id,
            Createdat = DateTime.Now
        });

        return Ok(new { message = "Վերադարձված է" });
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
    {
        if (!IsLibraryStaff()) return Forbid();

        var borrowing = await _borrowingRepository.GetByIdAsync(id);
        if (borrowing == null) return NotFound();

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = borrowing.Userid,
            Title = "❌ Հայտը մերժված է",
            Message = $"«{borrowing.BookTitle}» գրքի հայտը մերժված է: {dto.Reason}",
            Type = "borrow_rejected",
            Relatedid = id,
            Createdat = DateTime.Now
        });

        await _borrowingRepository.DeleteAsync(id);
        return Ok(new { message = "Մերժված է" });
    }

    [HttpPost("check-overdue")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckOverdue()
    {
        var overdue = await _borrowingRepository.GetOverdueAsync();

        foreach (var b in overdue)
            await _notificationRepository.CreateAsync(new Notification
            {
                Userid = b.Userid,
                Title = "⚠️ Վերադարձի ժամկետը լրացել է",
                Message = $"«{b.BookTitle}» գրքի վերադարձի ժամկետը լրացել է: Խնդրում ենք անհապաղ վերադարձնել:",
                Type = "borrow_overdue",
                Relatedid = 0,
                Createdat = DateTime.Now
            });

        return Ok(new { notified = overdue.Count() });
    }
}