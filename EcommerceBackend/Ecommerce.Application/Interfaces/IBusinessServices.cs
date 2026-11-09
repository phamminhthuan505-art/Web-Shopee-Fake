using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Category;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.Product;
using Ecommerce.Application.DTOs.Review;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<CurrentUserDto> GetCurrentUserAsync(int userId);
}

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
}

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync(int? categoryId = null, string? search = null);
    Task<ProductDto> GetByIdAsync(int id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto);
    Task<bool> DeleteAsync(int id);
}

public interface IOrderService
{
    // High-concurrency Transaction Stored Procedure requirement:
    Task<OrderDto> PlaceOrderWithTransactionSpAsync(int userId, CreateOrderDto dto);

    Task<OrderDto> GetOrderByIdAsync(int orderId, int? userId = null);
    Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int userId);
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
    Task<bool> CancelOrderAsync(int orderId, int userId);
}

public interface IReviewService
{
    Task<ReviewDto> CreateReviewAsync(int userId, CreateReviewDto dto);
    Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId);
}
