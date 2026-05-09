namespace NPUALibraryCafe.DTOs.Menu;

public class MenuItemResponseDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? CategoryId { get; set; }
    public decimal Price { get; set; }
    public string? ImagePath { get; set; }
    public bool Available { get; set; }
}