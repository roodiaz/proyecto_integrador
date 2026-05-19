using System;
using System.Collections.Generic;
using InvestLab.Data;
using Microsoft.EntityFrameworkCore;

namespace InvestLab.Data.Context;

public partial class InvestLabDbContext : DbContext
{
    public InvestLabDbContext(DbContextOptions<InvestLabDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alert> Alerts { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<ContactMessage> ContactMessages { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Portfolio> Portfolios { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    public virtual DbSet<UserTempCredential> UserTempCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("alerts_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Asset).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_alert_asset");

            entity.HasOne(d => d.User).WithMany(p => p.Alerts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_alert_user");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("assets_pkey");
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contact_messages_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("favorites_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Asset).WithMany(p => p.Favorites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fav_asset");

            entity.HasOne(d => d.User).WithMany(p => p.Favorites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_fav_user");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Alert).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notif_alert");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notif_user");
        });

        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portfolio_pkey");

            entity.HasOne(d => d.Asset).WithMany(p => p.Portfolios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_portfolio_asset");

            entity.HasOne(d => d.User).WithMany(p => p.Portfolios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_portfolio_user");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_refresh_user");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transactions_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Asset).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_transactions_asset");

            entity.HasOne(d => d.User).WithMany(p => p.Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_transactions_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.Property(e => e.Balance).HasDefaultValue(10000.00m);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_settings_pkey");

            entity.Property(e => e.Currency).HasDefaultValueSql("'USD'::character varying");
            entity.Property(e => e.EmailNotifications).HasDefaultValue(true);

            entity.HasOne(d => d.User).WithOne(p => p.UserSetting)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_settings_user");
        });

        modelBuilder.Entity<UserTempCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_temp_credentials_pkey");

            entity.HasIndex(e => e.UserId, "uq_temp_active")
                .IsUnique()
                .HasFilter("(is_used = false)");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsUsed).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithOne(p => p.UserTempCredential)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_temp_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
