using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Review;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;

    public ReviewService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewDto> CreateReviewAsync(int userId, CreateReviewDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.IsActive);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {dto.ProductId} was not found.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null)
        {
            throw new NotFoundException($"User with ID {userId} was not found.");
        }

        var review = new Review
        {
            UserId = userId,
            ProductId = dto.ProductId,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return new ReviewDto
        {
            Id = review.Id,
            UserId = user.Id,
            Username = user.Username,
            UserFullName = user.FullName,
            ProductId = product.Id,
            ProductName = product.Name,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<IEnumerable<ReviewDto>> GetProductReviewsAsync(int productId)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
        {
            throw new NotFoundException($"Product with ID {productId} was not found.");
        }

        return await _context.Reviews
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                Username = r.User.Username,
                UserFullName = r.User.FullName,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }
}
