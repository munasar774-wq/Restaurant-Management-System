using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Represents a food category (e.g., Appetizers, Main Course, Desserts, Beverages)
    /// </summary>
    public class Category
    {
        public int Id { get; set; }

        // Name of the category
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        // Optional description of the category
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }

        // Indicates if the category is active and should be displayed
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property for menu items in this category
        public virtual ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}
