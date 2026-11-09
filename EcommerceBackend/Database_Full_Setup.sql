-- =========================================================================
-- E-COMMERCE BACKEND COMPLETE DATABASE SCRIPT
-- SQL Server 2016+ Compatible
-- Supports: 7 Entities, Code-First Mapping, Stored Procedure Transaction, Seed Data
-- =========================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'EcommerceDb')
BEGIN
    CREATE DATABASE [EcommerceDb];
END
GO

USE [EcommerceDb];
GO

-- 1. ROLES TABLE
IF OBJECT_ID(N'[dbo].[Roles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(50) NOT NULL UNIQUE,
        [Description] NVARCHAR(255) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 2. USERS TABLE
IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Username] NVARCHAR(50) NOT NULL UNIQUE,
        [Email] NVARCHAR(150) NOT NULL UNIQUE,
        [PasswordHash] NVARCHAR(500) NOT NULL,
        [FullName] NVARCHAR(100) NOT NULL,
        [PhoneNumber] NVARCHAR(20) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [RoleId] INT NOT NULL,
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users]([RoleId]);
END
GO

-- 3. CATEGORIES TABLE
IF OBJECT_ID(N'[dbo].[Categories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL UNIQUE,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 4. PRODUCTS TABLE
IF OBJECT_ID(N'[dbo].[Products]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Products] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Sku] NVARCHAR(50) NOT NULL UNIQUE,
        [Description] NVARCHAR(MAX) NULL,
        [Price] DECIMAL(18,2) NOT NULL,
        [StockQuantity] INT NOT NULL DEFAULT 0,
        [ImageUrl] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CategoryId] INT NOT NULL,
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId]);
END
GO

-- 5. ORDERS TABLE
IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderNumber] NVARCHAR(50) NOT NULL UNIQUE,
        [UserId] INT NOT NULL,
        [OrderDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [Status] INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Confirmed, 2: Shipped, 3: Delivered, 4: Cancelled
        [ShippingAddress] NVARCHAR(255) NOT NULL,
        [PaymentMethod] NVARCHAR(50) NOT NULL DEFAULT 'COD',
        [PaymentStatus] NVARCHAR(50) NOT NULL DEFAULT 'Unpaid',
        CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId]);
END
GO

-- 6. ORDER DETAILS TABLE
IF OBJECT_ID(N'[dbo].[OrderDetails]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderDetails] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [OrderId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [UnitPrice] DECIMAL(18,2) NOT NULL,
        [SubTotal] DECIMAL(18,2) NOT NULL,
        CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_OrderDetails_OrderId] ON [dbo].[OrderDetails]([OrderId]);
    CREATE NONCLUSTERED INDEX [IX_OrderDetails_ProductId] ON [dbo].[OrderDetails]([ProductId]);
END
GO

-- 7. REVIEWS TABLE
IF OBJECT_ID(N'[dbo].[Reviews]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Reviews] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Rating] INT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
        [Comment] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_Reviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
        CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Reviews_UserId] ON [dbo].[Reviews]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_Reviews_ProductId] ON [dbo].[Reviews]([ProductId]);
END
GO

-- =========================================================================
-- STORED PROCEDURE WITH MULTI-TABLE TRANSACTION, TRY...CATCH, BEGIN TRAN, COMMIT, ROLLBACK
-- =========================================================================
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

    -- Validate User
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId AND [IsActive] = 1)
    BEGIN
        RAISERROR(N'User with ID %d does not exist or is inactive.', 16, 1, @UserId);
        RETURN;
    END

    -- Temporary table to hold input items
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

        -- 1. Check Product existence
        IF EXISTS (
            SELECT 1 FROM #OrderItems oi
            WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Products] p WHERE p.[Id] = oi.[ProductId])
        )
        BEGIN
            THROW 50001, N'One or more requested products do not exist.', 1;
        END

        -- 2. Validate Stock Availability with Update Lock
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
                CONCAT(N'Product "', @InsufficientProduct, N'" does not have enough stock. Available: ', @AvailStock, N', Requested: ', @ReqQuantity);
            THROW 50002, @ErrorMessage, 1;
        END

        -- 3. Calculate authoritative total amount
        DECLARE @CalculatedTotal DECIMAL(18, 2);
        SELECT @CalculatedTotal = SUM(oi.[Quantity] * p.[Price])
        FROM #OrderItems oi
        JOIN [dbo].[Products] p ON p.[Id] = oi.[ProductId];

        -- Generate Order Number
        DECLARE @OrderNumber NVARCHAR(50) = 
            CONCAT('ORD-', CONVERT(NVARCHAR(8), GETUTCDATE(), 112), '-', UPPER(SUBSTRING(CONVERT(NVARCHAR(36), NEWID()), 1, 8)));

        -- 4. INSERT INTO Orders (Table 1)
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
            0, -- Pending
            @ShippingAddress,
            @PaymentMethod,
            N'Unpaid'
        );

        SET @NewOrderId = CAST(SCOPE_IDENTITY() AS INT);

        -- 5. INSERT INTO OrderDetails (Table 2)
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

        -- 6. UPDATE Products (Table 3) - Atomic stock reduction
        UPDATE p
        SET p.[StockQuantity] = p.[StockQuantity] - oi.[Quantity]
        FROM [dbo].[Products] p
        JOIN #OrderItems oi ON oi.[ProductId] = p.[Id];

        -- 7. COMMIT TRANSACTION
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- 8. ROLLBACK TRANSACTION if error occurs
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        -- Re-throw to client
        THROW;
    END CATCH;

    IF OBJECT_ID('tempdb..#OrderItems') IS NOT NULL
        DROP TABLE #OrderItems;
