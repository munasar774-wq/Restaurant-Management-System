using RestaurantManagement.Models;

namespace RestaurantManagement.ViewModels
{
    /// <summary>
    /// ViewModel for daily report
    /// </summary>
    public class DailyReportViewModel
    {
        public DateTime Date { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CashPayments { get; set; }
        public decimal CardPayments { get; set; }
        public List<OrderSummary> Orders { get; set; } = new List<OrderSummary>();
        public List<TopSellingItem> TopSellingItems { get; set; } = new List<TopSellingItem>();
    }

    /// <summary>
    /// ViewModel for monthly report
    /// </summary>
    public class MonthlyReportViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailySummary> DailySummaries { get; set; } = new List<DailySummary>();
        public List<TopSellingItem> TopSellingItems { get; set; } = new List<TopSellingItem>();
        public List<EmployeePerformance> EmployeePerformances { get; set; } = new List<EmployeePerformance>();
    }

    /// <summary>
    /// Summary of an order for reports
    /// </summary>
    public class OrderSummary
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Top selling item summary
    /// </summary>
    public class TopSellingItem
    {
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Daily summary for monthly reports
    /// </summary>
    public class DailySummary
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Employee performance summary
    /// </summary>
    public class EmployeePerformance
    {
        public string EmployeeName { get; set; } = string.Empty;
        public int OrdersProcessed { get; set; }
        public decimal TotalSales { get; set; }
    }
}
