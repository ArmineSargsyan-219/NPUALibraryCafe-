using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using LibCafe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.DTOs.Favorites;
using System.Security.Claims;

namespace NPUALibraryCafe.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMenuitemRepository _menuRepository;
    private readonly LibraryCafeDbContext _context;

    public FavoritesController(
        IFavoriteRepository favoriteRepository,
        IBookRepository bookRepository,
        IMenuitemRepository menuRepository,
        LibraryCafeDbContext context)
    {
        _favoriteRepository = favoriteRepository;
        _bookRepository = bookRepository;
        _menuRepository = menuRepository;
        _context = context;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var favorites = await _favoriteRepository.GetByUserIdAsync(userId);

        var menuFavs = new List<FavoriteMenuResponseDto>();
        var bookFavs = new List<FavoriteBookResponseDto>();

        foreach (var fav in favorites)
        {
            if (fav.ItemType == "menu")
            {
                var items = await _context.Database
                    .SqlQueryRaw<FavMenuItemDto>(
                        "SELECT id, name, description, category_id, price, image_url, available, rating FROM menu_items WHERE id = {0}",
                        fav.ItemId)
                    .ToListAsync();
                var item = items.FirstOrDefault();
                if (item != null)
                    menuFavs.Add(new FavoriteMenuResponseDto
                    {
                        Id = fav.Id,
                        ItemId = fav.ItemId,
                        CreatedAt = fav.CreatedAt,
                        Name = item.Name,
                        Price = item.Price,
                        CategoryId = item.Category_id,
                        ImagePath = item.Image_url
                    });
            }
            else if (fav.ItemType == "book" && int.TryParse(fav.ItemId, out int bookId))
            {
                var book = await _bookRepository.GetByIdAsync(bookId);
                if (book != null)
                    bookFavs.Add(new FavoriteBookResponseDto
                    {
                        Id = fav.Id,
                        ItemId = fav.ItemId,
                        CreatedAt = fav.CreatedAt,
                        Title = book.Title,
                        Author = book.Author,
                        Category = book.Category,
                        ImagePath = book.Imagepath,
                        AvailableCopies = book.Availablecopies,
                        ShelfNumber = book.Shelfnumber,
                        PdfUrl = book.Pdfurl,
                        PdfAvailable = book.Pdfavailable
                    });
            }
        }

        return Ok(new { menuItems = menuFavs, books = bookFavs });
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetFavoriteIds()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var menuIds = await _favoriteRepository.GetIdsByUserAndTypeAsync(userId, "menu");
        var bookIds = await _favoriteRepository.GetIdsByUserAndTypeAsync(userId, "book");

        return Ok(new { menuItemIds = menuIds, bookIds });
    }

    [HttpPost("menu")]
    public async Task<IActionResult> AddMenuFavorite([FromBody] AddMenuFavoriteDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (await _favoriteRepository.ExistsAsync(userId, dto.MenuItemId, "menu"))
            return Ok(new { message = "Already in favorites" });

        await _favoriteRepository.AddAsync(new Favorite
        {
            UserId = userId,
            ItemId = dto.MenuItemId,
            ItemType = "menu",
            CreatedAt = DateTime.Now
        });

        return Ok(new { message = "Added to favorites" });
    }

    [HttpPost("book")]
    public async Task<IActionResult> AddBookFavorite([FromBody] AddBookFavoriteDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (await _favoriteRepository.ExistsAsync(userId, dto.BookId.ToString(), "book"))
            return Ok(new { message = "Already in favorites" });

        await _favoriteRepository.AddAsync(new Favorite
        {
            UserId = userId,
            ItemId = dto.BookId.ToString(),
            ItemType = "book",
            CreatedAt = DateTime.Now
        });

        return Ok(new { message = "Added to favorites" });
    }

    [HttpDelete("menu/{itemId}")]
    public async Task<IActionResult> RemoveMenuFavorite(string itemId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _favoriteRepository.DeleteAsync(userId, itemId, "menu");
        return Ok(new { message = "Removed from favorites" });
    }

    [HttpDelete("book/{bookId}")]
    public async Task<IActionResult> RemoveBookFavorite(int bookId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await _favoriteRepository.DeleteAsync(userId, bookId.ToString(), "book");
        return Ok(new { message = "Removed from favorites" });
    }

    internal class FavMenuItemDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Category_id { get; set; }
        public decimal Price { get; set; }
        public string? Image_url { get; set; }
        public bool Available { get; set; }
        public decimal? Rating { get; set; }
        public string? Description { get; set; }
    }
}