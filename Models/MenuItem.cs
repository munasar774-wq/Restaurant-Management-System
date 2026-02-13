using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Represents a food or drink item on the menu
    /// </summary>
    public class MenuItem
    {
        public int Id { get; set; }

        // Name of the menu item
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        // Description of the menu item
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        // Price of the item
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10,000")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        // Foreign key to the category
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // Indicates if the item is currently available
        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        // Optional image URL for the menu item
        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        // Navigation property for the category
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Navigation property for order items containing this menu item
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
