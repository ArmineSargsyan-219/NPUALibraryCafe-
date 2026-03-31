namespace NPUALibraryCafe.DTOs.Books;

public class UpdateShelfDto { public string ShelfNumber { get; set; } = ""; }
public class UpdatePdfDto { public string? PdfUrl { get; set; } }
public class UpdateCopiesDto { public int PhysicalCopies { get; set; } public int AvailableCopies { get; set; } }
public class AddReviewDto { public int Rating { get; set; } public string? Comment { get; set; } }
public class AddBookDto
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Isbn { get; set; }
    public string Category { get; set; } = "";
    public string? ShelfNumber { get; set; }
    public int PhysicalCopies { get; set; } = 1;
}