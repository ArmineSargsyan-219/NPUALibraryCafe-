using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            int.TryParse(User.FindFirst("userId")?.Value, out int id) ? id : 0;

        private string GetUserRole() =>
            User.FindFirst("role")?.Value ?? "";

        // POST /api/Orders - Create order (User)
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
                    var menuItem = await _context.Menuitems.FindAsync(item.ItemId);
                    if (menuItem == null)
                        return BadRequest(new { error = $"Menu item {item.ItemId} not found" });

                    total += menuItem.Price * item.Quantity;
                    itemDetails.Add(new { id = menuItem.Itemid, name = menuItem.Itemname, quantity = item.Quantity, price = menuItem.Price });
                }

                var order = new Cafeorder
                {
                    Userid = userId,
                    Orderdate = DateTime.Now,
                    Createdat = DateTime.Now,
                    Updatedat = DateTime.Now,
                    Totalamount = total,
                    Items = System.Text.Json.JsonSerializer.Serialize(itemDetails),
                    Status = "Pending"
                };

                _context.Cafeorders.Add(order);
                await _context.SaveChangesAsync();

                var payment = new Payment
                {
                    Orderid = order.Orderid,
                    Userid = userId,
                    Amount = total,
                    Paymentmethod = string.IsNullOrEmpty(dto.PaymentMethod) ? "cash" : dto.PaymentMethod,
                    Paymentdate = DateTime.Now
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await NotificationsController.CreateNotification(
                    _context, userId,
                    "Order Received! ☕",
                    $"Your order has been received and is waiting for café staff to confirm. Total: {total} AMD",
                    "order_pending",
                    order.Orderid
                );

                return Ok(new
                {
                    message = "Order created successfully",
                    orderId = order.Orderid,
                    totalAmount = total,
                    status = "Pending",
                    paymentId = payment.Paymentid
                });
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

        // GET /api/Orders/pending
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var role = GetUserRole();
            if (role != "cafe staff" && role != "admin")
                return Forbid();

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

        // GET /api/Orders/all
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
            if (order.Status != "Pending") return BadRequest(new { error = "Order is not pending" });

            order.Status = "Confirmed";
            order.Updatedat = DateTime.Now;
            await _context.SaveChangesAsync();

            await NotificationsController.CreateNotification(
                _context, order.Userid,
                "Order Confirmed! 👍",
                "Your order has been confirmed by café staff and is being prepared!",
                "order_confirmed",
                order.Orderid
            );

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
                "Order In Progress! ☕",
                "Your order is being prepared right now! Won't be long!",
                "order_inprogress",
                order.Orderid
            );

            return Ok(new { message = "Order marked as in progress", orderId = id });
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
                "✅ Order Ready! Come Pick Up!",
                "Your order is ready! Please come to the café counter to pick it up and pay.",
                "order_done",
                order.Orderid
            );

            return Ok(new { message = "Order marked as done", orderId = id });
        }

        // PUT /api/Orders/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var role = GetUserRole();
            if (role != "admin") return Forbid();

            var order = await _context.Cafeorders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = dto.Status;
            order.Updatedat = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated" });
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
            if (order.Userid != userId && role != "admin" && role != "cafe staff")
                return Forbid();

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

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "";
    }
}