using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Represents an ingredient or item in inventory
    /// </summary>
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100)]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 10000)]
        [Display(Name = "Quantity In Stock")]
        public double Quantity { get; set; }

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = "kg"; // kg, liters, pcs, etc.

        [Display(Name = "Low Stock Threshold")]
        public double LowStockThreshold { get; set; } = 5.0;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Display(Name = "Category")]
        public string Category { get; set; } = "General"; // Vegetable, Meat, Dairy, etc.
    }
}
