using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // 1. Ensure Stored Procedure is installed
            await EnsureStoredProcedureAsync();

            // 2. Seed Roles (Admin, Staff, Customer)
            if (!await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Seeding Roles...");
                var roles = new List<Role>
                {
                    new() { Name = "Admin", Description = "System administrator with full access" },
                    new() { Name = "Staff", Description = "Staff member with management privileges" },
                    new() { Name = "Customer", Description = "Registered retail customer" }
                };
                await _context.Roles.AddRangeAsync(roles);
                await _context.SaveChangesAsync();
            }

            var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin");
            var staffRole = await _context.Roles.FirstAsync(r => r.Name == "Staff");
            var customerRole = await _context.Roles.FirstAsync(r => r.Name == "Customer");

            // 3. Seed Default Users
            if (!await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Seeding default users...");
                var users = new List<User>
                {
                    new()
                    {
                        Username = "admin",
                        Email = "admin@ecommerce.local",
                        FullName = "System Administrator",
                        PhoneNumber = "0901234567",
                        PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                        RoleId = adminRole.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Username = "staff",
                        Email = "staff@ecommerce.local",
                        FullName = "Store Manager",
                        PhoneNumber = "0902345678",
                        PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                        RoleId = staffRole.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Username = "customer",
                        Email = "customer@ecommerce.local",
                        FullName = "Nguyen Van A",
                        PhoneNumber = "0903456789",
                        PasswordHash = _passwordHasher.HashPassword("Customer@123"),
                        RoleId = customerRole.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();
            }

            // 4. Seed Categories
            if (!await _context.Categories.AnyAsync())
            {
                _logger.LogInformation("Seeding Categories...");
                var categories = new List<Category>
                {
                    new() { Name = "Laptops & Computers", Description = "High-performance laptops, PCs, and workstations" },
                    new() { Name = "Smartphones & Tablets", Description = "Latest flagship smartphones and tablets" },
                    new() { Name = "Audio & Accessories", Description = "Wireless headphones, earbuds, and audio gear" }
                };
                await _context.Categories.AddRangeAsync(categories);
                await _context.SaveChangesAsync();
            }

            var laptopCategory = await _context.Categories.FirstAsync(c => c.Name == "Laptops & Computers");
            var phoneCategory = await _context.Categories.FirstAsync(c => c.Name == "Smartphones & Tablets");
            var audioCategory = await _context.Categories.FirstAsync(c => c.Name == "Audio & Accessories");

            // 5. Seed Products
            if (!await _context.Products.AnyAsync())
            {
                _logger.LogInformation("Seeding Products...");
                var products = new List<Product>
                {
                    new()
                    {
                        Name = "MacBook Pro 16 M3 Max",
                        Sku = "MBP-16-M3MAX",
                        Description = "Apple M3 Max chip with 16-core CPU, 40-core GPU, 48GB Unified Memory, 1TB SSD",
                        Price = 3499.00m,
                        StockQuantity = 25,
                        ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800",
                        CategoryId = laptopCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Dell XPS 15 9530",
                        Sku = "DELL-XPS-15",
                        Description = "Intel Core i9-13900H, 32GB DDR5, 1TB NVMe SSD, NVIDIA RTX 4070 8GB, 3.5K OLED Touch",
                        Price = 2499.00m,
                        StockQuantity = 18,
                        ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800",
                        CategoryId = laptopCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "iPhone 16 Pro Max 256GB",
                        Sku = "IPHONE-16-PROMAX",
                        Description = "Titanium design, Ceramic Shield front, A18 Pro chip, 48MP Fusion camera system",
                        Price = 1199.00m,
                        StockQuantity = 40,
                        ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800",
                        CategoryId = phoneCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Samsung Galaxy S24 Ultra",
                        Sku = "GALAXY-S24-ULTRA",
                        Description = "Galaxy AI, 200MP camera, Snapdragon 8 Gen 3, Titanium frame, Built-in S Pen",
                        Price = 1299.00m,
                        StockQuantity = 30,
                        ImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=800",
                        CategoryId = phoneCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Sony WH-1000XM5 Wireless Headphones",
                        Sku = "SONY-WH1000XM5",
                        Description = "Industry-leading noise cancellation, 30 hours battery, crystal clear hands-free calling",
                        Price = 399.00m,
                        StockQuantity = 50,
                        ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800",
                        CategoryId = audioCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Name = "Apple AirPods Pro (2nd Generation)",
                        Sku = "AIRPODS-PRO-2",
                        Description = "Active Noise Cancellation, Transparency mode, MagSafe Case with USB-C",
                        Price = 249.00m,
                        StockQuantity = 60,
                        ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800",
                        CategoryId = audioCategory.Id,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
            }

            // 6. Seed Sample Review
            if (!await _context.Reviews.AnyAsync())
            {
                var sampleCustomer = await _context.Users.FirstAsync(u => u.Username == "customer");
                var sampleProduct = await _context.Products.FirstAsync(p => p.Sku == "MBP-16-M3MAX");

                var review = new Review
                {
                    UserId = sampleCustomer.Id,
                    ProductId = sampleProduct.Id,
                    Rating = 5,
                    Comment = "Incredible performance and battery life! Best workstation laptop for developers.",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Reviews.AddAsync(review);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes the SQL script to create or update the stored procedure in the database.
    /// </summary>
    public async Task EnsureStoredProcedureAsync()
    {
        const string sql = @"
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateOrderWithStockDeduction]
    @UserId INT,
    @ShippingAddress NVARCHAR(255),
    @PaymentMethod NVARCHAR(50),
    @ItemsJson NVARCHAR(MAX),
    @NewOrderId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId AND [IsActive] = 1)
    BEGIN
        RAISERROR(N'User with ID %d does not exist or is inactive.', 16, 1, @UserId);
        RETURN;
    END

    CREATE TABLE #OrderItems (
        [ProductId] INT,
        [Quantity] INT
    );

    INSERT INTO #OrderItems ([ProductId], [Quantity])
    SELECT [ProductId], [Quantity]
    FROM OPENJSON(@ItemsJson)
    WITH (
        [ProductId] INT '$.ProductId',
        [Quantity] INT '$.Quantity'
    );

    IF NOT EXISTS (SELECT 1 FROM #OrderItems)
    BEGIN
        IF OBJECT_ID('tempdb..#OrderItems') IS NOT NULL DROP TABLE #OrderItems;
        RAISERROR(N'Order must contain at least one valid item.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (
            SELECT 1 FROM #OrderItems oi
            WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Products] p WHERE p.[Id] = oi.[ProductId])
        )
        BEGIN
            THROW 50001, N'One or more requested products do not exist.', 1;
        END

        DECLARE @InsufficientProduct NVARCHAR(200);
        DECLARE @ReqQuantity INT;
        DECLARE @AvailStock INT;

        SELECT TOP 1
            @InsufficientProduct = p.[Name],
            @ReqQuantity = oi.[Quantity],
            @AvailStock = p.[StockQuantity]
        FROM #OrderItems oi
        JOIN [dbo].[Products] p WITH (UPDLOCK, ROWLOCK) ON p.[Id] = oi.[ProductId]
        WHERE p.[IsActive] = 0 OR p.[StockQuantity] < oi.[Quantity];

        IF @InsufficientProduct IS NOT NULL
        BEGIN
            DECLARE @ErrorMessage NVARCHAR(500) = 
                CONCAT(N'Product ""', @InsufficientProduct, N'"" does not have enough stock. Available: ', @AvailStock, N', Requested: ', @ReqQuantity);
            THROW 50002, @ErrorMessage, 1;
        END

        DECLARE @CalculatedTotal DECIMAL(18, 2);
        SELECT @CalculatedTotal = SUM(oi.[Quantity] * p.[Price])
        FROM #OrderItems oi
        JOIN [dbo].[Products] p ON p.[Id] = oi.[ProductId];

        DECLARE @OrderNumber NVARCHAR(50) = 
            CONCAT('ORD-', CONVERT(NVARCHAR(8), GETUTCDATE(), 112), '-', UPPER(SUBSTRING(CONVERT(NVARCHAR(36), NEWID()), 1, 8)));

        INSERT INTO [dbo].[Orders] (
            [OrderNumber],
            [UserId],
            [OrderDate],
            [TotalAmount],
            [Status],
            [ShippingAddress],
            [PaymentMethod],
            [PaymentStatus]
        )
        VALUES (
            @OrderNumber,
            @UserId,
            GETUTCDATE(),
            @CalculatedTotal,
            0,
            @ShippingAddress,
            @PaymentMethod,
            N'Unpaid'
        );

        SET @NewOrderId = CAST(SCOPE_IDENTITY() AS INT);

        INSERT INTO [dbo].[OrderDetails] (
            [OrderId],
            [ProductId],
            [Quantity],
            [UnitPrice],
            [SubTotal]
        )
        SELECT 
            @NewOrderId,
            oi.[ProductId],
            oi.[Quantity],
            p.[Price],
            (oi.[Quantity] * p.[Price])
        FROM #OrderItems oi
        JOIN [dbo].[Products] p ON p.[Id] = oi.[ProductId];

        UPDATE p
        SET p.[StockQuantity] = p.[StockQuantity] - oi.[Quantity]
        FROM [dbo].[Products] p
        JOIN #OrderItems oi ON oi.[ProductId] = p.[Id];

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        THROW;
    END CATCH;

    IF OBJECT_ID('tempdb..#OrderItems') IS NOT NULL
        DROP TABLE #OrderItems;
END;
";

        await _context.Database.ExecuteSqlRawAsync(sql);
        _logger.LogInformation("Stored procedure [dbo].[sp_CreateOrderWithStockDeduction] successfully registered.");
    }
}
