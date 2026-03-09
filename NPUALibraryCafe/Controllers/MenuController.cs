using LibCafe.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NPUALibraryCafe.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;
        public MenuController(LibraryCafeDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult> GetAllMenuItems()
        {
            try
            {
                var items = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, description, category_id, price, image_url, available, rating FROM menu_items WHERE available = true ORDER BY category_id, name")
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult> GetByCategory(string category)
        {
            try
            {
                var items = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, description, category_id, price, image_url, available, rating FROM menu_items WHERE available = true AND category_id = {0} ORDER BY name",
                        category)
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet("search")]
        public async Task<ActionResult> Search([FromQuery] string query)
        {
            try
            {
                var items = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, description, category_id, price, image_url, available, rating FROM menu_items WHERE available = true AND (LOWER(name) LIKE {0} OR LOWER(description) LIKE {0}) ORDER BY name",
                        $"%{query.ToLower()}%")
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }

    public class MenuItemDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Category_id { get; set; }
        public decimal Price { get; set; }
        public string? Image_url { get; set; }
        public bool Available { get; set; }
        public decimal? Rating { get; set; }
    }
}