using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    [Authorize(Roles = "Admin,KitchenStaff")]
    public class RecipeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recipe
        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            // Calculate recipe status (has ingredients or not)
            ViewBag.RecipeCounts = await _context.RecipeIngredients
                .GroupBy(r => r.MenuItemId)
                .Select(g => new { MenuItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.MenuItemId, v => v.Count);

            return View(menuItems);
        }

        // GET: Recipe/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            var recipeIngredients = await _context.RecipeIngredients
                .Include(r => r.InventoryItem)
                .Where(r => r.MenuItemId == id)
                .ToListAsync();

            ViewBag.MenuItem = menuItem;
            ViewBag.Ingredients = recipeIngredients;
            
            // Dropdown for adding new ingredient
            ViewBag.InventoryItems = new SelectList(_context.InventoryItems.OrderBy(i => i.Name), "Id", "Name");

            return View();
        }

        // POST: Recipe/AddIngredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient(int menuItemId, int inventoryItemId, double quantity)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than 0.";
                return RedirectToAction(nameof(Edit), new { id = menuItemId });
            }

            // Check if already exists
            var exists = await _context.RecipeIngredients
                .AnyAsync(r => r.MenuItemId == menuItemId && r.InventoryItemId == inventoryItemId);

            if (exists)
            {
                TempData["Error"] = "This ingredient is already in the recipe.";
                return RedirectToAction(nameof(Edit), new { id = menuItemId });
            }

            var recipeIngredient = new RecipeIngredient
            {
                MenuItemId = menuItemId,
                InventoryItemId = inventoryItemId,
                QuantityRequired = quantity
            };

            _context.RecipeIngredients.Add(recipeIngredient);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ingredient added successfully.";
            return RedirectToAction(nameof(Edit), new { id = menuItemId });
        }

        // POST: Recipe/RemoveIngredient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveIngredient(int id)
        {
            var recipeIngredient = await _context.RecipeIngredients.FindAsync(id);
            if (recipeIngredient != null)
            {
                int menuItemId = recipeIngredient.MenuItemId;
                _context.RecipeIngredients.Remove(recipeIngredient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ingredient removed from recipe.";
                return RedirectToAction(nameof(Edit), new { id = menuItemId });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
