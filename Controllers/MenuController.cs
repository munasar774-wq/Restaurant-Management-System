using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Manages menu items and categories - Admin only
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MenuController> _logger;
        private readonly IWebHostEnvironment _environment;

        public MenuController(ApplicationDbContext context, ILogger<MenuController> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        #region Menu Items

        /// <summary>
        /// List all menu items
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category!.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();

            return View(menuItems);
        }

        /// <summary>
        /// Display create menu item form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View();
        }

        /// <summary>
        /// Create a new menu item with image upload
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem menuItem, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    menuItem.ImageUrl = await SaveImageAsync(imageFile);
                }

                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created menu item: {Name}", menuItem.Name);
                TempData["Success"] = "Menu item created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        /// <summary>
        /// Display edit menu item form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        /// <summary>
        /// Update menu item with optional new image
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MenuItem menuItem, IFormFile? imageFile)
        {
            if (id != menuItem.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing item to preserve image if no new one uploaded
                    var existingItem = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                    
                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingItem?.ImageUrl))
                        {
                            DeleteImage(existingItem.ImageUrl);
                        }
                        menuItem.ImageUrl = await SaveImageAsync(imageFile);
                    }
                    else
                    {
                        // Keep existing image
                        menuItem.ImageUrl = existingItem?.ImageUrl;
                    }

                    _context.Update(menuItem);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated menu item: {Name}", menuItem.Name);
                    TempData["Success"] = "Menu item updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenuItemExists(menuItem.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.Where(c => c.IsActive).ToListAsync(), "Id", "Name", menuItem.CategoryId);
            return View(menuItem);
        }

        /// <summary>
        /// Display delete confirmation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var menuItem = await _context.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
            {
                return NotFound();
            }

            return View(menuItem);
        }

        /// <summary>
        /// Delete menu item and its image
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                // Delete associated image
                if (!string.IsNullOrEmpty(menuItem.ImageUrl))
                {
                    DeleteImage(menuItem.ImageUrl);
                }

                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted menu item: {Name}", menuItem.Name);
                TempData["Success"] = "Menu item deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Toggle menu item availability
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem != null)
            {
                menuItem.IsAvailable = !menuItem.IsAvailable;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{menuItem.Name} is now {(menuItem.IsAvailable ? "available" : "unavailable")}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Save uploaded image and return the path
        /// </summary>
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "menu");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // Return relative path for storing in database
            return $"/uploads/menu/{uniqueFileName}";
        }

        /// <summary>
        /// Delete image from filesystem
        /// </summary>
        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }



        private bool MenuItemExists(int id)
        {
            return _context.MenuItems.Any(e => e.Id == id);
        }

        #endregion

        #region Categories

        /// <summary>
        /// List all categories
        /// </summary>
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.MenuItems)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        /// <summary>
        /// Display create category form
        /// </summary>
        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View();
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                // Check if category name already exists
                if (await _context.Categories.AnyAsync(c => c.Name == category.Name))
                {
                    ModelState.AddModelError("Name", "A category with this name already exists.");
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created category: {Name}", category.Name);
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Categories));
            }

            return View(category);
        }

        /// <summary>
        /// Display edit category form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        /// <summary>
        /// Update category
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Category updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Categories));
            }

            return View(category);
        }

        /// <summary>
        /// Display delete category confirmation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        /// <summary>
        /// Delete category
        /// </summary>
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category != null)
            {
                // Check if category has menu items
                if (category.MenuItems.Any())
                {
                    TempData["Error"] = "Cannot delete category with menu items. Remove menu items first.";
                    return RedirectToAction(nameof(Categories));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Category deleted successfully!";
            }

            return RedirectToAction(nameof(Categories));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }

        #endregion
    }
}
