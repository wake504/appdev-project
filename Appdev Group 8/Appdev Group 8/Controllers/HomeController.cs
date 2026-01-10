using System.Diagnostics;
using Appdev_Group_8.Models;
using Microsoft.AspNetCore.Mvc;

namespace Appdev_Group_8.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult AdminLogin()
        {
            return View("~/Views/Admin/AdminLogin.cshtml");
        }

        public IActionResult AdminDashboard()
        {
            return View("~/Views/Admin/AdminDashboard.cshtml");
        }

        public IActionResult ManageLostReports()
        {
            return View("~/Views/Admin/ManageLostReports.cshtml");
        }

        public IActionResult ManageFoundItems()
        {
            return View("~/Views/Admin/ManageFoundItems.cshtml");
        }

        public IActionResult AdminReports()
        {
            return View("~/Views/Admin/AdminReports.cshtml");
        }

        public IActionResult AddFoundItem()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitFoundItem(string itemName, string dateFound, string locationFound, string description)
        {
            // Process the found item here
            _logger.LogInformation($"Found item added: {itemName}");
            
            // Redirect to Manage Found Items
            return RedirectToAction("ManageFoundItems");
        }

        public IActionResult AllReports()
        {
            return View("~/Views/Admin/AllReports.cshtml");
        }

        public IActionResult Logout()
        {
            // Log the user out and redirect to login page
            _logger.LogInformation("Admin user logged out");
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
