using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("notificationid")]
        public int Notificationid { get; set; }

        [Column("userid")]
        public int Userid { get; set; }

        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Column("message")]
        public string Message { get; set; } = null!;

        [Column("type")]
        [MaxLength(50)]
        public string Type { get; set; } = null!;

        [Column("isread")]
        public bool Isread { get; set; } = false;

        [Column("relatedid")]
        public int? Relatedid { get; set; }

        [Column("createdat")]
        public DateTime Createdat { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("Userid")]
        public virtual User User { get; set; } = null!;
    }
}
