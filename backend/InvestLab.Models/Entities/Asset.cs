using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("assets")]
[Index("Symbol", Name = "assets_symbol_key", IsUnique = true)]
public partial class Asset
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("symbol")]
    [StringLength(20)]
    public string Symbol { get; set; } = null!;

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("sector")]
    [StringLength(50)]
    public string? Sector { get; set; }

    [Column("history_loaded")]
    public bool HistoryLoaded { get; set; }

    [Column("last_market_update_at")]
    public DateTime? LastMarketUpdateAt { get; set; }

    [InverseProperty("Asset")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    [InverseProperty("Asset")]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [InverseProperty("Asset")]
    public virtual ICollection<PortfolioHolding> PortfolioHoldings { get; set; } = new List<PortfolioHolding>();

    [InverseProperty("Asset")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
