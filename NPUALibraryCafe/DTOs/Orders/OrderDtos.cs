namespace NPUALibraryCafe.DTOs.Orders;

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

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
    public string? Items { get; set; }
}

public class OrderDetailDto : OrderResponseDto
{
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}