using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Appdev_Group_8.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public ICollection<Item>? Items { get; set; }
    }
}
