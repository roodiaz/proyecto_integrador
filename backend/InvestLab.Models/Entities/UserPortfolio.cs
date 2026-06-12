using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("user_portfolios")]
[Index("UserId", Name = "idx_user_portfolios_user")]
public partial class UserPortfolio
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("initial_balance")]
    [Precision(18, 2)]
    public decimal InitialBalance { get; set; }

    [Column("current_balance")]
    [Precision(18, 2)]
    public decimal CurrentBalance { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserPortfolios")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("Portfolio")]
    public virtual ICollection<PortfolioHolding> PortfolioHoldings { get; set; } = new List<PortfolioHolding>();

    [InverseProperty("Portfolio")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
