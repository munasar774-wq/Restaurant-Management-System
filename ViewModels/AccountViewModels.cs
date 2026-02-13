using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.ViewModels
{
    /// <summary>
    /// ViewModel for user login
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel for creating/editing employees
    /// </summary>
    public class EmployeeViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "Employee";

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// ViewModel for assigning roles to users
    /// </summary>
    public class AssignRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string SelectedRole { get; set; } = string.Empty;
        
        public List<string> AvailableRoles { get; set; } = new List<string>();
    }
}
