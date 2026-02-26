using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models
{
    [Table("reservations")]
    public class Reservation
    {
        [Key]
        [Column("reservationid")]
        public int Reservationid { get; set; }

        [Column("userid")]
        public int Userid { get; set; }

        [Column("reservationdate")]
        public DateTime Reservationdate { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        // NotMapped - kept for controller compatibility
        [NotMapped]
        public DateTime Starttime { get; set; }

        [NotMapped]
        public DateTime Endtime { get; set; }

        [NotMapped]
        public string Reservationtype { get; set; } = "solo";

        [NotMapped]
        public DateTime? Notificationsentat { get; set; }

        [NotMapped]
        public DateTime? Confirmedat { get; set; }

        [NotMapped]
        public DateTime? Cancelledat { get; set; }

        [NotMapped]
        public string? Notes { get; set; }

        [NotMapped]
        public DateTime Createdat { get; set; } = DateTime.UtcNow;

        [ForeignKey("Userid")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Reservationseat> Reservationseats { get; set; } = new List<Reservationseat>();
    }
}