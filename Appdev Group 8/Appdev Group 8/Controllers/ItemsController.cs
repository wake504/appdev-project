using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Appdev_Group_8.Data;
using Appdev_Group_8.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Claim = Appdev_Group_8.Models.Claim;

namespace Appdev_Group_8.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(ApplicationDbContext db, ILogger<ItemsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: Views/Items/Dashboard.cshtml
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var user = await _db.Users.FindAsync(userId);
                if (user != null)
                {
                    ViewData["UserFullName"] = user.FullName;
                }
            }

            return View();
        }

        // GET: Views/Items/ReportItem.cshtml
        [HttpGet]
        public async Task<IActionResult> ReportItem()
        {
            var categories = await _db.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            if (!categories.Any())
            {
                // ensure at least one category exists
                _db.Categories.Add(new Category { CategoryName = "Uncategorized" });
                await _db.SaveChangesAsync();

                categories = await _db.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
            }

            ViewData["Categories"] = categories;
            return View();
        }

        // POST: Views/Items/ReportItem.cshtml (form should use asp-action="ReportItem")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportItem(
            string itemName,
            string description,
            string dateLost,
            string lastSeenLocation,
            int? categoryId = null,
            ItemType itemType = ItemType.Lost)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                ModelState.AddModelError(string.Empty, "Item name is required.");
                // reload categories for re-render
                ViewData["Categories"] = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();
                ViewData["SelectedType"] = itemType.ToString(); // itemType is your parameter
                return View("ReportItem");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            DateTime? parsedDate = null;
            if (DateTime.TryParse(dateLost, out var dt))
                parsedDate = dt;

            var category = await EnsureDefaultCategoryAsync(categoryId);

            var item = new Item
            {
                Title = itemName,
                Description = description,
                Location = lastSeenLocation,
                DateReported = DateTime.UtcNow,
                DateLostOrFound = parsedDate,
                ItemType = itemType, // use selected type (Lost or Found)
                Status = ItemStatus.Pending,
                UserId = userId,
                CategoryId = category.CategoryId
            };

            _db.Items.Add(item);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {UserId} reported {Type} item {Title}", userId, itemType, itemName);
            return RedirectToAction("MyReports");
        }

        // GET: Views/Items/ViewItems.cshtml  (list of all reports)
        [HttpGet]
        public async Task<IActionResult> ViewItems()
        {
            var items = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ReportingUser)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            return View(items);
        }

        // GET: Views/Items/MyReports.cshtml  (items for current user)
        [HttpGet]
        public async Task<IActionResult> MyReports()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var items = await _db.Items
                .Where(i => i.UserId == userId)
                .Include(i => i.Category)
                .Include(i => i.Claims)
                .OrderByDescending(i => i.DateReported)
                .ToListAsync();

            // Check for FINDER notifications (when someone claims their found item)
            var finderNotifications = await _db.Claims
                .Include(c => c.Item)
                .Where(c => c.Item.UserId == userId 
                    && c.Item.ItemType == ItemType.Found 
                    && !c.FinderNotified 
                    && c.ClaimStatus == ClaimStatus.PendingApproval)
                .ToListAsync();

            if (finderNotifications.Any())
            {
                var finderMessages = finderNotifications
                    .Select(c => $"Someone claimed your found item '<strong>{c.Item?.Title}</strong>'. Please bring it to the Security Office, PUP Main Gate for verification.")
                    .ToList();

                TempData["MatchNotifications"] = string.Join("<br/>", finderMessages);

                foreach (var claim in finderNotifications)
                {
                    claim.FinderNotified = true;
                }
                await _db.SaveChangesAsync();
            }

            // Check for OWNER notifications (when their item is ready for pickup)
            var ownerNotificationKey = $"OwnerNotification_{userId}";
            if (TempData.ContainsKey(ownerNotificationKey))
            {
                var ownerMessage = TempData[ownerNotificationKey]?.ToString();
                if (!string.IsNullOrEmpty(ownerMessage))
                {
                    // Combine with existing notifications or create new
                    if (TempData["MatchNotifications"] != null)
                    {
                        TempData["MatchNotifications"] = TempData["MatchNotifications"] + "<br/>" + ownerMessage;
                    }
                    else
                    {
                        TempData["MatchNotifications"] = ownerMessage;
                    }
                    TempData.Remove(ownerNotificationKey);
                }
            }

            return View(items);
        }

        // POST: Report finding a lost item (match it)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportFound(int itemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var item = await _db.Items.FindAsync(itemId);
            if (item == null) return NotFound();

            // Only allow if it's a LOST item and is Pending
            if (item.ItemType != ItemType.Lost || item.Status != ItemStatus.Pending)
            {
                TempData["ClaimError"] = "This item is not available.";
                return RedirectToAction("ViewItems");
            }

            // Don't allow the original reporter to claim their own lost item
            if (item.UserId == userId)
            {
                TempData["ClaimError"] = "You cannot report finding your own lost item.";
                return RedirectToAction("ViewItems");
            }

            var claim = new Claim
            {
                ItemId = itemId,
                UserId = userId,
                ClaimDate = DateTime.UtcNow,
                ClaimStatus = ClaimStatus.PendingApproval
            };

            _db.Claims.Add(claim);
            item.Status = ItemStatus.UnderReview;
            await _db.SaveChangesAsync();

            TempData["ClaimSuccess"] = "You've reported finding this item. Admin will review.";
            return RedirectToAction("MyReports");
        }

        // POST: Claim a found item (you're the original owner)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimFound(int itemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var item = await _db.Items.FindAsync(itemId);
            if (item == null) return NotFound();

            // Only allow if it's a FOUND item and is Pending
            if (item.ItemType != ItemType.Found || item.Status != ItemStatus.Pending)
            {
                TempData["ClaimError"] = "This item is not available for claiming.";
                return RedirectToAction("ViewItems");
            }

            // Don't allow the finder to claim their own found item
            if (item.UserId == userId)
            {
                TempData["ClaimError"] = "You cannot claim an item you reported finding.";
                return RedirectToAction("ViewItems");
            }

            var claim = new Claim
            {
                ItemId = itemId,
                UserId = userId,
                ClaimDate = DateTime.UtcNow,
                ClaimStatus = ClaimStatus.PendingApproval
            };

            _db.Claims.Add(claim);
            item.Status = ItemStatus.UnderReview;
            await _db.SaveChangesAsync();

            TempData["ClaimSuccess"] = "Claim submitted. You'll need to verify ownership with admin.";
            return RedirectToAction("MyReports");
        }

        // GET: Views/Items/FindMatches.cshtml - Show potential matches (LOST ITEMS ONLY)
        [HttpGet]
        public async Task<IActionResult> FindMatches(int itemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var item = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.ReportingUser)
                .FirstOrDefaultAsync(i => i.ItemId == itemId);

            if (item == null) return NotFound();

            // Can only find matches for your own items
            if (item.UserId != userId)
            {
                TempData["Error"] = "You can only find matches for your own items.";
                return RedirectToAction("MyReports");
            }

            // ONLY ALLOW LOST ITEMS TO FIND MATCHES
            if (item.ItemType != ItemType.Lost)
            {
                TempData["Error"] = "Only lost items can search for matches. Found items will be matched automatically when someone reports losing them.";
                return RedirectToAction("MyReports");
            }

            // Find potential matches (will only show Found items)
            var matches = await FindPotentialMatches(item);

            ViewData["SourceItem"] = item;
            return View(matches);
        }

        // Helper: Find potential matches
        private async Task<List<ItemMatchResult>> FindPotentialMatches(Item sourceItem)
        {
            // Since we now only allow Lost items to search, opposite type is always Found
            var oppositeType = ItemType.Found;

            // Get all Found items that are pending
            var candidates = await _db.Items
                .Where(i => i.ItemType == oppositeType
                    && i.Status == ItemStatus.Pending
                    && i.ItemId != sourceItem.ItemId)
                .Include(i => i.Category)
                .Include(i => i.ReportingUser)
                .ToListAsync();

            // Score each candidate
            var matches = candidates.Select(candidate => new ItemMatchResult
            {
                Item = candidate,
                MatchScore = CalculateMatchScore(sourceItem, candidate)
            })
            .Where(m => m.MatchScore > 0) // Only show if some match criteria met
            .OrderByDescending(m => m.MatchScore)
            .Take(10) // Top 10 matches
            .ToList();

            return matches;
        }

        // POST: Confirm a match
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMatch(int sourceItemId, int matchedItemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Challenge();

            var sourceItem = await _db.Items
                .Include(i => i.ReportingUser)
                .FirstOrDefaultAsync(i => i.ItemId == sourceItemId);
                
            var matchedItem = await _db.Items
                .Include(i => i.ReportingUser)
                .FirstOrDefaultAsync(i => i.ItemId == matchedItemId);

            if (sourceItem == null || matchedItem == null)
                return NotFound();

            if (sourceItem.UserId != userId)
            {
                TempData["Error"] = "Invalid operation.";
                return RedirectToAction("MyReports");
            }

            if (sourceItem.ItemType == matchedItem.ItemType)
            {
                TempData["Error"] = "Cannot match items of the same type.";
                return RedirectToAction("FindMatches", new { itemId = sourceItemId });
            }

            // Create claim on the matched item
            var claim = new Claim
            {
                ItemId = matchedItemId,
                UserId = userId,
                ClaimDate = DateTime.UtcNow,
                ClaimStatus = ClaimStatus.PendingApproval,
                VerificationNotes = $"Match initiated by {sourceItem.ReportingUser?.FullName} for lost item: {sourceItem.Title} (ID: {sourceItemId})",
                FinderNotified = false,
                OwnerLostItemId = sourceItemId  // Store owner's lost item ID
            };

            _db.Claims.Add(claim);
            matchedItem.Status = ItemStatus.UnderReview;
            sourceItem.Status = ItemStatus.UnderReview;  // Also update owner's lost item
            await _db.SaveChangesAsync();

            TempData["ClaimSuccess"] = $"Match confirmed! The finder will be notified to bring the item to Security Office, PUP Main Gate. Admin will verify and contact you.";
            
            _logger.LogInformation("Match confirmed: User {UserId} matched Lost item {SourceId} with Found item {MatchedId}.", 
                userId, sourceItemId, matchedItemId);
            
            return RedirectToAction("MyReports");
        }

        // Helper: Calculate match score
        private int CalculateMatchScore(Item source, Item candidate)
        {
            int score = 0;

            // Category match (40 points)
            if (source.CategoryId == candidate.CategoryId)
                score += 40;

            // Location match (30 points)
            if (!string.IsNullOrWhiteSpace(source.Location)
                && !string.IsNullOrWhiteSpace(candidate.Location)
                && source.Location.Equals(candidate.Location, StringComparison.OrdinalIgnoreCase))
            {
                score += 30;
            }

            // Partial location match (15 points)
            else if (!string.IsNullOrWhiteSpace(source.Location)
                && !string.IsNullOrWhiteSpace(candidate.Location)
                && (source.Location.Contains(candidate.Location, StringComparison.OrdinalIgnoreCase)
                    || candidate.Location.Contains(source.Location, StringComparison.OrdinalIgnoreCase)))
            {
                score += 15;
            }

            // Date proximity (20 points - within 7 days)
            var sourceDate = source.DateLostOrFound ?? source.DateReported;
            var candidateDate = candidate.DateLostOrFound ?? candidate.DateReported;
            var daysDiff = Math.Abs((sourceDate - candidateDate).TotalDays);

            if (daysDiff <= 1)
                score += 20;
            else if (daysDiff <= 3)
                score += 15;
            else if (daysDiff <= 7)
                score += 10;

            // Title similarity (10 points)
            if (!string.IsNullOrWhiteSpace(source.Title)
                && !string.IsNullOrWhiteSpace(candidate.Title))
            {
                var sourceTitleLower = source.Title.ToLower();
                var candidateTitleLower = candidate.Title.ToLower();

                if (sourceTitleLower.Contains(candidateTitleLower)
                    || candidateTitleLower.Contains(sourceTitleLower))
                {
                    score += 10;
                }
            }

            return score;
        }

        // helper: ensure a fallback category exists
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
    }

    // Model for match results
    public class ItemMatchResult
    {
        public Item Item { get; set; } = null!;
        public int MatchScore { get; set; }

        public string MatchQuality => MatchScore switch
        {
            >= 80 => "Excellent Match",
            >= 60 => "Good Match",
            >= 40 => "Possible Match",
            _ => "Weak Match"
        };

        public string MatchBadgeClass => MatchScore switch
        {
            >= 80 => "bg-success",
            >= 60 => "bg-primary",
            >= 40 => "bg-warning",
            _ => "bg-secondary"
        };
    }

}
