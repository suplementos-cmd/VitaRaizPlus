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
    public DbSet<CatalogItemEntity> CatalogItems => Set<CatalogItemEntity>();
    public DbSet<SaleEntity> Sales => Set<SaleEntity>();
    public DbSet<CollectionEntity> Collections => Set<CollectionEntity>();

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

        modelBuilder.Entity<CatalogItemEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Category, x.Code }).IsUnique();
            entity.Property(x => x.Category).HasMaxLength(32);
            entity.Property(x => x.Code).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<SaleEntity>(entity =>
        {
            entity.HasKey(x => x.IdV);
            entity.Property(x => x.IdV).HasMaxLength(32);
            entity.Property(x => x.NombreCliente).HasMaxLength(256);
            entity.Property(x => x.Zona).HasMaxLength(128);
            entity.Property(x => x.FormaPago).HasMaxLength(64);
            entity.Property(x => x.DiaCobro).HasMaxLength(64);
            entity.Property(x => x.Vendedor).HasMaxLength(128);
            entity.Property(x => x.Usuario).HasMaxLength(128);
            entity.Property(x => x.Cobrador).HasMaxLength(128);
            
            // Performance indexes - Quick Win #1
            entity.HasIndex(x => x.FechaVenta);
            entity.HasIndex(x => x.Zona);
            entity.HasIndex(x => x.Vendedor);
            entity.HasIndex(x => x.Estado);
            entity.HasIndex(x => new { x.FechaVenta, x.Zona }); // Composite for dashboard
            
            entity.HasMany(x => x.Collections)
                .WithOne(x => x.Sale)
                .HasForeignKey(x => x.IdV)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CollectionEntity>(entity =>
        {
            entity.HasKey(x => x.IdCc);
            entity.Property(x => x.IdCc).HasMaxLength(32);
            entity.Property(x => x.Usuario).HasMaxLength(128);
            entity.Property(x => x.Zona).HasMaxLength(128);
            
            // Performance indexes - Quick Win #1
            entity.HasIndex(x => x.FechaCobro);
            entity.HasIndex(x => x.Usuario);
            entity.HasIndex(x => x.Zona);
            entity.HasIndex(x => x.Estatus);
            entity.HasIndex(x => new { x.FechaCobro, x.Zona }); // Composite for dashboard
        });
    }
}
