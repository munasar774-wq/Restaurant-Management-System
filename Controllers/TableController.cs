using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Manages restaurant tables
    /// </summary>
    [Authorize]
    public class TableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TableController> _logger;

        public TableController(ApplicationDbContext context, ILogger<TableController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// List all tables with their status
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var tables = await _context.Tables
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return View(tables);
        }

        /// <summary>
        /// Display create table form - Admin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Create a new table - Admin only
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Table table)
        {
            if (ModelState.IsValid)
            {
                // Check if table number already exists
                if (await _context.Tables.AnyAsync(t => t.TableNumber == table.TableNumber))
                {
                    ModelState.AddModelError("TableNumber", "This table number already exists.");
                    return View(table);
                }

                _context.Add(table);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created table number: {Number}", table.TableNumber);
                TempData["Success"] = "Table created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(table);
        }

        /// <summary>
        /// Display edit table form - Admin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        /// <summary>
        /// Update table - Admin only
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Table table)
        {
            if (id != table.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if new table number conflicts with existing
                    if (await _context.Tables.AnyAsync(t => t.TableNumber == table.TableNumber && t.Id != id))
                    {
                        ModelState.AddModelError("TableNumber", "This table number already exists.");
                        return View(table);
                    }

                    _context.Update(table);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Table updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TableExists(table.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(table);
        }

        /// <summary>
        /// Display delete confirmation - Admin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            return View(table);
        }

        /// <summary>
        /// Delete table - Admin only
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                // Check if table has active orders
                var hasActiveOrders = await _context.Orders
                    .AnyAsync(o => o.TableId == id && 
                        (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready));

                if (hasActiveOrders)
                {
                    TempData["Error"] = "Cannot delete table with active orders.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Table deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Toggle table occupancy status
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleOccupancy(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                table.IsOccupied = !table.IsOccupied;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Table {table.TableNumber} is now {(table.IsOccupied ? "occupied" : "available")}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TableExists(int id)
        {
            return _context.Tables.Any(e => e.Id == id);
        }
    }
}
