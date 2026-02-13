SET QUOTED_IDENTIFIER ON;
USE PizzaResturentandDrinkDb;

-- Fix Payments Table Schema Mismatches

-- Rename Date -> PaymentDate
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'Date')
BEGIN
    EXEC sp_rename 'Payments.Date', 'PaymentDate', 'COLUMN';
END

-- Rename Method -> PaymentMethod
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'Method')
BEGIN
    EXEC sp_rename 'Payments.Method', 'PaymentMethod', 'COLUMN';
END

-- Add ProcessedByUserId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'ProcessedByUserId')
BEGIN
    ALTER TABLE Payments ADD ProcessedByUserId nvarchar(450) NULL;
END
