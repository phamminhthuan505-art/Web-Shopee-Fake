using Ecommerce.Application.Common.Models;
using Ecommerce.Application.DTOs.Product;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Retrieve all products with optional category and search filters (Public)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] string? search)
    {
        var result = await _productService.GetAllAsync(categoryId, search);
        return Ok(ApiResponse<IEnumerable<ProductDto>>.Ok(result));
    }

    /// <summary>
    /// Get product details by ID (Public)
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return Ok(ApiResponse<ProductDto>.Ok(result));
    }

    /// <summary>
    /// Create new product (Admin or Staff only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<ProductDto>.Ok(result, "Product created successfully."));
    }

    /// <summary>
    /// Update existing product (Admin or Staff only)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ProductDto>.Ok(result, "Product updated successfully."));
    }

    /// <summary>
    /// Delete product (Admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Product deleted successfully."));
    }
}
