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

        [Column("reservationtype")]
        [MaxLength(20)]
        public string Reservationtype { get; set; } = null!;

        [Column("starttime")]
        public DateTime Starttime { get; set; }

        [Column("endtime")]
        public DateTime Endtime { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Column("notificationsentat")]
        public DateTime? Notificationsentat { get; set; }

        [Column("confirmedat")]
        public DateTime? Confirmedat { get; set; }

        [Column("cancelledat")]
        public DateTime? Cancelledat { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [Column("createdat")]
        public DateTime Createdat { get; set; } = DateTime.Now;

        [ForeignKey("Userid")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<Reservationseat> Reservationseats { get; set; } = new List<Reservationseat>();
    }
}