SET QUOTED_IDENTIFIER ON;
USE PizzaResturentandDrinkDb;

-- Fix TableNumber Schema Mismatch

-- 1. Clean data: Remove 'T' prefix (assuming format T1, T2...)
-- Check if any values are non-numeric after removing T?
-- If conversion fails, this script will fail.

BEGIN TRANSACTION;

-- Remove 'T' or 't'
UPDATE Tables 
SET TableNumber = REPLACE(REPLACE(TableNumber, 'T', ''), 't', '')
WHERE TableNumber LIKE '%T%' OR TableNumber LIKE '%t%';

-- 2. Alter column to INT
-- This will fail if there are still non-numeric values
ALTER TABLE Tables ALTER COLUMN TableNumber int NOT NULL;

COMMIT TRANSACTION;