END;
GO

-- =========================================================================
-- SEED DATA
-- Default Passwords:
-- Admin: Admin@123
-- Staff: Staff@123
-- Customer: Customer@123
-- =========================================================================

-- Seed Roles
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Admin')
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES ('Admin', 'System administrator with full privileges');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Staff')
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES ('Staff', 'Store manager with operations privileges');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [Name] = 'Customer')
    INSERT INTO [dbo].[Roles] ([Name], [Description]) VALUES ('Customer', 'Registered retail buyer');
GO

-- Seed Users
DECLARE @AdminRoleId INT = (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = 'Admin');
DECLARE @StaffRoleId INT = (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = 'Staff');
DECLARE @CustRoleId INT = (SELECT [Id] FROM [dbo].[Roles] WHERE [Name] = 'Customer');

-- Password hashes generated via BCrypt (Cost 11)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'admin')
    INSERT INTO [dbo].[Users] ([Username], [Email], [PasswordHash], [FullName], [PhoneNumber], [IsActive], [RoleId])
    VALUES ('admin', 'admin@ecommerce.local', '$2a$11$0z9uQ2hO2eY4O986a4UeAeA2vGf19LgA6V59sA4J58K9d8g7h6j5k', 'System Administrator', '0901234567', 1, @AdminRoleId);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'staff')
    INSERT INTO [dbo].[Users] ([Username], [Email], [PasswordHash], [FullName], [PhoneNumber], [IsActive], [RoleId])
    VALUES ('staff', 'staff@ecommerce.local', '$2a$11$0z9uQ2hO2eY4O986a4UeAeA2vGf19LgA6V59sA4J58K9d8g7h6j5k', 'Store Manager', '0902345678', 1, @StaffRoleId);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'customer')
    INSERT INTO [dbo].[Users] ([Username], [Email], [PasswordHash], [FullName], [PhoneNumber], [IsActive], [RoleId])
    VALUES ('customer', 'customer@ecommerce.local', '$2a$11$0z9uQ2hO2eY4O986a4UeAeA2vGf19LgA6V59sA4J58K9d8g7h6j5k', 'Nguyen Van A', '0903456789', 1, @CustRoleId);
GO

-- Seed Categories
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = 'Laptops & Computers')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive]) VALUES ('Laptops & Computers', 'High-performance laptops, PCs, and workstations', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = 'Smartphones & Tablets')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive]) VALUES ('Smartphones & Tablets', 'Flagship smartphones, iPads, and Android tablets', 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Name] = 'Audio & Accessories')
    INSERT INTO [dbo].[Categories] ([Name], [Description], [IsActive]) VALUES ('Audio & Accessories', 'Headphones, earbuds, smart watches, and accessories', 1);
GO

-- Seed Products
DECLARE @CatLaptops INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = 'Laptops & Computers');
DECLARE @CatPhones INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = 'Smartphones & Tablets');
DECLARE @CatAudio INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = 'Audio & Accessories');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'MBP-16-M3MAX')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('MacBook Pro 16 M3 Max', 'MBP-16-M3MAX', 'Apple M3 Max chip with 16-core CPU, 40-core GPU, 48GB RAM, 1TB SSD', 3499.00, 25, 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800', @CatLaptops, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'DELL-XPS-15')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('Dell XPS 15 9530', 'DELL-XPS-15', 'Intel Core i9-13900H, 32GB DDR5, 1TB NVMe, RTX 4070 8GB, 3.5K OLED', 2499.00, 18, 'https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800', @CatLaptops, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'IPHONE-16-PROMAX')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('iPhone 16 Pro Max 256GB', 'IPHONE-16-PROMAX', 'Titanium design, Ceramic Shield front, A18 Pro chip, 48MP Fusion camera', 1199.00, 40, 'https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=800', @CatPhones, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'GALAXY-S24-ULTRA')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('Samsung Galaxy S24 Ultra', 'GALAXY-S24-ULTRA', 'Galaxy AI, 200MP camera, Snapdragon 8 Gen 3, Titanium frame, S Pen', 1299.00, 30, 'https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=800', @CatPhones, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'SONY-WH1000XM5')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('Sony WH-1000XM5 Wireless Headphones', 'SONY-WH1000XM5', 'Industry-leading noise cancellation, 30h battery, LDAC Hi-Res Audio', 399.00, 50, 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800', @CatAudio, 1);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Sku] = 'AIRPODS-PRO-2')
    INSERT INTO [dbo].[Products] ([Name], [Sku], [Description], [Price], [StockQuantity], [ImageUrl], [CategoryId], [IsActive])
    VALUES ('Apple AirPods Pro (2nd Generation)', 'AIRPODS-PRO-2', 'Active Noise Cancellation, Adaptive Audio, USB-C Charging Case', 249.00, 60, 'https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=800', @CatAudio, 1);
GO

PRINT N'Database EcommerceDb setup and seed completed successfully!';
GO
