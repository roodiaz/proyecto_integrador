using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("favorites")]
[Index("UserId", Name = "idx_favorites_user")]
[Index("UserId", "AssetId", Name = "uq_fav", IsUnique = true)]
public partial class Favorite
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("Favorites")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Favorites")]
    public virtual User User { get; set; } = null!;
}
