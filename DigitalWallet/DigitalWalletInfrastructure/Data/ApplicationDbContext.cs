using DigitalWalletCore.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalWalletInfrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            

        }

        public DbSet<Merchant> Merchant { get; set; }
        public DbSet<School> School { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
        public DbSet<Wallet> Wallet { get; set; }
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<AuditLog> AuditLog { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Transaction>()
                .HasOne(t => t.SenderWallet)
                .WithMany(w => w.SentTransactions)
                .HasForeignKey(t => t.SenderWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Wallet>(entity =>
            {
                entity.Property(w => w.Balance).HasPrecision(18, 2);
                entity.Property(w => w.LockedBalance).HasPrecision(18, 2);
                entity.HasIndex(w => w.WalletNumber).IsUnique();
                entity.HasCheckConstraint("CK_Wallet_Balance_NonNegative", "\"Balance\" >= 0");
                entity.HasCheckConstraint("CK_Wallet_LockedBalance_NonNegative", "\"LockedBalance\" >= 0");
            });

            builder.Entity<School>().HasIndex(s => s.Code).IsUnique();
            builder.Entity<Merchant>().HasIndex(m => m.UserId).IsUnique();

            builder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount).HasPrecision(18, 2);
                entity.Property(t => t.SenderBalanceBefore).HasPrecision(18, 2);
                entity.Property(t => t.SenderBalanceAfter).HasPrecision(18, 2);
                entity.Property(t => t.ReceiverBalanceBefore).HasPrecision(18, 2);
                entity.Property(t => t.ReceiverBalanceAfter).HasPrecision(18, 2);
                entity.HasIndex(t => t.Reference).IsUnique();
                entity.HasIndex(t => new { t.Status, t.CreatedAt });
                entity.HasIndex(t => new { t.SenderWalletId, t.CreatedAt });
                entity.HasIndex(t => new { t.ReceiverWalletId, t.CreatedAt });
                entity.HasCheckConstraint("CK_Transaction_Amount_Positive", "\"Amount\" > 0");
            });

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(t => t.TokenHash).IsUnique();
                entity.HasIndex(t => new { t.UserId, t.ExpiresAt });
                entity.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(a => a.Details).HasColumnType("jsonb");
                entity.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });
                entity.HasIndex(a => new { a.ActorUserId, a.CreatedAt });
            });

            builder.Entity<Transaction>()
                .HasOne(t => t.ReceiverWallet)
                .WithMany(w => w.ReceivedTransactions)
                .HasForeignKey(t => t.ReceiverWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
