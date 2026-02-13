SET QUOTED_IDENTIFIER ON;
USE PizzaResturentandDrinkDb;

-- Fix PaymentStatus column in Orders table
-- Problem: It is NOT NULL regarding the database schema, but the application code does not know about it and doesn't insert a value.
-- Solution: Add a default constraint so it defaults to 0 (Pending/Unpaid) when not specified.

-- Check if constraint already exists (to avoid error)
IF NOT EXISTS (SELECT * FROM sys.default_constraints WHERE parent_object_id = OBJECT_ID('Orders') AND parent_column_id = COLUMNPROPERTY(OBJECT_ID('Orders'), 'PaymentStatus', 'ColumnId'))
BEGIN
    ALTER TABLE Orders ADD CONSTRAINT DF_Orders_PaymentStatus DEFAULT 0 FOR PaymentStatus;
END
