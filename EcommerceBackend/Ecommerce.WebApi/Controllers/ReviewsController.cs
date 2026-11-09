using Ecommerce.Application.Common.Models;
using Ecommerce.Application.DTOs.Review;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

public class ReviewsController : ApiControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Get all reviews for a specific product (Public)
    /// </summary>
    [HttpGet("product/{productId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProduct(int productId)
    {
        var result = await _reviewService.GetProductReviewsAsync(productId);
        return Ok(ApiResponse<IEnumerable<ReviewDto>>.Ok(result));
    }

    /// <summary>
    /// Post a rating and review for a product (Registered Customers/Users)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Customer,Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var result = await _reviewService.CreateReviewAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetByProduct), new { productId = result.ProductId }, 
            ApiResponse<ReviewDto>.Ok(result, "Thank you for your review!"));
    }
}
