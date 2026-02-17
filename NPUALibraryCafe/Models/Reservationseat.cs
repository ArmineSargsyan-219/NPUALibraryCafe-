using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models
{
    [Table("reservationseats")]
    public class Reservationseat
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("reservationid")]
        public int Reservationid { get; set; }

        [Column("seatid")]
        [MaxLength(20)]
        public string Seatid { get; set; } = null!;

        // Navigation property
        [ForeignKey("Reservationid")]
        public virtual Reservation Reservation { get; set; } = null!;
    }
}