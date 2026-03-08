using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using NPUALibraryCafe.Models;

namespace NPUALibraryCafe.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public OrdersController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

        private string GetUserRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // POST /api/Orders - Create order
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest(new { error = "No items in order" });

            try
            {
                decimal total = 0;
                var itemDetails = new List<object>();

                foreach (var item in dto.Items)
                {
                    var menuItems = await _context.Database
                        .SqlQueryRaw<MenuItemLookup>(
                            "SELECT id, name, price FROM menu_items WHERE id = {0}", item.ItemId)
                        .ToListAsync();
                    var menuItem = menuItems.FirstOrDefault();
                    if (menuItem == null)
                        return BadRequest(new { error = $"Menu item {item.ItemId} not found" });

                    total += menuItem.Price * item.Quantity;
                    itemDetails.Add(new { id = item.ItemId, name = menuItem.Name, qty = item.Quantity, price = menuItem.Price });
                }

                var itemsJson = JsonSerializer.Serialize(itemDetails);
                var orderIds = await _context.Database
                    .SqlQueryRaw<OrderIdRow>(
                        @"INSERT INTO orders (user_id, items, total_price, status, order_time, created_at, updated_at)
                          VALUES ({0}, {1}::jsonb, {2}, 'Pending', NOW(), NOW(), NOW())
                          RETURNING id",
                        userId, itemsJson, total)
                    .ToListAsync();
                var orderId = orderIds.FirstOrDefault()?.Id ?? 0;

                await NotificationsController.CreateNotification(
                    _context, userId,
                    "Պատվեր ստացվեց ☕",
                    $"Ձեր պատվերն ստացվեց և սպասում է հաստատման։ Ընդամենը՝ {total} AMD",
                    "order_pending",
                    orderId
                );

                return Ok(new { message = "Պատվերը ընդունվեց!", orderId = orderId, totalAmount = total, status = "Pending" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create order", detail = ex.Message });
            }
        }

        // GET /api/Orders/my-orders
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var orders = await _context.Cafeorders
                .Where(o => o.Userid == userId)
                .OrderByDescending(o => o.Orderdate)
                .Select(o => new
                {
                    orderId = o.Orderid,
                    orderDate = o.Orderdate,
                    totalAmount = o.Totalamount,
                    status = o.Status,
                    items = o.Items
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET /api/Orders/pending - Café staff sees active orders
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var role = GetUserRole();
            if (role != "cafe staff" && role != "admin") return Forbid();

            var orders = await _context.Cafeorders
                .Where(o => o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "InProgress")
                .Include(o => o.User)
                .OrderBy(o => o.Orderdate)
                .Select(o => new
                {
                    orderId = o.Orderid,
                    userName = o.User.Fullname,
                    userEmail = o.User.Email,
                    orderDate = o.Orderdate,
                    totalAmount = o.Totalamount,
                    status = o.Status,
                    items = o.Items
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET /api/Orders/all - Admin
        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var role = GetUserRole();
            if (role != "admin") return Forbid();

            var orders = await _context.Cafeorders
                .Include(o => o.User)
                .OrderByDescending(o => o.Orderdate)
                .Select(o => new
                {
                    orderId = o.Orderid,
                    userName = o.User.Fullname,
                    orderDate = o.Orderdate,
                    totalAmount = o.Totalamount,
                    status = o.Status,
                    items = o.Items
                })
                .ToListAsync();

            return Ok(orders);
        }

        // PUT /api/Orders/{id}/confirm
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var role = GetUserRole();
            if (role != "cafe staff" && role != "admin") return Forbid();

            var order = await _context.Cafeorders.FindAsync(id);
            if (order == null) return NotFound(new { error = "Order not found" });

            order.Status = "Confirmed";
            order.Updatedat = DateTime.Now;
            await _context.SaveChangesAsync();

            await NotificationsController.CreateNotification(
                _context, order.Userid,
                "Պատվերը հաստատվեց 👍",
                "Ձեր պատվերը հաստատվեց սրճարանի անձնակազմի կողմից!",
                "order_confirmed", id);

            return Ok(new { message = "Order confirmed", orderId = id });
        }

        // PUT /api/Orders/{id}/inprogress
        [HttpPut("{id}/inprogress")]
        public async Task<IActionResult> MarkInProgress(int id)
        {
            var role = GetUserRole();
            if (role != "cafe staff" && role != "admin") return Forbid();

            var order = await _context.Cafeorders.FindAsync(id);
            if (order == null) return NotFound(new { error = "Order not found" });

            order.Status = "InProgress";
            order.Updatedat = DateTime.Now;
            await _context.SaveChangesAsync();

            await NotificationsController.CreateNotification(
                _context, order.Userid,
                "Պատվերը պատրաստվում է ☕",
                "Ձեր պատվերն այժմ պատրաստվում է! Շուտով կլինի պատրաստ!",
                "order_inprogress", id);

            return Ok(new { message = "Order in progress", orderId = id });
        }

        // PUT /api/Orders/{id}/done
        [HttpPut("{id}/done")]
        public async Task<IActionResult> MarkDone(int id)
        {
            var role = GetUserRole();
            if (role != "cafe staff" && role != "admin") return Forbid();

            var order = await _context.Cafeorders.FindAsync(id);
            if (order == null) return NotFound(new { error = "Order not found" });

            order.Status = "Done";
            order.Completedat = DateTime.Now;
            order.Updatedat = DateTime.Now;
            await _context.SaveChangesAsync();

            await NotificationsController.CreateNotification(
                _context, order.Userid,
                "✅ Պատվերը պատրաստ է!",
                "Ձեր պատվերը պատրաստ է! Եկեք վերցնել սրճարանի կրպակից:",
                "order_done", id);

            return Ok(new { message = "Order done", orderId = id });
        }

        // GET /api/Orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var order = await _context.Cafeorders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Orderid == id);

            if (order == null) return NotFound();
            if (order.Userid != userId && role != "admin" && role != "cafe staff") return Forbid();

            return Ok(new
            {
                orderId = order.Orderid,
                userName = order.User?.Fullname,
                orderDate = order.Orderdate,
                totalAmount = order.Totalamount,
                status = order.Status,
                items = order.Items
            });
        }
    }

    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; } = new();
        public string? PaymentMethod { get; set; }
        public string? OrderType { get; set; }
    }

    public class OrderItemDto
    {
        public string ItemId { get; set; } = "";
        public int Quantity { get; set; }
    }

    public class OrderIdRow { public int Id { get; set; } }
    public class MenuItemLookup
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}