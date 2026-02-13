using RestaurantManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagement.ViewModels
{
    /// <summary>
    /// ViewModel for admin dashboard statistics
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalMenuItems { get; set; }
        public int AvailableTables { get; set; }
        public int OccupiedTables { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }

    /// <summary>
    /// ViewModel for employee dashboard
    /// </summary>
    public class EmployeeDashboardViewModel
    {
        public int MyOrdersToday { get; set; }
        public int PendingOrders { get; set; }
        public int ReadyOrders { get; set; }
        public List<Order> ActiveOrders { get; set; } = new List<Order>();
        public List<Table> AvailableTables { get; set; } = new List<Table>();
    }

    /// <summary>
    /// ViewModel for creating a new order
    /// </summary>
    public class CreateOrderViewModel
    {
        [Required(ErrorMessage = "Table is required")]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<Table> AvailableTables { get; set; } = new List<Table>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
    }

    /// <summary>
    /// ViewModel for order items in the create order form
    /// </summary>
    public class OrderItemViewModel
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
        
        public decimal UnitPrice { get; set; }
        
        [StringLength(200)]
        public string? SpecialInstructions { get; set; }
    }

    /// <summary>
    /// ViewModel for updating order status
    /// </summary>
    public class UpdateOrderStatusViewModel
    {
        public int OrderId { get; set; }
        public OrderStatus CurrentStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
    }

    /// <summary>
    /// ViewModel for processing payment
    /// </summary>
    public class PaymentViewModel
    {
        public int OrderId { get; set; }
        
        [Display(Name = "Order Total")]
        public decimal OrderTotal { get; set; }
        
        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Display(Name = "Amount Received")]
        [Range(0.01, 100000)]
        public decimal AmountReceived { get; set; }
        
        public decimal Change => AmountReceived - OrderTotal;
    }
}
