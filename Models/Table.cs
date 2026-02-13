using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.Models
{
    /// <summary>
    /// Represents a dining table in the restaurant
    /// </summary>
    public class Table
    {
        public int Id { get; set; }

        // Unique table number displayed to customers
        [Required(ErrorMessage = "Table number is required")]
        [Range(1, 999, ErrorMessage = "Table number must be between 1 and 999")]
        [Display(Name = "Table Number")]
        public int TableNumber { get; set; }

        // Maximum seating capacity of the table
        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
        public int Capacity { get; set; }

        // Indicates if the table is currently occupied
        [Display(Name = "Is Occupied")]
        public bool IsOccupied { get; set; } = false;

        // Optional description or location of the table
        [StringLength(100)]
        public string? Description { get; set; }

        // Navigation property for orders at this table
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
