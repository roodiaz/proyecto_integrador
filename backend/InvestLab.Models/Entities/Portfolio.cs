using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("portfolio")]
[Index("UserId", Name = "idx_portfolio_user")]
[Index("UserId", "AssetId", Name = "uq_user_asset", IsUnique = true)]
public partial class Portfolio
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("quantity")]
    [Precision(18, 6)]
    public decimal Quantity { get; set; }

    [Column("avg_price")]
    [Precision(18, 4)]
    public decimal AvgPrice { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("Portfolios")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Portfolios")]
    public virtual User User { get; set; } = null!;
}
