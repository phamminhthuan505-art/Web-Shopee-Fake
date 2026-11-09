using System.Text.Json;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;

    public OrderService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the multi-table Stored Procedure transaction (sp_CreateOrderWithStockDeduction)
    /// satisfying rubric requirement: "Bắt buộc có tối thiểu 1 nghiệp vụ giao dịch ghi nhiều bảng 
    /// sử dụng khối TRY...CATCH kết hợp BEGIN TRAN, COMMIT và ROLLBACK"
    /// </summary>
    public async Task<OrderDto> PlaceOrderWithTransactionSpAsync(int userId, CreateOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
        {
            throw new BadRequestException("Order must contain at least one item.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user == null)
        {
            throw new NotFoundException($"Active user with ID {userId} was not found.");
        }

        // Serialize items into JSON payload for the stored procedure
        var payloadItems = dto.Items.Select(i => new
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList();

        var itemsJson = JsonSerializer.Serialize(payloadItems);

        // Execute the atomic SQL Server stored procedure with TRY...CATCH and BEGIN TRAN/COMMIT/ROLLBACK
        var createdOrderId = await _context.ExecuteOrderTransactionStoredProcedureAsync(
            userId,
            dto.ShippingAddress.Trim(),
            dto.PaymentMethod.Trim(),
            itemsJson);

        if (createdOrderId <= 0)
        {
            throw new BadRequestException("Failed to create order via transaction procedure.");
        }

        return await GetOrderByIdAsync(createdOrderId);
    }

    public async Task<OrderDto> GetOrderByIdAsync(int orderId, int? userId = null)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        if (userId.HasValue && order.UserId != userId.Value)
        {
            throw new ForbiddenException("You are not authorized to view this order.");
        }

        return MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        order.Status = newStatus;
        if (newStatus == OrderStatus.Delivered)
        {
            order.PaymentStatus = "Paid";
        }

        await _context.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<bool> CancelOrderAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new NotFoundException($"Order with ID {orderId} was not found.");
        }

        if (order.UserId != userId)
        {
            throw new ForbiddenException("You are not authorized to cancel this order.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new BadRequestException($"Cannot cancel order in '{order.Status}' status. Only Pending orders can be cancelled.");
        }

        // Return reserved inventory back to product stock
        foreach (var detail in order.OrderDetails)
        {
            detail.Product.StockQuantity += detail.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return true;
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.User.FullName,
            CustomerEmail = order.User.Email,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            Items = order.OrderDetails.Select(d => new OrderDetailDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product.Name,
                ProductSku = d.Product.Sku,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                SubTotal = d.SubTotal
            }).ToList()
        };
    }
}
