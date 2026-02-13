using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Enum representing the possible statuses of an order
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,
        Preparing = 1,
        Ready = 2,
        Delivered = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Represents a customer order in the restaurant
    /// </summary>
    public class Order
    {
        public int Id { get; set; }

        // Foreign key to the table where the order is placed
        [Required(ErrorMessage = "Table is required")]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        // Foreign key to the user who created the order
        [Required]
        [Display(Name = "Created By")]
        public string UserId { get; set; } = string.Empty;

        // Date and time when the order was placed
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Current status of the order
        [Display(Name = "Status")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Total amount of the order (calculated from order items)
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        // Optional notes for the order
        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation property for the table
        [ForeignKey("TableId")]
        public virtual Table? Table { get; set; }

        // Navigation property for the user who created the order
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Navigation property for order items
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Navigation property for payment
        public virtual Payment? Payment { get; set; }
    }
}
