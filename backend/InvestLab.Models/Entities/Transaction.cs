using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static InvestLab.Models.Enums;

namespace InvestLab.Data;

[Table("transactions")]
[Index("UserId", Name = "idx_transactions_user")]
public partial class Transaction
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("type")]
    public TransactionType Type { get; set; }

    [Column("quantity")]
    [Precision(18, 6)]
    public decimal Quantity { get; set; }

    [Column("price")]
    [Precision(18, 4)]
    public decimal Price { get; set; }

    [Column("total")]
    [Precision(18, 2)]
    public decimal Total { get; set; }

    [Column("balance_before")]
    [Precision(18, 2)]
    public decimal BalanceBefore { get; set; }

    [Column("balance_after")]
    [Precision(18, 2)]
    public decimal BalanceAfter { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("Transactions")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Transactions")]
    public virtual User User { get; set; } = null!;
}
