using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Appdev_Group_8.Models
{
    public enum ClaimStatus
    {
        PendingApproval,
        Approved,
        Rejected,
        ItemCollected  // NEW: When owner has physically collected item
    }

    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        [ForeignKey(nameof(Item))]
        public int ItemId { get; set; }

        [Required]
        [ForeignKey(nameof(ClaimingUser))]
        public int UserId { get; set; }

        public DateTime ClaimDate { get; set; } = DateTime.UtcNow;

        [Required]
        public ClaimStatus ClaimStatus { get; set; } = ClaimStatus.PendingApproval;

        [MaxLength(2000)]
        public string? VerificationNotes { get; set; }

        // NEW: Where the item is being held for pickup
        [MaxLength(200)]
        public string? CollectionLocation { get; set; }

        // NEW: When the item was physically collected
        public DateTime? CollectionDate { get; set; }

        // Track if finder has been notified
        public bool FinderNotified { get; set; } = false;  // ADD THIS

        // NEW: Store owner's lost item ID
        public int? OwnerLostItemId { get; set; }

        public Item? Item { get; set; }

        public User? ClaimingUser { get; set; }
    }
}
