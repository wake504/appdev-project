using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Appdev_Group_8.Models
{
    public enum ItemType
    {
        Lost,
        Found
    }

    public enum ItemStatus
    {
        Pending,
        UnderReview,
        Claimed,
        Resolved  
    }

    public class Item
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        [ForeignKey(nameof(ReportingUser))]
        public int UserId { get; set; }

        [Required]
        [ForeignKey(nameof(Category))]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        public DateTime DateReported { get; set; } = DateTime.UtcNow;

        // optional: date the item was lost/found
        public DateTime? DateLostOrFound { get; set; }

        [Required]
        public ItemType ItemType { get; set; } = ItemType.Lost;

        [Required]
        public ItemStatus Status { get; set; } = ItemStatus.Pending;

        public User? ReportingUser { get; set; }

        public Category? Category { get; set; }

        public ICollection<Claim>? Claims { get; set; }
    }
}
