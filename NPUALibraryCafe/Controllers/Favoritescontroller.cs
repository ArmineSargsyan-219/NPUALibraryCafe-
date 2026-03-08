using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public FavoritesController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private string GetUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        // GET: api/Favorites
        [HttpGet]
        public async Task<ActionResult> GetFavorites()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var menuFavs = await _context.Database
                    .SqlQueryRaw<FavoriteMenuDto>(
                        @"SELECT f.id, f.item_id, f.created_at,
                                 m.name, m.price, m.category_id, m.image_url
                          FROM favorites f
                          JOIN menu_items m ON m.id = f.item_id
                          WHERE f.user_id = {0} AND f.item_type = 'menu'
                          ORDER BY f.created_at DESC",
                        userId)
                    .ToListAsync();

                var bookFavs = await _context.Database
                    .SqlQueryRaw<FavoriteBookDto>(
                        @"SELECT f.id, f.item_id::int AS book_id, f.created_at,
                                 b.title, b.author, b.category, b.imagepath,
                                 b.availablecopies, b.shelfnumber, b.pdfurl, b.pdfavailable
                          FROM favorites f
                          JOIN books b ON b.bookid = f.item_id::int
                          WHERE f.user_id = {0} AND f.item_type = 'book'
                          ORDER BY f.created_at DESC",
                        userId)
                    .ToListAsync();

                return Ok(new { menuItems = menuFavs, books = bookFavs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get favorites", details = ex.Message });
            }
        }

        // GET: api/Favorites/ids
        [HttpGet("ids")]
        public async Task<ActionResult> GetFavoriteIds()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var menuIds = await _context.Database
                    .SqlQueryRaw<FavItemIdRow>(
                        "SELECT item_id FROM favorites WHERE user_id = {0} AND item_type = 'menu'",
                        userId)
                    .ToListAsync();

                var bookIds = await _context.Database
                    .SqlQueryRaw<FavItemIdRow>(
                        "SELECT item_id FROM favorites WHERE user_id = {0} AND item_type = 'book'",
                        userId)
                    .ToListAsync();

                return Ok(new
                {
                    menuItemIds = menuIds.Select(r => r.Item_id).ToList(),
                    bookIds = bookIds.Select(r => r.Item_id).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get favorite ids", details = ex.Message });
            }
        }

        // POST: api/Favorites/menu
        [HttpPost("menu")]
        public async Task<ActionResult> AddMenuFavorite([FromBody] AddMenuFavoriteDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var count = await _context.Database
                    .SqlQueryRaw<FavCountRow>(
                        "SELECT COUNT(*)::int AS Count FROM favorites WHERE user_id = {0} AND item_id = {1} AND item_type = 'menu'",
                        userId, dto.MenuItemId)
                    .ToListAsync();

                if (count.FirstOrDefault()?.Count > 0)
                    return Ok(new { message = "Already in favorites" });

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO favorites (user_id, item_id, item_type, created_at) VALUES ({0}, {1}, 'menu', NOW())",
                    userId, dto.MenuItemId);

                return Ok(new { message = "Added to favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add favorite", details = ex.Message });
            }
        }

        // POST: api/Favorites/book
        [HttpPost("book")]
        public async Task<ActionResult> AddBookFavorite([FromBody] AddBookFavoriteDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var count = await _context.Database
                    .SqlQueryRaw<FavCountRow>(
                        "SELECT COUNT(*)::int AS Count FROM favorites WHERE user_id = {0} AND item_id = {1} AND item_type = 'book'",
                        userId, dto.BookId.ToString())
                    .ToListAsync();

                if (count.FirstOrDefault()?.Count > 0)
                    return Ok(new { message = "Already in favorites" });

                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO favorites (user_id, item_id, item_type, created_at) VALUES ({0}, {1}, 'book', NOW())",
                    userId, dto.BookId.ToString());

                return Ok(new { message = "Added to favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add favorite", details = ex.Message });
            }
        }

        // DELETE: api/Favorites/menu/{itemId}
        [HttpDelete("menu/{itemId}")]
        public async Task<ActionResult> RemoveMenuFavorite(string itemId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM favorites WHERE user_id = {0} AND item_id = {1} AND item_type = 'menu'",
                    userId, itemId);

                return Ok(new { message = "Removed from favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove favorite", details = ex.Message });
            }
        }

        // DELETE: api/Favorites/book/{bookId}
        [HttpDelete("book/{bookId}")]
        public async Task<ActionResult> RemoveBookFavorite(int bookId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM favorites WHERE user_id = {0} AND item_id = {1} AND item_type = 'book'",
                    userId, bookId.ToString());

                return Ok(new { message = "Removed from favorites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove favorite", details = ex.Message });
            }
        }
    }

    public class FavoriteMenuDto
    {
        public int Id { get; set; }
        public string Item_id { get; set; } = null!;
        public DateTime Created_at { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Category_id { get; set; }
        public string? Image_url { get; set; }
    }

    public class FavoriteBookDto
    {
        public int Id { get; set; }
        public int Book_id { get; set; }
        public DateTime Created_at { get; set; }
        public string Title { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string? Category { get; set; }
        public string? Imagepath { get; set; }
        public int Availablecopies { get; set; }
        public string? Shelfnumber { get; set; }
        public string? Pdfurl { get; set; }
        public bool Pdfavailable { get; set; }
    }

    public class FavItemIdRow { public string Item_id { get; set; } = null!; }
    public class FavCountRow { public int Count { get; set; } }
    public class AddMenuFavoriteDto { public string MenuItemId { get; set; } = null!; }
    public class AddBookFavoriteDto { public int BookId { get; set; } }
}