using LibCafe.Domain.Entities;
using LibCafe.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPUALibraryCafe.DTOs.Orders;
using System.Security.Claims;
using System.Text.Json;

namespace NPUALibraryCafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ICafeorderRepository _orderRepository;
    private readonly IMenuitemRepository _menuRepository;
    private readonly INotificationRepository _notificationRepository;

    public OrdersController(
        ICafeorderRepository orderRepository,
        IMenuitemRepository menuRepository,
        INotificationRepository notificationRepository)
    {
        _orderRepository = orderRepository;
        _menuRepository = menuRepository;
        _notificationRepository = notificationRepository;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

    private string GetUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    private bool IsCafeStaff() =>
    GetUserRole() is "cafe staff" or "coffee_worker" or "admin";

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest(new { error = "No items in order" });

        decimal total = 0;
        var itemDetails = new List<object>();

        foreach (var item in dto.Items)
        {
            var menuItem = await _menuRepository.GetByIdAsync(item.ItemId);
            if (menuItem == null)
                return BadRequest(new { error = $"Menu item {item.ItemId} not found" });

            total += menuItem.Price * item.Quantity;
            itemDetails.Add(new { id = item.ItemId, name = menuItem.Itemname, qty = item.Quantity, price = menuItem.Price });
        }

        var order = new Cafeorder
        {
            Userid = userId,
            Items = JsonSerializer.Serialize(itemDetails),
            Totalamount = total,
            Status = "pending",
            Orderdate = DateTime.Now,
            Createdat = DateTime.Now,
            Updatedat = DateTime.Now
        };

        var orderId = await _orderRepository.AddAsync(order);

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = userId,
            Title = "Պատվեր ստացվեց ☕",
            Message = $"Ձեր պատվերն ստացվեց և սպասում է հաստատման։ Ընդամենը՝ {total} AMD",
            Type = "order_pending",
            Relatedid = orderId,
            Createdat = DateTime.Now
        });

        return Ok(new { message = "Պատվերը ընդունվեց!", orderId, totalAmount = total, status = "pending" });
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return Ok(orders.Select(o => new OrderResponseDto
        {
            OrderId = o.Orderid,
            OrderDate = o.Orderdate,
            TotalAmount = o.Totalamount,
            Status = o.Status,
            Items = o.Items
        }));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingOrders()
    {
        if (!IsCafeStaff()) return Forbid();

        var orders = await _orderRepository.GetPendingAsync();
        return Ok(orders.Select(o => new OrderDetailDto
        {
            OrderId = o.Orderid,
            OrderDate = o.Orderdate,
            TotalAmount = o.Totalamount,
            Status = o.Status,
            Items = o.Items,
            UserName = o.User.Fullname,
            UserEmail = o.User.Email
        }));
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllOrders()
    {
        if (GetUserRole() != "admin") return Forbid();

        var orders = await _orderRepository.GetAllAsync();
        return Ok(orders.Select(o => new OrderDetailDto
        {
            OrderId = o.Orderid,
            OrderDate = o.Orderdate,
            TotalAmount = o.Totalamount,
            Status = o.Status,
            Items = o.Items,
            UserName = o.User.Fullname
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = GetUserId();
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound();
        if (order.Userid != userId && !IsCafeStaff()) return Forbid();

        return Ok(new OrderDetailDto
        {
            OrderId = order.Orderid,
            OrderDate = order.Orderdate,
            TotalAmount = order.Totalamount,
            Status = order.Status,
            Items = order.Items,
            UserName = order.User?.Fullname,
            UserEmail = order.User?.Email
        });
    }



    [HttpPut("{id}/ready")]
    public async Task<IActionResult> MarkReady(int id)
    {
        if (!IsCafeStaff()) return Forbid();
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound(new { error = "Order not found" });

        order.Status = "ready";
        order.Updatedat = DateTime.Now;
        await _orderRepository.UpdateAsync(order);

        await _notificationRepository.CreateAsync(new Notification
        {
            Userid = order.Userid,
            Title = "✅ Պատվերը պատրաստ է!",
            Message = "Ձեր պատվերը պատրաստ է! Եկեք վերցնել սրճարանի կրպակից:",
            Type = "order_done",
            Relatedid = id,
            Createdat = DateTime.Now
        });

        return Ok(new { message = "Order ready", orderId = id });
    }

    [HttpPut("{id}/history")]
    public async Task<IActionResult> MarkHistory(int id)
    {
        if (!IsCafeStaff()) return Forbid();
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound(new { error = "Order not found" });

        order.Status = "history";
        order.Completedat = DateTime.Now;
        order.Updatedat = DateTime.Now;
        await _orderRepository.UpdateAsync(order);

        return Ok(new { message = "Order collected", orderId = id });
    }

}