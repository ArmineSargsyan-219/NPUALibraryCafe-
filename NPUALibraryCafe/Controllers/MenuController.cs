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

        // GET: api/Menu
        [HttpGet]
        public async Task<ActionResult> GetAllMenuItems()
        {
            try
            {
                var menuItems = await _context.Menuitems
                    .OrderBy(m => m.Category)
                    .ThenBy(m => m.Itemname)
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
        public async Task<ActionResult> GetMenuItemById(int id)
        {
            try
            {
                var menuItem = await _context.Menuitems.FindAsync(id);

                if (menuItem == null)
                {
                    return NotFound(new { error = "Menu item not found" });
                }

                return Ok(menuItem);
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
                var menuItems = await _context.Menuitems
                    .Where(m => m.Category.ToLower() == category.ToLower())
                    .OrderBy(m => m.Itemname)
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
                {
                    return BadRequest(new { error = "Search query cannot be empty" });
                }

                var menuItems = await _context.Menuitems
                    .Where(m => m.Itemname.ToLower().Contains(query.ToLower()))
                    .OrderBy(m => m.Itemname)
                    .ToListAsync();

                return Ok(menuItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Search failed", details = ex.Message });
            }
        }
    }
}