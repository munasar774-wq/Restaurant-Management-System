SET QUOTED_IDENTIFIER ON;
USE PizzaResturentandDrinkDb;

-- AspNetUsers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE AspNetUsers ADD CreatedDate datetime2 NOT NULL DEFAULT GETDATE();
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IsActive')
BEGIN
    ALTER TABLE AspNetUsers ADD IsActive bit NOT NULL DEFAULT 1;
END

-- Categories
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'Description')
BEGIN
    ALTER TABLE Categories ADD Description nvarchar(max) NULL;
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND name = 'IsActive')
BEGIN
    ALTER TABLE Categories ADD IsActive bit NOT NULL DEFAULT 1;
END

-- Tables
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tables]') AND name = 'Number')
BEGIN
    EXEC sp_rename 'Tables.Number', 'TableNumber', 'COLUMN';
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tables]') AND name = 'Description')
BEGIN
    ALTER TABLE Tables ADD Description nvarchar(100) NULL;
END

-- Orders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'Notes')
BEGIN
    ALTER TABLE Orders ADD Notes nvarchar(500) NULL;
END

-- OrderItems
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND name = 'Price')
BEGIN
    EXEC sp_rename 'OrderItems.Price', 'UnitPrice', 'COLUMN';
END
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND name = 'SpecialInstructions')
BEGIN
    ALTER TABLE OrderItems ADD SpecialInstructions nvarchar(200) NULL;
END

-- Payments
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
BEGIN
    CREATE TABLE Payments (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId int NOT NULL,
        Amount decimal(18,2) NOT NULL,
        PaymentMethod int NOT NULL,
        PaymentDate datetime2 NOT NULL,
        ProcessedByUserId nvarchar(450) NOT NULL,
        CONSTRAINT FK_Payments_Orders_OrderId FOREIGN KEY (OrderId) REFERENCES Orders (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Payments_AspNetUsers_ProcessedByUserId FOREIGN KEY (ProcessedByUserId) REFERENCES AspNetUsers (Id)
    );
    CREATE INDEX IX_Payments_OrderId ON Payments (OrderId);
END
