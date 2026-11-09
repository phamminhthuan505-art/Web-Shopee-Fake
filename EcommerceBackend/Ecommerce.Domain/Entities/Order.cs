using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string ShippingAddress { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD";
    public string PaymentStatus { get; set; } = "Unpaid";

    // Navigation property
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
