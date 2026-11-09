using Ecommerce.Application.Common.Models;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

[Authorize]
public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Checkout and place a new order using the SQL Server Stored Procedure Transaction!
    /// (Requirement: Multi-table write transaction using TRY...CATCH and BEGIN TRAN/COMMIT/ROLLBACK)
    /// </summary>
    [HttpPost("checkout")]
    [Authorize(Roles = "Customer,Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderDto dto)
    {
        var result = await _orderService.PlaceOrderWithTransactionSpAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, 
            ApiResponse<OrderDto>.Ok(result, "Order successfully placed via stored procedure transaction!"));
    }

    /// <summary>
    /// Get current logged-in user's order history
    /// </summary>
    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders()
    {
        var result = await _orderService.GetMyOrdersAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(result));
    }

    /// <summary>
    /// Get all system orders (Admin or Staff only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _orderService.GetAllOrdersAsync();
        return Ok(ApiResponse<IEnumerable<OrderDto>>.Ok(result));
    }

    /// <summary>
    /// Get order details by ID (Owner or Admin/Staff)
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(int id)
    {
        int? filterUserId = IsAdmin || CurrentUserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase) 
            ? null 
            : CurrentUserId;

        var result = await _orderService.GetOrderByIdAsync(id, filterUserId);
        return Ok(ApiResponse<OrderDto>.Ok(result));
    }

    /// <summary>
    /// Update order status (Admin or Staff only)
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
        return Ok(ApiResponse<OrderDto>.Ok(result, $"Order status updated to {dto.Status}."));
    }

    /// <summary>
    /// Cancel a pending order and automatically restore inventory stock
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _orderService.CancelOrderAsync(id, CurrentUserId);
        return Ok(ApiResponse<bool>.Ok(result, "Order cancelled and product stock returned successfully."));
    }
}
