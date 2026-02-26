using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models
{
    [Table("reservationseats")]
    public class Reservationseat
    {
        [Key]
        [Column("reservationseatid")]
        public int Reservationseatid { get; set; }

        [Column("reservationid")]
        public int Reservationid { get; set; }

        [Column("seatnumber")]
        public int Seatnumber { get; set; }

        // NotMapped - kept for controller compatibility
        [NotMapped]
        public string Seatid { get; set; } = "";

        [ForeignKey("Reservationid")]
        public virtual Reservation Reservation { get; set; } = null!;
    }
}