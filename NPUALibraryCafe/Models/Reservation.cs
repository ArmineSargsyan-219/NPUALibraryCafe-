using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models
{
    [Table("reservations")]
    public class Reservation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("table_id")]
        public int TableId { get; set; }

        [Column("user_email")]
        [MaxLength(255)]
        public string UserEmail { get; set; } = null!;

        [Column("user_name")]
        [MaxLength(150)]
        public string UserName { get; set; } = null!;

        [Column("start_time")]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        public DateTime EndTime { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("TableId")]
        public virtual CafeTable? Table { get; set; }
    }

    [Table("tables")]
    public class CafeTable
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("table_number")]
        [MaxLength(10)]
        public string TableNumber { get; set; } = null!;

        [Column("capacity")]
        public int Capacity { get; set; }

        [Column("type")]
        [MaxLength(20)]
        public string Type { get; set; } = "group";

        [Column("position_row")]
        public int PositionRow { get; set; }

        [Column("position_col")]
        public int PositionCol { get; set; }

        [Column("is_reserved")]
        public bool IsReserved { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}