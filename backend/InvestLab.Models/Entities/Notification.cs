using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("notifications")]
[Index("UserId", Name = "idx_notifications_user")]
public partial class Notification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("alert_id")]
    public int AlertId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("message")]
    [StringLength(500)]
    public string? Message { get; set; }

    [Column("price")]
    [Precision(18, 4)]
    public decimal? Price { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("is_read")]
    public bool? IsRead { get; set; }

    [ForeignKey("AlertId")]
    [InverseProperty("Notifications")]
    public virtual Alert Alert { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
