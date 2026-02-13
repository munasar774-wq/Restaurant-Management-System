CREATE TABLE [InventoryItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Quantity] float NOT NULL,
    [Unit] nvarchar(20) NOT NULL,
    [LowStockThreshold] float NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    [Category] nvarchar(max) NOT NULL DEFAULT N'General',
    CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id])
);
GO
