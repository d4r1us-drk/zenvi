using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zenvi.Core.Data.Entities;

namespace Zenvi.Core.Data.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User>(options)
{
    public DbSet<Media> Media { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.ToTable(name: "User");
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable(name: "Role");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        builder.Entity<Media>(entity =>
        {
            entity.ToTable("Media");

            entity.HasKey(e => e.Name);

            entity.Property(e => e.Name)
                .HasMaxLength(50);

            entity.Property(e => e.Type)
                .IsRequired();

            entity.HasOne<Post>(m => m.Post)
                .WithMany(p => p.MediaContent)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Message>(m => m.Message)
                .WithMany(m => m.MediaContent)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<Follow>(entity =>
        {
            entity.HasKey(e => new { e.SourceId, e.TargetId });

            entity.HasOne(e => e.Source)
                .WithMany()
                .HasForeignKey(e => e.SourceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Target)
                .WithMany()
                .HasForeignKey(e => e.TargetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.FollowedAt)
                .IsRequired();
        });

        builder.Entity<Post>(entity =>
        {
            entity.ToTable("Posts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Content)
                .HasMaxLength(5000);

            entity.Property(e => e.LikeCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.PostOp)
                .WithMany()
                .HasForeignKey("PostOpId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.MediaContent)
                .WithOne(m => m.Post)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RepliedTo)
                .WithMany()
                .HasForeignKey(e => e.RepliedToId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.Content)
                .HasMaxLength(5000);

            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Sender)
                .WithMany()
                .HasForeignKey("SenderId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Receiver)
                .WithMany()
                .HasForeignKey("ReceiverId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.MediaContent)
                .WithOne(m => m.Message)
                .HasForeignKey(m => m.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}