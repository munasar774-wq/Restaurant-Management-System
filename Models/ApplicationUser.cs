using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Extended user model that includes additional properties for restaurant staff
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Full name of the employee
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // Date when the user account was created
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Indicates if the user account is active
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property for orders created by this user
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
