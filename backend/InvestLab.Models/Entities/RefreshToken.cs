using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data;

[Table("refresh_tokens")]
[Index("Token", Name = "idx_refresh_tokens_token")]
[Index("UserId", Name = "idx_refresh_tokens_user")]
[Index("Token", Name = "refresh_tokens_token_key", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("token")]
    [StringLength(512)]
    public string Token { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("created_by_ip")]
    [StringLength(64)]
    public string? CreatedByIp { get; set; }

    [Column("user_agent")]
    [StringLength(256)]
    public string? UserAgent { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
