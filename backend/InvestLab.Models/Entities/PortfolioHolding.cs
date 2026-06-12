using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("portfolio_holdings")]
[Index("UserId", Name = "idx_portfolio_holdings_user")]
[Index("PortfolioId", Name = "idx_portfolio_holdings_portfolio")]
[Index("PortfolioId", "AssetId", Name = "uq_portfolio_asset", IsUnique = true)]
public partial class PortfolioHolding
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("portfolio_id")]
    public int PortfolioId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("quantity")]
    [Precision(18, 6)]
    public decimal Quantity { get; set; }

    [Column("avg_price")]
    [Precision(18, 4)]
    public decimal AvgPrice { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("PortfolioHoldings")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("PortfolioHoldings")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("PortfolioId")]
    [InverseProperty("PortfolioHoldings")]
    public virtual UserPortfolio Portfolio { get; set; } = null!;
}
