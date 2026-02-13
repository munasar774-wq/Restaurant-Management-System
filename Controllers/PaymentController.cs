using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;
using RestaurantManagement.ViewModels;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Handles payment processing
    /// </summary>
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display payment form for an order
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Process(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Check if already paid
            if (order.Payment != null)
            {
                TempData["Error"] = "This order has already been paid.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            // Check if order is ready or delivered
            if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Delivered)
            {
                TempData["Error"] = "Payment can only be processed for ready or delivered orders.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            var viewModel = new PaymentViewModel
            {
                OrderId = orderId,
                OrderTotal = order.TotalAmount,
                AmountReceived = order.TotalAmount
            };

            ViewBag.Order = order;
            return View(viewModel);
        }

        /// <summary>
        /// Process payment for an order
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(PaymentViewModel model)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .FirstOrDefaultAsync(o => o.Id == model.OrderId);

            if (order == null)
            {
                return NotFound();
            }

            // Validate amount received
            if (model.AmountReceived < order.TotalAmount)
            {
                ModelState.AddModelError("AmountReceived", "Amount received must be at least the order total.");
                ViewBag.Order = order;
                model.OrderTotal = order.TotalAmount;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            // Create payment record
            var payment = new Payment
            {
                OrderId = model.OrderId,
                Amount = order.TotalAmount,
                PaymentMethod = model.PaymentMethod,
                PaymentDate = DateTime.Now,
                ProcessedByUserId = user!.Id
            };

            _context.Payments.Add(payment);

            // Mark order as delivered if not already
            if (order.Status != OrderStatus.Delivered)
            {
                order.Status = OrderStatus.Delivered;
            }

            // Free up the table
            if (order.Table != null)
            {
                var hasOtherActiveOrders = await _context.Orders
                    .AnyAsync(o => o.TableId == order.TableId && 
                        o.Id != order.Id &&
                        (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.Ready));

                if (!hasOtherActiveOrders)
                {
                    order.Table.IsOccupied = false;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment processed for order #{OrderId}: {Amount} via {Method}", 
                order.Id, payment.Amount, payment.PaymentMethod);

            // Calculate change
            var change = model.AmountReceived - order.TotalAmount;
            if (change > 0)
            {
                TempData["Success"] = $"Payment processed successfully! Change: ${change:F2}";
            }
            else
            {
                TempData["Success"] = "Payment processed successfully!";
            }

            return RedirectToAction("Details", "Order", new { id = model.OrderId });
        }

        /// <summary>
        /// View payment receipt
        /// </summary>
        public async Task<IActionResult> Receipt(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Payment)
                    .ThenInclude(p => p!.ProcessedBy)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Payment == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
