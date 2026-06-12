using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("Username", Name = "users_username_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("email")]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(256)]
    public string PasswordHash { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("password_changed_at")]
    public DateTime? PasswordChangedAt { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("phone")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Column("update_at")]
    public DateTimeOffset? UpdateAt { get; set; }

    [Column("birth_date")]
    public DateTimeOffset? BirthDate { get; set; }

    [Column("profile_image_url")]
    public string? ProfileImageUrl { get; set; }

    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; }

    [Column("locked_until")]
    public DateTime? LockedUntil { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();

    [InverseProperty("User")]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<PortfolioHolding> PortfolioHoldings { get; set; } = new List<PortfolioHolding>();

    [InverseProperty("User")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("User")]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    [InverseProperty("User")]
    public virtual ICollection<UserPortfolio> UserPortfolios { get; set; } = new List<UserPortfolio>();

    [InverseProperty("User")]
    public virtual UserSetting? UserSetting { get; set; }

    [InverseProperty("User")]
    public virtual UserTempCredential? UserTempCredential { get; set; }
}
