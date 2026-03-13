using Microsoft.EntityFrameworkCore;

namespace SalesCobrosGeo.Web.Data;

public sealed class AppSecurityDbContext : DbContext
{
    public AppSecurityDbContext(DbContextOptions<AppSecurityDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUserEntity> Users => Set<AppUserEntity>();
    public DbSet<AppUserPermissionEntity> UserPermissions => Set<AppUserPermissionEntity>();
    public DbSet<AppSessionEntity> Sessions => Set<AppSessionEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.HasKey(x => x.Username);
            entity.Property(x => x.Username).HasMaxLength(64);
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.Property(x => x.Theme).HasMaxLength(32);
            entity.Property(x => x.Role).HasMaxLength(64);
            entity.Property(x => x.RoleLabel).HasMaxLength(128);
            entity.Property(x => x.Zone).HasMaxLength(128);
            entity.HasMany(x => x.Permissions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.Username)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Sessions)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.Username)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUserPermissionEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Username, x.Permission }).IsUnique();
            entity.Property(x => x.Permission).HasMaxLength(128);
        });

        modelBuilder.Entity<AppSessionEntity>(entity =>
        {
            entity.HasKey(x => x.SessionId);
            entity.Property(x => x.SessionId).HasMaxLength(64);
            entity.Property(x => x.Username).HasMaxLength(64);
            entity.Property(x => x.DisplayName).HasMaxLength(160);
            entity.Property(x => x.RoleLabel).HasMaxLength(128);
            entity.Property(x => x.Zone).HasMaxLength(128);
            entity.HasIndex(x => new { x.Username, x.LastSeenUtc });
        });

        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedUtc);
            entity.Property(x => x.EventType).HasMaxLength(64);
            entity.Property(x => x.Username).HasMaxLength(64);
        });
    }
}
