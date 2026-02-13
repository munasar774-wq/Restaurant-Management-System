using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Join table linking a Menu Item to an Inventory Item (Ingredient)
    /// </summary>
    public class RecipeIngredient
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [Required]
        public int InventoryItemId { get; set; }

        [Required]
        [Range(0.001, 1000, ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity Required")]
        public double QuantityRequired { get; set; }

        [ForeignKey("MenuItemId")]
        public virtual MenuItem? MenuItem { get; set; }

        [ForeignKey("InventoryItemId")]
        public virtual InventoryItem? InventoryItem { get; set; }
    }
}
