using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;
using RestaurantManagement.ViewModels;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Handles dashboard views for Admin and Employee roles
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Main dashboard - redirects to appropriate view based on user role
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if user is admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }

            return RedirectToAction("EmployeeDashboard");
        }

        /// <summary>
        /// Admin dashboard with comprehensive statistics
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var viewModel = new AdminDashboardViewModel
            {
                // Count all orders
                TotalOrders = await _context.Orders.CountAsync(),
                
                // Count pending orders
                PendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing)
                    .CountAsync(),

                // Count employees (users with Employee role)
                TotalEmployees = (await _userManager.GetUsersInRoleAsync("Employee")).Count,

                // Count menu items
                TotalMenuItems = await _context.MenuItems.Where(m => m.IsAvailable).CountAsync(),

                // Count tables
                AvailableTables = await _context.Tables.Where(t => !t.IsOccupied).CountAsync(),
                OccupiedTables = await _context.Tables.Where(t => t.IsOccupied).CountAsync(),

                // Calculate today's revenue from completed orders with payments
                TodayRevenue = await _context.Payments
                    .Where(p => p.PaymentDate.Date == today)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,

                // Calculate monthly revenue
                MonthlyRevenue = await _context.Payments
                    .Where(p => p.PaymentDate >= startOfMonth)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,

                // Get recent orders
                RecentOrders = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Employee dashboard with their relevant tasks
        /// </summary>
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> EmployeeDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.Today;

            var viewModel = new EmployeeDashboardViewModel
            {
                // Count orders created by this employee today
                MyOrdersToday = await _context.Orders
                    .Where(o => o.UserId == user!.Id && o.OrderDate.Date == today)
                    .CountAsync(),

                // Count pending orders
                PendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .CountAsync(),

                // Count ready orders
                ReadyOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Ready)
                    .CountAsync(),

                // Get active orders (not delivered or cancelled)
                ActiveOrders = await _context.Orders
                    .Include(o => o.Table)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                    .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(20)
                    .ToListAsync(),

                // Get available tables
                AvailableTables = await _context.Tables
                    .Where(t => !t.IsOccupied)
                    .OrderBy(t => t.TableNumber)
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}
