using Ecommerce.Application.DTOs.Product;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] ProductQueryParameters queryParameters)
    {
        var products = await _productService.GetPagedListAsync(queryParameters);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var success = await _productService.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _productService.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}