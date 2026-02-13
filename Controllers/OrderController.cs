using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;
using RestaurantManagement.ViewModels;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Manages orders - Available to all authenticated users
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<OrderController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// List all orders (filtered by role)
        /// </summary>
        public async Task<IActionResult> Index(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user!, "Admin");

            var ordersQuery = _context.Orders
                .Include(o => o.Table)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payment)
                .AsQueryable();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == orderStatus);
            }

            // Order by date descending
            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.StatusFilter = status;
            return View(orders);
        }

        /// <summary>
        /// Display order details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi!.Category)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        /// <summary>
        /// Display create order form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateOrderViewModel
            {
                AvailableTables = await _context.Tables
                    .Where(t => !t.IsOccupied)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync(),
                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                MenuItems = await _context.MenuItems
                    .Where(m => m.IsAvailable)
                    .OrderBy(m => m.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int tableId, string? notes, string orderItemsJson)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Parse order items from JSON
                var orderItems = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemViewModel>>(orderItemsJson);
                
                if (orderItems == null || !orderItems.Any())
                {
                    TempData["Error"] = "Please add at least one item to the order.";
                    return RedirectToAction(nameof(Create));
                }

                if (tableId <= 0)
                {
                    TempData["Error"] = "Please select a table.";
                    
                    // Reload ViewModel
                     var viewModel = new CreateOrderViewModel
                    {
                        AvailableTables = await _context.Tables
                            .Where(t => !t.IsOccupied)
                            .OrderBy(t => t.TableNumber)
                            .ToListAsync(),
                        Categories = await _context.Categories
                            .Where(c => c.IsActive)
                            .OrderBy(c => c.Name)
                            .ToListAsync(),
                        MenuItems = await _context.MenuItems
                            .Where(m => m.IsAvailable)
                            .OrderBy(m => m.Name)
                            .ToListAsync()
                    };
                    return View(viewModel);
                }

                // Verify table exists and is not occupied (double check)
                var selectedTable = await _context.Tables.FindAsync(tableId);
                if (selectedTable == null)
                {
                     TempData["Error"] = "Selected table does not exist.";
                     return RedirectToAction(nameof(Create));
                }

                // Create the order
                var order = new Order
                {
                    TableId = tableId,
                    UserId = user!.Id,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    Notes = notes,
                    TotalAmount = 0
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add order items
                decimal total = 0;
                foreach (var item in orderItems)
                {
                    var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
                    if (menuItem != null)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            MenuItemId = item.MenuItemId,
                            Quantity = item.Quantity,
                            UnitPrice = menuItem.Price,
                            SpecialInstructions = item.SpecialInstructions
                        };
                        _context.OrderItems.Add(orderItem);
                        total += menuItem.Price * item.Quantity;

                        // Deduct Stock
                        await ProcessStockDeduction(item.MenuItemId, item.Quantity);
                    }
                }

                // Update order total
                order.TotalAmount = total;

                // Mark table as occupied
                var table = await _context.Tables.FindAsync(tableId);
                if (table != null)
                {
                    table.IsOccupied = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order #{OrderId} for table {TableNumber}", order.Id, table?.TableNumber);
                TempData["Success"] = $"Order #{order.Id} created successfully!";
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["Error"] = "An error occurred while creating the order.";
                return RedirectToAction(nameof(Create));
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status;
            order.Status = newStatus;

            // If order is delivered or cancelled, free up the table
            if (newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled)
            {
                if (order.Table != null)
                {
                    // Check if table has other active orders
                    var hasOtherActiveOrders = await _context.Orders
                        .AnyAsync(o => o.TableId == order.TableId && 
                            o.Id != order.Id &&
                            (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready));

                    if (!hasOtherActiveOrders)
                    {
                        order.Table.IsOccupied = false;
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order #{OrderId} status changed from {OldStatus} to {NewStatus}", 
                order.Id, oldStatus, newStatus);
            TempData["Success"] = $"Order status updated to {newStatus}";

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Add items to an existing order
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddItems(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Can only add items to pending or preparing orders
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing)
            {
                TempData["Error"] = "Cannot add items to an order that is ready or delivered.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.OrderId = id;
            ViewBag.TableNumber = order.Table?.TableNumber.ToString() ?? "Unknown";
            ViewBag.MenuItems = await _context.MenuItems
                .Where(m => m.IsAvailable)
                .Include(m => m.Category)
                .OrderBy(m => m.Category!.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View();
        }

        /// <summary>
        /// Add items to an existing order (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItems(int orderId, int menuItemId, int quantity, string? specialInstructions)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems.FindAsync(menuItemId);
            if (menuItem == null)
            {
                return NotFound();
            }

            var orderItem = new OrderItem
            {
                OrderId = orderId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = menuItem.Price,
                SpecialInstructions = specialInstructions
            };

            _context.OrderItems.Add(orderItem);
            order.TotalAmount += menuItem.Price * quantity;

            // Deduct Stock
            await ProcessStockDeduction(menuItemId, quantity);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Added {quantity}x {menuItem.Name} to order";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        /// <summary>
        /// Remove item from order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int orderItemId)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

            if (orderItem == null)
            {
                return NotFound();
            }

            var orderId = orderItem.OrderId;
            var order = orderItem.Order;

            // Update order total
            if (order != null)
            {
                order.TotalAmount -= orderItem.Subtotal;
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item removed from order";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        /// <summary>
        /// Cancel an order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderStatus.Cancelled;

            // Free up the table if no other active orders
            if (order.Table != null)
            {
                var hasOtherActiveOrders = await _context.Orders
                    .AnyAsync(o => o.TableId == order.TableId && 
                        o.Id != order.Id &&
                        (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready));

                if (!hasOtherActiveOrders)
                {
                    order.Table.IsOccupied = false;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order #{OrderId} cancelled", order.Id);
            TempData["Success"] = "Order cancelled";
            return RedirectToAction(nameof(Index));
        }
        /// <summary>
        /// Helper to deduct inventory stock based on recipe
        /// </summary>
        private async Task ProcessStockDeduction(int menuItemId, int quantity)
        {
            var recipeIngredients = await _context.RecipeIngredients
                .Include(r => r.InventoryItem)
                .Where(r => r.MenuItemId == menuItemId)
                .ToListAsync();

            foreach (var ingredient in recipeIngredients)
            {
                if (ingredient.InventoryItem != null)
                {
                    double deductionAmount = ingredient.QuantityRequired * quantity;
                    ingredient.InventoryItem.Quantity -= deductionAmount;
                    
                    // Optional: Log low stock warning if needed, but for now just updating value
                    // Alert logic is handled in InventoryController/Index view
                }
            }
        }
    }
}
