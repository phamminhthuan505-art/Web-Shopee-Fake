using Ecommerce.Application.Common.Models;
using Ecommerce.Application.DTOs.Category;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Get all product categories (Public)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _categoryService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(result));
    }

    /// <summary>
    /// Get category details by ID (Public)
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        return Ok(ApiResponse<CategoryDto>.Ok(result));
    }

    /// <summary>
    /// Create new category (Admin or Staff only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var result = await _categoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CategoryDto>.Ok(result, "Category created successfully."));
    }

    /// <summary>
    /// Update existing category (Admin or Staff only)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var result = await _categoryService.UpdateAsync(id, dto);
        return Ok(ApiResponse<CategoryDto>.Ok(result, "Category updated successfully."));
    }

    /// <summary>
    /// Delete category (Admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Category deleted successfully."));
    }
}
