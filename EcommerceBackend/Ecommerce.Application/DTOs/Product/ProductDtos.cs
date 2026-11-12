using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.Product;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "SKU must be between 3 and 50 characters.")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, 1000000, ErrorMessage = "Stock quantity cannot be negative.")]
    public int StockQuantity { get; set; }

    [Url(ErrorMessage = "Invalid image URL format.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }
}

public class UpdateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SKU is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "SKU must be between 3 and 50 characters.")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, 1000000, ErrorMessage = "Stock quantity cannot be negative.")]
    public int StockQuantity { get; set; }

    [Url(ErrorMessage = "Invalid image URL format.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "Category ID is required.")]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductQueryParameters
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}