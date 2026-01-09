using Appdev_Group_8.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Appdev_Group_8.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ILogger<ItemsController> logger)
        {
            _logger = logger;
        }

        public IActionResult ReportLostItem()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitReport(string itemName, string description, string dateLost, string lastSeenLocation, string contactInfo)
        {
            // Process the report here
            // You can save to database or perform other operations
            _logger.LogInformation($"Lost item reported: {itemName}");
            
            // For now, redirect to MyReports
            return RedirectToAction("MyReports");
        }

        public IActionResult ViewFoundItems()
        {
            return View();
        }

        public IActionResult MyReports()
        {
            return View();
        }
    }
}
