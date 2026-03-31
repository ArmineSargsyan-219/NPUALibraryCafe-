namespace NPUALibraryCafe.DTOs.Borrowings;

public class RejectDto { public string? Reason { get; set; } }

public class BorrowingResponseDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = "";
    public string BookAuthor { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? BorrowedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
}

public class BorrowingDetailDto : BorrowingResponseDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
}