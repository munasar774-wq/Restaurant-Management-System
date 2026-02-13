using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Models;

namespace RestaurantManagement.Data
{
    /// <summary>
    /// Initializes the database with default data including roles and admin user
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Seeds the database with default roles and admin account
        /// </summary>
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Ensure schema contains custom properties
            await EnsureSchema(context);

            // Seed roles
            string[] roles = { "Admin", "Employee", "KitchenStaff" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed default admin user
            var adminEmail = "admin@restaurant.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed default kitchen staff user
            var kitchenEmail = "chef@restaurant.com";
            var kitchenUser = await userManager.FindByEmailAsync(kitchenEmail);

            if (kitchenUser == null)
            {
                kitchenUser = new ApplicationUser
                {
                    UserName = kitchenEmail,
                    Email = kitchenEmail,
                    FullName = "Head Chef",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(kitchenUser, "Chef@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(kitchenUser, "KitchenStaff");
                }
            }

            // Seed default categories if none exist
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Appetizers", Description = "Start your meal with these delicious appetizers", IsActive = true },
                    new Category { Name = "Main Course", Description = "Our signature main dishes", IsActive = true },
                    new Category { Name = "Desserts", Description = "Sweet treats to end your meal", IsActive = true },
                    new Category { Name = "Beverages", Description = "Refreshing drinks", IsActive = true }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed default tables if none exist
            if (!context.Tables.Any())
            {
                var tables = new List<Table>
                {
                    new Table { TableNumber = 1, Capacity = 2, Description = "Window seat", IsOccupied = false },
                    new Table { TableNumber = 2, Capacity = 4, Description = "Center table", IsOccupied = false },
                    new Table { TableNumber = 3, Capacity = 4, Description = "Near entrance", IsOccupied = false },
                    new Table { TableNumber = 4, Capacity = 6, Description = "Family table", IsOccupied = false },
                    new Table { TableNumber = 5, Capacity = 8, Description = "Party table", IsOccupied = false }
                };
                context.Tables.AddRange(tables);
                await context.SaveChangesAsync();
            }

            // Seed sample menu items if none exist
            if (!context.MenuItems.Any())
            {
                var categories = context.Categories.ToList();
                var menuItems = new List<MenuItem>
                {
                    // Appetizers
                    new MenuItem { Name = "Spring Rolls", Description = "Crispy vegetable spring rolls", Price = 5.99m, CategoryId = categories.First(c => c.Name == "Appetizers").Id, IsAvailable = true },
                    new MenuItem { Name = "Garlic Bread", Description = "Toasted bread with garlic butter", Price = 4.50m, CategoryId = categories.First(c => c.Name == "Appetizers").Id, IsAvailable = true },
                    
                    // Main Course
                    new MenuItem { Name = "Grilled Chicken", Description = "Herb-marinated grilled chicken breast", Price = 15.99m, CategoryId = categories.First(c => c.Name == "Main Course").Id, IsAvailable = true },
                    new MenuItem { Name = "Beef Steak", Description = "Premium beef steak with sides", Price = 22.99m, CategoryId = categories.First(c => c.Name == "Main Course").Id, IsAvailable = true },
                    new MenuItem { Name = "Pasta Carbonara", Description = "Classic Italian pasta", Price = 13.50m, CategoryId = categories.First(c => c.Name == "Main Course").Id, IsAvailable = true },
                    
                    // Desserts
                    new MenuItem { Name = "Chocolate Cake", Description = "Rich chocolate layer cake", Price = 6.99m, CategoryId = categories.First(c => c.Name == "Desserts").Id, IsAvailable = true },
                    new MenuItem { Name = "Ice Cream", Description = "Three scoops of your choice", Price = 4.99m, CategoryId = categories.First(c => c.Name == "Desserts").Id, IsAvailable = true },
                    
                    // Beverages
                    new MenuItem { Name = "Fresh Orange Juice", Description = "Freshly squeezed orange juice", Price = 3.99m, CategoryId = categories.First(c => c.Name == "Beverages").Id, IsAvailable = true },
                    new MenuItem { Name = "Coffee", Description = "Hot brewed coffee", Price = 2.50m, CategoryId = categories.First(c => c.Name == "Beverages").Id, IsAvailable = true },
                    new MenuItem { Name = "Soft Drink", Description = "Variety of soft drinks", Price = 2.00m, CategoryId = categories.First(c => c.Name == "Beverages").Id, IsAvailable = true }
                };
                context.MenuItems.AddRange(menuItems);
                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureSchema(ApplicationDbContext context)
        {
            try
            {
                // Check and add FullName
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'FullName' AND Object_ID = Object_ID(N'AspNetUsers'))
                    BEGIN
                        ALTER TABLE AspNetUsers ADD FullName NVARCHAR(100) NOT NULL DEFAULT '';
                    END
                ");

                // Check and add CreatedDate
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CreatedDate' AND Object_ID = Object_ID(N'AspNetUsers'))
                    BEGIN
                        ALTER TABLE AspNetUsers ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE();
                    END
                ");

                // Check and add IsActive
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'IsActive' AND Object_ID = Object_ID(N'AspNetUsers'))
                    BEGIN
                        ALTER TABLE AspNetUsers ADD IsActive BIT NOT NULL DEFAULT 1;
                    END
                ");

                // Check and add InventoryItems table
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[InventoryItems]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [InventoryItems] (
                            [Id] int NOT NULL IDENTITY,
                            [Name] nvarchar(100) NOT NULL,
                            [Quantity] float NOT NULL,
                            [Unit] nvarchar(20) NOT NULL,
                            [LowStockThreshold] float NOT NULL,
                            [LastUpdated] datetime2 NOT NULL,
                            [Category] nvarchar(max) NOT NULL,
                            CONSTRAINT [PK_InventoryItems] PRIMARY KEY ([Id])
                        );
                    END
                ");

                // Check and add RecipeIngredients table
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[RecipeIngredients]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [RecipeIngredients] (
                            [Id] int NOT NULL IDENTITY,
                            [MenuItemId] int NOT NULL,
                            [InventoryItemId] int NOT NULL,
                            [QuantityRequired] float NOT NULL,
                            CONSTRAINT [PK_RecipeIngredients] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_RecipeIngredients_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_RecipeIngredients_InventoryItems_InventoryItemId] FOREIGN KEY ([InventoryItemId]) REFERENCES [InventoryItems] ([Id]) ON DELETE CASCADE
                        );
                        
                        CREATE INDEX [IX_RecipeIngredients_MenuItemId] ON [RecipeIngredients] ([MenuItemId]);
                        CREATE INDEX [IX_RecipeIngredients_InventoryItemId] ON [RecipeIngredients] ([InventoryItemId]);
                    END
                ");

                // Check and add Reservations table
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Reservations]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [Reservations] (
                            [Id] int NOT NULL IDENTITY,
                            [TableId] int NOT NULL,
                            [CustomerName] nvarchar(100) NOT NULL,
                            [CustomerPhone] nvarchar(20) NOT NULL,
                            [ReservationDate] datetime2 NOT NULL,
                            [Guests] int NOT NULL,
                            [Status] int NOT NULL,
                            [Notes] nvarchar(200) NULL,
                            CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_Reservations_Tables_TableId] FOREIGN KEY ([TableId]) REFERENCES [Tables] ([Id]) ON DELETE CASCADE
                        );
                        
                        CREATE INDEX [IX_Reservations_TableId] ON [Reservations] ([TableId]);
                    END
                ");
            }
            catch (Exception ex)
            {
                // Prepare for the case where table might not exist or other issues - though EnsureCreatedAsync should handle table existence.
                Console.WriteLine($"Schema update error: {ex.Message}");
            }
        }
    }
}
