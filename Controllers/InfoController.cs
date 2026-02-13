using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagement.Controllers
{
    /// <summary>
    /// Handles static pages: About, Contact, Services
    /// </summary>
    public class InfoController : Controller
    {
        /// <summary>
        /// About page - information about the restaurant
        /// </summary>
        public IActionResult About()
        {
            return View();
        }

        /// <summary>
        /// Contact page - restaurant contact details
        /// </summary>
        public IActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Services page - what services the restaurant offers
        /// </summary>
        public IActionResult Services()
        {
            return View();
        }
    }
}
