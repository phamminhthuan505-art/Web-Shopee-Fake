namespace Ecommerce.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
