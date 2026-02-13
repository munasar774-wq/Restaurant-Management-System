using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagement.Models
{
    public enum ReservationStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Completed
    }

    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Phone Number")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date & Time")]
        public DateTime ReservationDate { get; set; }

        [Required]
        [Range(1, 20)]
        public int Guests { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [StringLength(200)]
        public string? Notes { get; set; }

        [ForeignKey("TableId")]
        public virtual Table? Table { get; set; }
    }
}
