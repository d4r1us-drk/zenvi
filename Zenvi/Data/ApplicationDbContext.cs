using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zenvi.Data.Models;

namespace Zenvi.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Media> Media { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(entity =>
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
            entity.HasKey(m => m.MediaID);
            entity.Property(m => m.MediaBlob).IsRequired();
            entity.Property(m => m.Type).IsRequired();
            entity.Property(m => m.CreatedAt).IsRequired();
        });
    }
}