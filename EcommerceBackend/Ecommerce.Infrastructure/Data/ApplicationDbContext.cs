using System.Data;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    /// <summary>
    /// Executes the multi-table Stored Procedure transaction sp_CreateOrderWithStockDeduction
    /// which uses BEGIN TRAN, COMMIT, ROLLBACK and TRY...CATCH.
    /// </summary>
    public async Task<int> ExecuteOrderTransactionStoredProcedureAsync(
        int userId,
        string shippingAddress,
        string paymentMethod,
        string itemsJson,
        CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "[dbo].[sp_CreateOrderWithStockDeduction]";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 60;

            var paramUserId = new SqlParameter("@UserId", SqlDbType.Int) { Value = userId };
            var paramAddress = new SqlParameter("@ShippingAddress", SqlDbType.NVarChar, 255) { Value = shippingAddress };
            var paramPaymentMethod = new SqlParameter("@PaymentMethod", SqlDbType.NVarChar, 50) { Value = paymentMethod };
            var paramJson = new SqlParameter("@ItemsJson", SqlDbType.NVarChar, -1) { Value = itemsJson };

            var paramOrderId = new SqlParameter("@NewOrderId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            command.Parameters.Add(paramUserId);
            command.Parameters.Add(paramAddress);
            command.Parameters.Add(paramPaymentMethod);
            command.Parameters.Add(paramJson);
            command.Parameters.Add(paramOrderId);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (paramOrderId.Value != DBNull.Value && paramOrderId.Value != null)
            {
                return Convert.ToInt32(paramOrderId.Value);
            }

            throw new BadRequestException("Stored procedure completed without returning an order ID.");
        }
        catch (SqlException ex)
        {
            // Catch business errors raised by the Stored Procedure THROW/RAISERROR
            throw new BadRequestException($"Order transaction failed: {ex.Message}");
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}
