using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderDetail> OrderDetails { get; }
    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Executes the stored procedure: sp_CreateOrderWithStockDeduction
    Task<int> ExecuteOrderTransactionStoredProcedureAsync(
        int userId, 
        string shippingAddress, 
        string paymentMethod, 
        string itemsJson, 
        CancellationToken cancellationToken = default);
}
