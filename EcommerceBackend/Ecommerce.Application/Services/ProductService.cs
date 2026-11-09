using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Product;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(int? categoryId = null, string? search = null)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = p.Reviews.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ProductDto> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            AverageRating = product.Reviews.Any() ? Math.Round(product.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount = product.Reviews.Count,
            CreatedAt = product.CreatedAt
        };
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            throw new BadRequestException($"Category with ID {dto.CategoryId} does not exist.");
        }

        var skuExists = await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.Trim().ToLower());
        if (skuExists)
        {
            throw new BadRequestException($"Product with SKU '{dto.Sku}' already exists.");
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Sku = dto.Sku.Trim().ToUpper(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            ImageUrl = dto.ImageUrl?.Trim(),
            CategoryId = dto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
        {
            throw new BadRequestException($"Category with ID {dto.CategoryId} does not exist.");
        }

        var skuConflict = await _context.Products.AnyAsync(p => p.Id != id && p.Sku.ToLower() == dto.Sku.Trim().ToLower());
        if (skuConflict)
        {
            throw new BadRequestException($"SKU '{dto.Sku}' is already assigned to another product.");
        }

        product.Name = dto.Name.Trim();
        product.Sku = dto.Sku.Trim().ToUpper();
        product.Description = dto.Description?.Trim();
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.ImageUrl = dto.ImageUrl?.Trim();
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(product.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.OrderDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            throw new NotFoundException($"Product with ID {id} was not found.");
        }

        if (product.OrderDetails.Any())
        {
            // Soft delete to protect historical order integrity
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }
}
