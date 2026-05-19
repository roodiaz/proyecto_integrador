using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("user_settings")]
[Index("UserId", Name = "user_settings_user_id_key", IsUnique = true)]
public partial class UserSetting
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string? Currency { get; set; }

    [Column("email_notifications")]
    public bool? EmailNotifications { get; set; }

    [Column("max_alerts")]
    public int MaxAlerts { get; set; }

    [Column("max_favorites")]
    public int MaxFavorites { get; set; }

    [Column("max_operations_per_day")]
    public int MaxOperationsPerDay { get; set; }

    [Column("max_daily_searches")]
    public int MaxDailySearches { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserSetting")]
    public virtual User User { get; set; } = null!;
}
