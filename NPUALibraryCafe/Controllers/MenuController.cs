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

        // GET: api/Menu/all - Admin: get ALL items including unavailable
        [HttpGet("all")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();
                var items = await _context.Database
                    .SqlQueryRaw<MenuItemDto>(
                        "SELECT id, name, description, category_id, price, image_url, available, rating FROM menu_items ORDER BY category_id, name")
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // PUT: api/Menu/{id} - Admin: edit menu item
        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> UpdateMenuItem(string id, [FromBody] UpdateMenuItemDto dto)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE menu_items SET name={0}, description={1}, price={2}, available={3}, category_id={4} WHERE id={5}",
                    dto.Name, dto.Description ?? "", dto.Price, dto.Available, dto.CategoryId ?? "", id);
                return Ok(new { message = "Updated" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // POST: api/Menu - Admin: add new menu item
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> AddMenuItem([FromBody] AddMenuItemDto dto)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();
                var newId = dto.CategoryId?.Substring(0, 1).ToLower() + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO menu_items (id, name, description, category_id, price, available, rating) VALUES ({0},{1},{2},{3},{4},{5},{6})",
                    newId, dto.Name, dto.Description ?? "", dto.CategoryId ?? "other", dto.Price, true, 0);
                return Ok(new { message = "Added", id = newId });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        // DELETE: api/Menu/{id} - Admin: delete menu item
        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> DeleteMenuItem(string id)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "admin") return Forbid();
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM menu_items WHERE id={0}", id);
                return Ok(new { message = "Deleted" });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }
    }

    public class UpdateMenuItemDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool Available { get; set; }
        public string? CategoryId { get; set; }
    }

    public class AddMenuItemDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? CategoryId { get; set; }
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