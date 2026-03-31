namespace NPUALibraryCafe.DTOs.Favorites;

public class AddMenuFavoriteDto { public string MenuItemId { get; set; } = null!; }
public class AddBookFavoriteDto { public int BookId { get; set; } }

public class FavoriteMenuResponseDto
{
    public int Id { get; set; }
    public string ItemId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? CategoryId { get; set; }
    public string? ImagePath { get; set; }
}

public class FavoriteBookResponseDto
{
    public int Id { get; set; }
    public string ItemId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string? Category { get; set; }
    public string? ImagePath { get; set; }
    public int AvailableCopies { get; set; }
    public string? ShelfNumber { get; set; }
    public string? PdfUrl { get; set; }
    public bool PdfAvailable { get; set; }
}