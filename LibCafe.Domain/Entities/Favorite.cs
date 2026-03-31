using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibCafe.Domain.Entities;

[Table("favorites")]
public class Favorite
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = null!;

    [Column("item_id")]
    public string ItemId { get; set; } = null!;

    [Column("item_type")]
    public string ItemType { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}