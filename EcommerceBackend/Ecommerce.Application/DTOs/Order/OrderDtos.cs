using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs.Order;

public class OrderItemRequestDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
    public int Quantity { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Shipping address is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Shipping address must be between 5 and 255 characters.")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Payment method is required.")]
    [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters.")]
    public string PaymentMethod { get; set; } = "COD";

    [Required(ErrorMessage = "At least one order item is required.")]
    [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<OrderItemRequestDto> Items { get; set; } = new();
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public List<OrderDetailDto> Items { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Order status is required.")]
    public OrderStatus Status { get; set; }
}
