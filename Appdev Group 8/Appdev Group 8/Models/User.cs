using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Appdev_Group_8.Models
{
    public enum UserRole
    {
        User,
        Admin
    }

    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(254)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? SchoolId { get; set; }

        // store salted+hashed password
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public ICollection<Item>? ReportedItems { get; set; }

        public ICollection<Claim>? Claims { get; set; }
    }
}
