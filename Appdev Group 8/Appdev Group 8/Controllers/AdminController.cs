using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Appdev_Group_8.Data;
using Appdev_Group_8.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Appdev_Group_8.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext db, ILogger<AdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /Admin/AdminDashboard
        public async Task<IActionResult> AdminDashboard()
        {
            var totalLost = await _db.Items.CountAsync(i => i.ItemType == ItemType.Lost);
            var totalFound = await _db.Items.CountAsync(i => i.ItemType == ItemType.Found);

            var items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ReportingUser)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            ViewData["TotalLost"] = totalLost;
            ViewData["TotalFound"] = totalFound;

            return View(items); // Views/Admin/AdminDashboard.cshtml expects IEnumerable<Item>
        }

        // GET: /Admin/ClaimReports
        public async Task<IActionResult> ClaimReports()
        {
            var claims = await _db.Claims
                .Include(c => c.Item)
                .Include(c => c.ClaimingUser)
                .OrderByDescending(c => c.ClaimDate)
                .ToListAsync();

            return View(claims); // Views/Admin/ClaimReports.cshtml expects IEnumerable<Claim>
        }

        // GET: /Admin/ViewUsers
        public async Task<IActionResult> ViewUsers()
        {
            var users = await _db.Users
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users); // Views/Admin/ViewUsers.cshtml expects IEnumerable<User>
        }

        public async Task<IActionResult> ManageLostReports()
        {
            var items = await _db.Items
                .Where(i => i.ItemType == ItemType.Lost)
                .Include(i => i.ReportingUser)
                .Include(i => i.Category)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(items); // Views/Admin/ManageLostReports.cshtml should accept IEnumerable<Item>
        }

        public async Task<IActionResult> ManageFoundItems()
        {
            var items = await _db.Items
                .Where(i => i.ItemType == ItemType.Found)
                .Include(i => i.ReportingUser)
                .Include(i => i.Category)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(items); // Views/Admin/ManageFoundItems.cshtml should accept IEnumerable<Item>
        }

        public async Task<IActionResult> AllReports()
        {
            var items = await _db.Items
                .Include(i => i.ReportingUser)
                .Include(i => i.Category)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(items); // Views/Admin/AllReports.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFoundItem(string itemName, string dateFound, string locationFound, string description, int? categoryId = null)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                ModelState.AddModelError(string.Empty, "Item name is required.");
                return RedirectToAction("ManageFoundItems");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                // fallback or challenge if no valid user id
                return Challenge();
            }

            DateTime? parsed = null;
            if (DateTime.TryParse(dateFound, out var dt))
                parsed = dt;

            var category = await EnsureDefaultCategoryAsync(categoryId);

            var item = new Item
            {
                Title = itemName,
                Description = description,
                Location = locationFound,
                DateReported = DateTime.UtcNow,
                DateLostOrFound = parsed,
                ItemType = ItemType.Found,
                Status = ItemStatus.Pending,
                CategoryId = category.CategoryId,
                UserId = userId
            };

            _db.Items.Add(item);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} added found item: {Title}", userId, itemName);
            return RedirectToAction("ManageFoundItems");
        }

        public IActionResult AdminReports()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Admin logged out");
            return RedirectToAction("Index", "Home");
        }

        private async Task<Category> EnsureDefaultCategoryAsync(int? categoryId)
        {
            if (categoryId.HasValue)
            {
                var c = await _db.Categories.FindAsync(categoryId.Value);
                if (c != null) return c;
            }

            var existing = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Uncategorized");
            if (existing != null) return existing;

            var created = new Category { CategoryName = "Uncategorized" };
            _db.Categories.Add(created);
            await _db.SaveChangesAsync();
            return created;
        }

        // Add these admin endpoints to approve/reject claims
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClaim(int claimId, string? verificationNotes = null, string? collectionLocation = null)
        {
            var claim = await _db.Claims.Include(c => c.Item).FirstOrDefaultAsync(c => c.ClaimId == claimId);
            if (claim == null) return NotFound();

            claim.ClaimStatus = ClaimStatus.Approved;
            claim.VerificationNotes = verificationNotes;
            claim.CollectionLocation = collectionLocation ?? "Security Office, Main Building"; // Default location
            claim.ClaimDate = DateTime.UtcNow;

            if (claim.Item != null)
            {
                claim.Item.Status = ItemStatus.Claimed;
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Claim {ClaimId} approved. Item ready for collection at {Location}", claimId, claim.CollectionLocation);
            
            return RedirectToAction("ClaimReports");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClaim(int claimId, string? verificationNotes = null)
        {
            var claim = await _db.Claims.Include(c => c.Item).FirstOrDefaultAsync(c => c.ClaimId == claimId);
            if (claim == null) return NotFound();

            claim.ClaimStatus = ClaimStatus.Rejected;
            claim.VerificationNotes = verificationNotes;
            claim.ClaimDate = DateTime.UtcNow;

            if (claim.Item != null && claim.Item.Status == ItemStatus.UnderReview)
            {
                claim.Item.Status = ItemStatus.Pending;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("ClaimReports");
        }

        // NEW: Mark item as physically collected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCollected(int claimId)
        {
            var claim = await _db.Claims
                .Include(c => c.Item)
                    .ThenInclude(i => i.ReportingUser)
                .Include(c => c.ClaimingUser)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);
                
            if (claim == null) return NotFound();

            if (claim.ClaimStatus != ClaimStatus.Approved)
            {
                TempData["Error"] = "Claim must be approved before marking as collected.";
                return RedirectToAction("ClaimReports");
            }

            // Update claim status
            claim.ClaimStatus = ClaimStatus.ItemCollected;
            claim.CollectionDate = DateTime.UtcNow;

            // Get the matched item (the one that was claimed)
            var matchedItem = claim.Item;
            
            if (matchedItem != null)
            {
                // Mark matched item as Resolved
                matchedItem.Status = ItemStatus.Resolved;

                // Find the opposite item (if Lost item was claimed, find the Found item reported by the claimer, or vice versa)
                Item? oppositeItem = null;

                if (matchedItem.ItemType == ItemType.Found)
                {
                    // Matched item is Found, so find the Lost item reported by the claimer
                    oppositeItem = await _db.Items
                        .Where(i => i.UserId == claim.UserId 
                            && i.ItemType == ItemType.Lost
                            && i.Status == ItemStatus.Claimed
                            && i.Title.ToLower() == matchedItem.Title.ToLower())
                        .FirstOrDefaultAsync();
                }
                else if (matchedItem.ItemType == ItemType.Lost)
                {
                    // Matched item is Lost, so find the Found item reported by the original reporter
                    oppositeItem = await _db.Items
                        .Where(i => i.UserId == matchedItem.UserId 
                            && i.ItemType == ItemType.Found
                            && i.Status == ItemStatus.Claimed
                            && i.Title.ToLower() == matchedItem.Title.ToLower())
                        .FirstOrDefaultAsync();
                }

                // Mark opposite item as Resolved too
                if (oppositeItem != null)
                {
                    oppositeItem.Status = ItemStatus.Resolved;
                    _logger.LogInformation("Both items marked as Resolved: Lost Item {LostId} and Found Item {FoundId}", 
                        matchedItem.ItemType == ItemType.Lost ? matchedItem.ItemId : oppositeItem.ItemId,
                        matchedItem.ItemType == ItemType.Found ? matchedItem.ItemId : oppositeItem.ItemId);
                }
            }

            await _db.SaveChangesAsync();
            
            _logger.LogInformation("Item from Claim {ClaimId} collected by owner on {Date}", claimId, claim.CollectionDate);
            
            TempData["Success"] = "Item marked as collected successfully. Both Lost and Found reports have been resolved.";
            return RedirectToAction("ClaimReports");
        }
    }
}
