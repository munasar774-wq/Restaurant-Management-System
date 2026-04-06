using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RestaurantManagement.Models;
using RestaurantManagement.ViewModels;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Handles user authentication: login, logout, and access denied
    /// </summary>
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display the login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // Redirect if already logged in
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                
                // If user is authenticated but not in database, clear the stale cookie
                await _signInManager.SignOutAsync();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Process login request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(model.Email);
                
                if (user != null)
                {
                    // Check if user is active
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                        return View(model);
                    }

                    // Attempt to sign in
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                        
                        // Redirect to return URL or dashboard
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Dashboard");
                    }

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User {Email} account locked out.", model.Email);
                        ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
                        return View(model);
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return View(model);
        }

        /// <summary>
        /// Log out the current user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login", "Account");
        }

        /// <summary>
        /// Display access denied page
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Display user profile
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);
            
            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                CreatedDate = user.CreatedDate,
                Role = roles.FirstOrDefault() ?? "Unknown"
            };

            return View(model);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FullName = model.FullName;
            
            var emailResult = await _userManager.SetEmailAsync(user, model.Email);
            user.UserName = model.Email; // Keep UserName and Email synced
            
            var updateResult = await _userManager.UpdateAsync(user);

            if (emailResult.Succeeded && updateResult.Succeeded)
            {
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in emailResult.Errors.Concat(updateResult.Errors))
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Restore non-editable fields if validation fails
            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "Unknown";
            model.CreatedDate = user.CreatedDate;

            return View(model);
        }

        /// <summary>
        /// Display settings/change password page
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult Settings()
        {
            return View(new ChangePasswordViewModel());
        }

        /// <summary>
        /// Process change password request
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Settings");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        /// <summary>
        /// TEMPORARY: Reset admin password - DELETE THIS AFTER USE!
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetAdminPassword()
        {
            var adminEmails = new[] { "maska@gmail.com", "mascuud@gmail.com" };
            var newPassword = "Admin@123";
            var results = new List<string>();

            foreach (var email in adminEmails)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                    
                    if (result.Succeeded)
                    {
                        results.Add($"✅ Password reset for: {email}");
                    }
                    else
                    {
                        results.Add($"❌ Failed for {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            ViewBag.Results = results;
            ViewBag.NewPassword = newPassword;
            return View("PasswordReset");
        }
    }
}
