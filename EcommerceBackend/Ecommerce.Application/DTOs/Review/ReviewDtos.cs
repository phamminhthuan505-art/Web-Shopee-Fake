using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.Review;

public class CreateReviewDto
{
    [Required(ErrorMessage = "Product ID is required.")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Rating is required.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
