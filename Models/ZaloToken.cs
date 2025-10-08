using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SigmaNotificationBackend.Models
{
    [Table("ZaloToken")] // tên chính xác theo DB
    public class ZaloToken
    {
        [Key]
        public int Id { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public DateTime ExpiresAt { get; set; }

        [Column("CreateAt")]
        public DateTime? CreatedAt { get; set; }

        [Column("UpdateAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
