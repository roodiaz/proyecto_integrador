using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static InvestLab.Models.Enums;

namespace InvestLab.Data;

[Table("alerts")]
[Index("UserId", Name = "idx_alerts_user")]
public partial class Alert
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("asset_id")]
    public int AssetId { get; set; }

    [Column("condition_type")]
    public ConditionType ConditionType { get; set; }

    [Column("operator")]
    public AlertOperator Operator { get; set; }

    [Column("value")]
    [Precision(18, 4)]
    public decimal Value { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("last_triggered")]
    public DateTime? LastTriggered { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("Alerts")]
    public virtual Asset Asset { get; set; } = null!;

    [InverseProperty("Alert")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [ForeignKey("UserId")]
    [InverseProperty("Alerts")]
    public virtual User User { get; set; } = null!;
}
