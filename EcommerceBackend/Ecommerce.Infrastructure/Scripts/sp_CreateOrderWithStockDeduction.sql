-- =========================================================================
-- Business Stored Procedure with Multi-Table Writes & Atomic Transaction
-- Requirement: "Bắt buộc có tối thiểu 1 nghiệp vụ giao dịch ghi nhiều bảng 
-- sử dụng khối TRY...CATCH kết hợp BEGIN TRAN, COMMIT và ROLLBACK"
--
-- Description:
-- 1. Validates User existence and active status.
-- 2. Parses order items JSON payload: [{"ProductId": 1, "Quantity": 2}, ...]
-- 3. BEGIN TRAN within TRY...CATCH block.
-- 4. Checks product existence and stock availability.
-- 5. If out of stock, THROW exception triggering CATCH and ROLLBACK TRAN.
-- 6. Calculates total amount from authoritative product prices.
-- 7. Inserts into Orders table (Table 1).
-- 8. Inserts items into OrderDetails table (Table 2).
-- 9. Updates Products table to deduct stock quantity (Table 3).
-- 10. COMMIT TRAN on success, returns generated @NewOrderId.
-- 11. In CATCH: checks @@TRANCOUNT > 0 and executes ROLLBACK TRAN, then THROWs.
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

    -- 1. Validate User
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Id] = @UserId AND [IsActive] = 1)
    BEGIN
        RAISERROR(N'User with ID %d does not exist or is inactive.', 16, 1, @UserId);
        RETURN;
    END

    -- Temporary table to hold incoming requested items
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
        DROP TABLE #OrderItems;
        RAISERROR(N'Order must contain at least one valid item.', 16, 1);
        RETURN;
    END

    -- 2. Begin Transaction with TRY...CATCH
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Check if any Product does not exist
        IF EXISTS (
            SELECT 1 FROM #OrderItems oi
            WHERE NOT EXISTS (SELECT 1 FROM [dbo].[Products] p WHERE p.[Id] = oi.[ProductId])
        )
        BEGIN
            THROW 50001, N'One or more requested products do not exist.', 1;
        END

        -- Check if any Product is inactive or does not have sufficient stock
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

        -- 4. INSERT INTO [dbo].[Orders] (Table 1)
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
            0, -- Pending (OrderStatus.Pending)
            @ShippingAddress,
            @PaymentMethod,
            N'Unpaid'
        );

        SET @NewOrderId = CAST(SCOPE_IDENTITY() AS INT);

        -- 5. INSERT INTO [dbo].[OrderDetails] (Table 2)
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

        -- 6. UPDATE [dbo].[Products] to deduct stock (Table 3)
        UPDATE p
        SET p.[StockQuantity] = p.[StockQuantity] - oi.[Quantity]
        FROM [dbo].[Products] p
        JOIN #OrderItems oi ON oi.[ProductId] = p.[Id];

        -- 7. COMMIT TRANSACTION on success
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- 8. ROLLBACK TRANSACTION if an error occurred anywhere in the TRY block
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END;

        -- Re-throw the caught error back to the caller
        THROW;
    END CATCH;

    -- Clean up temporary table
    IF OBJECT_ID('tempdb..#OrderItems') IS NOT NULL
        DROP TABLE #OrderItems;
END;
