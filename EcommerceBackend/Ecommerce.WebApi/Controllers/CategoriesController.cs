using Ecommerce.Application.DTOs.Category;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

public class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto)
    {
        var created = await _categoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        var success = await _categoryService.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _categoryService.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}