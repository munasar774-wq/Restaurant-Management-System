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
    /// Manages employees - Admin only functionality
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<EmployeeController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// List all employees
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var employeeList = new List<(ApplicationUser User, string Role)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                employeeList.Add((user, roles.FirstOrDefault() ?? "No Role"));
            }

            return View(employeeList);
        }

        /// <summary>
        /// Display create employee form
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(new[] { "Admin", "Employee" });
            return View(new EmployeeViewModel());
        }

        /// <summary>
        /// Create a new employee
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    ViewBag.Roles = new SelectList(new[] { "Admin", "Employee" });
                    return View(model);
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    IsActive = model.IsActive,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    // Assign role
                    await _userManager.AddToRoleAsync(user, model.Role);
                    _logger.LogInformation("Created new employee: {Email} with role {Role}", model.Email, model.Role);
                    
                    TempData["Success"] = "Employee created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(new[] { "Admin", "Employee" });
            return View(model);
        }

        /// <summary>
        /// Display edit employee form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new EmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? "Employee",
                IsActive = user.IsActive
            };

            ViewBag.Roles = new SelectList(new[] { "Admin", "Employee" }, model.Role);
            return View(model);
        }

        /// <summary>
        /// Update employee details
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel model)
        {
            // Remove password validation for edit (password is optional)
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id!);
                if (user == null)
                {
                    return NotFound();
                }

                // Update user properties
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                        await _userManager.ResetPasswordAsync(user, token, model.Password);
                    }

                    // Update role
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);

                    TempData["Success"] = "Employee updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(new[] { "Admin", "Employee" });
            return View(model);
        }

        /// <summary>
        /// Display delete confirmation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// <summary>
        /// Delete employee
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Don't allow deleting the current user
            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Deleted employee: {Email}", user.Email);
                TempData["Success"] = "Employee deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete employee.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Display assign role form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new AssignRoleViewModel
            {
                UserId = user.Id,
                UserName = user.FullName,
                CurrentRole = currentRoles.FirstOrDefault() ?? "None",
                AvailableRoles = allRoles!
            };

            return View(model);
        }

        /// <summary>
        /// Assign new role to employee
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(AssignRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                // Remove current roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add new role
                await _userManager.AddToRoleAsync(user, model.SelectedRole);

                TempData["Success"] = $"Role updated successfully for {user.FullName}!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
