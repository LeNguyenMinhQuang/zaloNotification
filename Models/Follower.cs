
using System;
using System.ComponentModel.DataAnnotations;

namespace SigmaNotificationBackend.Models
{
    public class Follower
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty;

        public DateTime FollowDate { get; set; } = DateTime.UtcNow;

        public int Role { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }

        public string? RoleDetail { get; set; } = "operator";


    }
}