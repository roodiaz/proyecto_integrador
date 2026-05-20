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

    [Column("alerts_used")]
    public int AlertsUsed { get; set; }

    [Column("favorites_used")]
    public int FavoritesUsed { get; set; }

    [Column("operations_used_today")]
    public int OperationsUsedToday { get; set; }

    [Column("searches_used_today")]
    public int SearchesUsedToday { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserSetting")]
    public virtual User User { get; set; } = null!;
}
