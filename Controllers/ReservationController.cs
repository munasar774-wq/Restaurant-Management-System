using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Data;
using RestaurantManagement.Models;

namespace RestaurantManagement.Controllers
{
    [Authorize]
    public class ReservationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reservation
        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .OrderBy(r => r.ReservationDate)
                .Where(r => r.ReservationDate >= DateTime.Today)
                .ToListAsync();

            ViewData["TableId"] = new SelectList(_context.Tables.OrderBy(t => t.TableNumber), "Id", "TableNumber");
            
            return View(reservations);
        }

        // GET: Reservation/Create
        public IActionResult Create(int? tableId)
        {
            ViewData["TableId"] = new SelectList(_context.Tables.OrderBy(t => t.TableNumber), "Id", "TableNumber", tableId);
            return View();
        }

        // POST: Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TableId,CustomerName,CustomerPhone,ReservationDate,Guests,Notes")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Basic validation: Check if table is already reserved for that time
                var conflict = await _context.Reservations
                    .AnyAsync(r => r.TableId == reservation.TableId 
                        && r.ReservationDate < reservation.ReservationDate.AddHours(2) 
                        && r.ReservationDate > reservation.ReservationDate.AddHours(-2)
                        && r.Status != ReservationStatus.Cancelled);

                if (conflict)
                {
                    ModelState.AddModelError("ReservationDate", "This table is already booked near this time.");
                    ViewData["TableId"] = new SelectList(_context.Tables, "Id", "TableNumber", reservation.TableId);
                    return View(reservation);
                }

                reservation.Status = ReservationStatus.Confirmed;
                _context.Add(reservation);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Reservation booked successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["TableId"] = new SelectList(_context.Tables, "Id", "TableNumber", reservation.TableId);
            return View(reservation);
        }

        // POST: Reservation/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Reservation cancelled.";
            }
            return RedirectToAction(nameof(Index));
        }

         // POST: Reservation/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Completed;
                await _context.SaveChangesAsync();
                 TempData["Success"] = "Reservation marked as completed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
