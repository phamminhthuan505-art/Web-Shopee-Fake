namespace Ecommerce.Domain.Entities;

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}
