using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public MenuController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        // GET: api/Menu  (kept for backwards compatibility)
        [HttpGet]
        public async Task<ActionResult> GetAllMenuItems()
        {
            return await GetItems();
        }

        // GET: api/Menu/items
        [HttpGet("items")]
        public async Task<ActionResult> GetItems()
        {
            try
            {
                var menuItems = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, price, category_id, image_url, description FROM menu_items ORDER BY category_id, name"
                    )
                    .ToListAsync();

                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve menu items", details = ex.Message });
            }
        }

        // GET: api/Menu/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetMenuItemById(string id)
        {
            try
            {
                var menuItems = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, price, category_id, image_url, description FROM menu_items WHERE id = {0}", id
                    )
                    .ToListAsync();

                var item = menuItems.FirstOrDefault();
                if (item == null)
                    return NotFound(new { error = "Menu item not found" });

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve menu item", details = ex.Message });
            }
        }

        // GET: api/Menu/category/{category}
        [HttpGet("category/{category}")]
        public async Task<ActionResult> GetMenuItemsByCategory(string category)
        {
            try
            {
                var menuItems = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, price, category_id, image_url, description FROM menu_items WHERE LOWER(category_id) = LOWER({0}) ORDER BY name", category
                    )
                    .ToListAsync();

                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve menu items", details = ex.Message });
            }
        }

        // GET: api/Menu/search?query=coffee
        [HttpGet("search")]
        public async Task<ActionResult> SearchMenuItems([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest(new { error = "Search query cannot be empty" });

                var menuItems = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, price, category_id, image_url, description FROM menu_items WHERE LOWER(name) LIKE LOWER({0}) ORDER BY name",
                        $"%{query}%"
                    )
                    .ToListAsync();

                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", details = ex.Message });
            }
        }
    }

    // DTO matching the actual menu_items table columns
    public class MenuItemDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Category_id { get; set; }
        public string? Image_url { get; set; }
        public string? Description { get; set; }
    }
}