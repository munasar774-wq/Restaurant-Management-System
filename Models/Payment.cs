using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Enum representing the payment methods accepted
    /// </summary>
    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        EVC = 2,
        Edahab = 3
    }

    /// <summary>
    /// Represents a payment for an order
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }

        // Foreign key to the order being paid
        [Required]
        [Display(Name = "Order")]
        public int OrderId { get; set; }

        // Payment amount
        [Required(ErrorMessage = "Amount is required")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        [Range(0.01, 100000, ErrorMessage = "Amount must be between 0.01 and 100,000")]
        public decimal Amount { get; set; }

        // Method of payment (Cash or Card)
        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }

        // Date and time of payment
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // ID of the user who processed the payment
        [Display(Name = "Processed By")]
        public string? ProcessedByUserId { get; set; }

        // Navigation property for the order
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        // Navigation property for the user who processed payment
        [ForeignKey("ProcessedByUserId")]
        public virtual ApplicationUser? ProcessedBy { get; set; }
    }
}
