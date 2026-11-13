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
    /// Lấy danh sách sản phẩm có tìm kiếm theo tên/SKU và lọc theo danh mục (Công khai)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductQueryParameters queryParameters)
    {
        var products = await _productService.GetAllAsync(queryParameters.CategoryId, queryParameters.SearchTerm);
        return Ok(ApiResponse<IEnumerable<ProductDto>>.Ok(products));
    }

    /// <summary>
    /// Xem chi tiết sản phẩm theo ID kèm điểm đánh giá và danh mục (Công khai)
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>
    /// Thêm sản phẩm mới (Chỉ dành cho Admin và Staff)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, 
            ApiResponse<ProductDto>.Ok(created, "Thêm sản phẩm mới thành công."));
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm (Chỉ dành cho Admin và Staff)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var updated = await _productService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ProductDto>.Ok(updated, "Cập nhật sản phẩm thành công."));
    }

    /// <summary>
    /// Xóa sản phẩm theo ID (Chỉ dành cho Admin và Staff, tự động Soft Delete nếu đã có đơn hàng)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(true, "Xóa sản phẩm thành công."));
    }
}
