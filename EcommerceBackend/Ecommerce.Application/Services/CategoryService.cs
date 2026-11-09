using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Category;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;

    public CategoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new NotFoundException($"Category with ID {id} was not found.");
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ProductCount = category.Products.Count,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower()))
        {
            throw new BadRequestException($"Category '{dto.Name}' already exists.");
        }

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ProductCount = 0,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new NotFoundException($"Category with ID {id} was not found.");
        }

        var duplicate = await _context.Categories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (duplicate)
        {
            throw new BadRequestException($"Category name '{dto.Name}' is already taken.");
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ProductCount = category.Products.Count,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            throw new NotFoundException($"Category with ID {id} was not found.");
        }

        if (category.Products.Any())
        {
            throw new BadRequestException($"Cannot delete category '{category.Name}' because it has {category.Products.Count} associated products.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
}
