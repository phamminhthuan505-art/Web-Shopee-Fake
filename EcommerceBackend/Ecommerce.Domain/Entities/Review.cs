namespace Ecommerce.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public int Rating { get; set; } // 1 - 5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
