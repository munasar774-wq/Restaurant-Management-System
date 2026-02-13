using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Represents an individual item within an order
    /// </summary>
    public class OrderItem
    {
        public int Id { get; set; }

        // Foreign key to the order
        [Required]
        [Display(Name = "Order")]
        public int OrderId { get; set; }

        // Foreign key to the menu item
        [Required]
        [Display(Name = "Menu Item")]
        public int MenuItemId { get; set; }

        // Quantity of this item ordered
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;

        // Price per unit at the time of order (captures historical price)
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        // Calculated subtotal (Quantity * UnitPrice)
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal Subtotal => Quantity * UnitPrice;

        // Optional special instructions for this item
        [StringLength(200)]
        [Display(Name = "Special Instructions")]
        public string? SpecialInstructions { get; set; }

        // Navigation property for the order
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Navigation property for the menu item
        [ForeignKey("MenuItemId")]
        public virtual MenuItem? MenuItem { get; set; }
    }
}
