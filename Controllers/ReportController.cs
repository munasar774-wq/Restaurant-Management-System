using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;
using RestaurantManagement.ViewModels;
using System.Globalization;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Generates daily and monthly reports - Admin only
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Display report selection page
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Generate daily report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Daily(DateTime? date = null)
        {
            var reportDate = date ?? DateTime.Today;

            var orders = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi!.Category)
                .Include(o => o.Payment)
                .Where(o => o.OrderDate.Date == reportDate.Date)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.PaymentDate.Date == reportDate.Date)
                .ToListAsync();

            var viewModel = new DailyReportViewModel
            {
                Date = reportDate,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalRevenue = payments.Sum(p => p.Amount),
                CashPayments = payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount),
                CardPayments = payments.Where(p => p.PaymentMethod == PaymentMethod.Card).Sum(p => p.Amount),
                
                Orders = orders.Select(o => new OrderSummary
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TableNumber = o.Table?.TableNumber.ToString() ?? "N/A",
                    EmployeeName = o.User?.FullName ?? "Unknown",
                    Total = o.TotalAmount,
                    Status = o.Status.ToString(),
                    PaymentMethod = o.Payment?.PaymentMethod.ToString() ?? "Unpaid"
                }).ToList(),

                // Get top selling items for the day
                TopSellingItems = orders
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.MenuItem != null)
                    .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem!.Name, Category = oi.MenuItem.Category?.Name ?? "Unknown" })
                    .Select(g => new TopSellingItem
                    {
                        ItemName = g.Key.Name,
                        Category = g.Key.Category,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Subtotal)
                    })
                    .OrderByDescending(t => t.QuantitySold)
                    .Take(10)
                    .ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Generate monthly report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Monthly(int? year = null, int? month = null)
        {
            var reportYear = year ?? DateTime.Today.Year;
            var reportMonth = month ?? DateTime.Today.Month;

            var startDate = new DateTime(reportYear, reportMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi!.Category)
                .Include(o => o.Payment)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();

            var viewModel = new MonthlyReportViewModel
            {
                Year = reportYear,
                Month = reportMonth,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(reportMonth),
                TotalOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                TotalRevenue = payments.Sum(p => p.Amount),
                AverageOrderValue = orders.Any(o => o.Status == OrderStatus.Delivered) 
                    ? orders.Where(o => o.Status == OrderStatus.Delivered).Average(o => o.TotalAmount) 
                    : 0,

                // Daily breakdown
                DailySummaries = Enumerable.Range(1, DateTime.DaysInMonth(reportYear, reportMonth))
                    .Select(day =>
                    {
                        var dayDate = new DateTime(reportYear, reportMonth, day);
                        return new DailySummary
                        {
                            Date = dayDate,
                            OrderCount = orders.Count(o => o.OrderDate.Date == dayDate && o.Status == OrderStatus.Delivered),
                            Revenue = payments.Where(p => p.PaymentDate.Date == dayDate).Sum(p => p.Amount)
                        };
                    })
                    .ToList(),

                // Top selling items for the month
                TopSellingItems = orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.MenuItem != null)
                    .GroupBy(oi => new { oi.MenuItemId, oi.MenuItem!.Name, Category = oi.MenuItem.Category?.Name ?? "Unknown" })
                    .Select(g => new TopSellingItem
                    {
                        ItemName = g.Key.Name,
                        Category = g.Key.Category,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Subtotal)
                    })
                    .OrderByDescending(t => t.Revenue)
                    .Take(10)
                    .ToList(),

                // Employee performance
                EmployeePerformances = orders
                    .Where(o => o.Status == OrderStatus.Delivered && o.User != null)
                    .GroupBy(o => o.User!.FullName)
                    .Select(g => new EmployeePerformance
                    {
                        EmployeeName = g.Key,
                        OrdersProcessed = g.Count(),
                        TotalSales = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(e => e.TotalSales)
                    .ToList()
            };

            // Populate available months for selection
            ViewBag.AvailableMonths = Enumerable.Range(1, 12)
                .Select(m => new { Month = m, Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m) })
                .ToList();
            ViewBag.AvailableYears = Enumerable.Range(DateTime.Today.Year - 5, 6).ToList();

            return View(viewModel);
        }
    }
}
