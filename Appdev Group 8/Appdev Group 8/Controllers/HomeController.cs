using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Appdev_Group_8.Data;
using Appdev_Group_8.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Appdev_Group_8.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, IPasswordHasher<User> passwordHasher, ILogger<HomeController> logger)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {
            // prevent showing prior ModelState errors on fresh GET
            ModelState.Clear();

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string identifier, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            _logger.LogInformation("Login attempt for identifier: {Identifier}", identifier);

            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("Empty identifier or password");
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View("Index");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.SchoolId == identifier);
            if (user == null)
            {
                _logger.LogInformation("User not found for identifier: {Identifier}", identifier);
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View("Index");
            }

            _logger.LogInformation("User found (Id={UserId}, Role={Role})", user.UserId, user.Role);

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
            _logger.LogInformation("Password verification result: {Result}", verify);

            if (verify == PasswordVerificationResult.Failed)
            {
                _logger.LogInformation("Password verification failed for user {UserId}", user.UserId);
                ModelState.AddModelError(string.Empty, "Invalid credentials.");
                return View("Index");
            }

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Name, user.FullName),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            _logger.LogInformation("User {UserId} signed in", user.UserId);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == UserRole.Admin)
                return RedirectToAction("AdminDashboard", "Admin");

            return RedirectToAction("Dashboard", "Items");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            ModelState.Clear();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(string fullName, string email, string schoolId, string password)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Full name and password are required.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(schoolId))
            {
                ModelState.AddModelError(string.Empty, "Either email or school ID is required.");
                return View();
            }

            // Check if user already exists
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => 
                (u.Email == email && !string.IsNullOrWhiteSpace(email)) || 
                (u.SchoolId == schoolId && !string.IsNullOrWhiteSpace(schoolId)));

            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "A user with this email or school ID already exists.");
                return View();
            }

            // Create new user with default role = User (0)
            var newUser = new User
            {
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                SchoolId = string.IsNullOrWhiteSpace(schoolId) ? null : schoolId,
                Role = UserRole.User // Default role
            };

            // Hash password
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New user created: {FullName}", fullName);

            // Automatically sign in the new user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, newUser.UserId.ToString()),
                new System.Security.Claims.Claim(ClaimTypes.Name, newUser.FullName),
                new System.Security.Claims.Claim(ClaimTypes.Role, newUser.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            _logger.LogInformation("New user {UserId} signed in", newUser.UserId);

            return RedirectToAction("Dashboard", "Items");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
