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
    public class OrdersController : ControllerBase
    {
        private readonly LibraryCafeDbContext _context;

        public OrdersController(LibraryCafeDbContext context)
        {
            _context = context;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Calculate total amount
                decimal totalAmount = 0;
                foreach (var item in dto.Items)
                {
                    var menuItem = await _context.Menuitems.FindAsync(item.ItemId);
                    if (menuItem == null)
                    {
                        return BadRequest(new { error = $"Menu item {item.ItemId} not found" });
                    }
                    totalAmount += menuItem.Price * item.Quantity;
                }

                // Create cafe order
                var order = new Cafeorder
                {
                    Userid = userId,
                    Orderdate = DateTime.UtcNow,
                    Totalamount = totalAmount,
                    Ordertype = dto.OrderType ?? "pickup",
                    Status = "Pending"
                };

                _context.Cafeorders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var item in dto.Items)
                {
                    var orderItem = new Cafeorderitem
                    {
                        Orderid = order.Orderid,
                        Itemid = item.ItemId,
                        Quantity = item.Quantity
                    };
                    _context.Cafeorderitems.Add(orderItem);
                }

                // Create payment record
                var payment = new Payment
                {
                    Orderid = order.Orderid,
                    Userid = userId,
                    Amount = totalAmount,
                    Paymentmethod = dto.PaymentMethod ?? "cash",
                    Paymentdate = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Order created successfully",
                    orderId = order.Orderid,
                    totalAmount = totalAmount,
                    paymentId = payment.Paymentid
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to create order", details = ex.Message });
            }
        }

        // GET: api/Orders/my-orders
        [HttpGet("my-orders")]
        public async Task<ActionResult> GetMyOrders()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var orders = await _context.Cafeorders
                    .Include(o => o.Cafeorderitems)
                        .ThenInclude(oi => oi.Item)
                    .Include(o => o.Payments)
                    .Where(o => o.Userid == userId)
                    .OrderByDescending(o => o.Orderdate)
                    .Select(o => new
                    {
                        orderId = o.Orderid,
                        orderDate = o.Orderdate,
                        totalAmount = o.Totalamount,
                        orderType = o.Ordertype,
                        status = o.Status,
                        items = o.Cafeorderitems.Select(oi => new
                        {
                            itemId = oi.Itemid,
                            itemName = oi.Item.Itemname,
                            quantity = oi.Quantity,
                            price = oi.Item.Price
                        }).ToList(),
                        paymentMethod = o.Payments.FirstOrDefault() != null
                            ? o.Payments.FirstOrDefault().Paymentmethod
                            : null
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve orders", details = ex.Message });
            }
        }

        // GET: api/Orders/{orderId}
        [HttpGet("{orderId}")]
        public async Task<ActionResult> GetOrderById(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var order = await _context.Cafeorders
                    .Include(o => o.Cafeorderitems)
                        .ThenInclude(oi => oi.Item)
                    .Include(o => o.Payments)
                    .Where(o => o.Orderid == orderId && o.Userid == userId)
                    .Select(o => new
                    {
                        orderId = o.Orderid,
                        orderDate = o.Orderdate,
                        totalAmount = o.Totalamount,
                        orderType = o.Ordertype,
                        status = o.Status,
                        items = o.Cafeorderitems.Select(oi => new
                        {
                            itemId = oi.Itemid,
                            itemName = oi.Item.Itemname,
                            quantity = oi.Quantity,
                            price = oi.Item.Price
                        }).ToList(),
                        paymentMethod = o.Payments.FirstOrDefault() != null
                            ? o.Payments.FirstOrDefault().Paymentmethod
                            : null,
                        paymentDate = o.Payments.FirstOrDefault() != null
                            ? o.Payments.FirstOrDefault().Paymentdate
                            : (DateTime?)null
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve order", details = ex.Message });
            }
        }

        // PUT: api/Orders/{orderId}/status
        [HttpPut("{orderId}/status")]
        public async Task<ActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var order = await _context.Cafeorders
                    .FirstOrDefaultAsync(o => o.Orderid == orderId && o.Userid == userId);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                order.Status = dto.Status;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update order", details = ex.Message });
            }
        }
    }

    // DTOs
    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; } = new();
        public string? PaymentMethod { get; set; } = "cash";
        public string? OrderType { get; set; } = "pickup";
    }

    public class OrderItemDto
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}