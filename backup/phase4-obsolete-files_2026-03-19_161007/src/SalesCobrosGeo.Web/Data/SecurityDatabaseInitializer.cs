using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Data;

public sealed class SecurityDatabaseInitializer
{
    private const int CurrentSchemaVersion = 3;

    private readonly AppSecurityDbContext _dbContext;

    public SecurityDatabaseInitializer(AppSecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Initialize()
    {
        _dbContext.Database.EnsureCreated();
        EnsureSchemaUpToDate();

        var now = DateTime.UtcNow;
        SeedUsers(now);
        SeedCatalogs();
        SeedSalesAndCollections();
        _dbContext.SaveChanges();
    }

    private void EnsureSchemaUpToDate()
    {
        var version = GetUserVersion();
        if (version >= CurrentSchemaVersion)
        {
            return;
        }

        if (version < 1)
        {
            EnsureUserColumns();
            EnsureSessionColumns();
        }

        if (version < 2)
        {
            EnsureCatalogTables();
        }

        if (version < 3)
        {
            EnsureSalesTables();
        }

        SetUserVersion(CurrentSchemaVersion);
    }

    private int GetUserVersion()
    {
        using var connection = _dbContext.Database.GetDbConnection();
        EnsureOpen(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar() ?? 0);
    }

    private void SetUserVersion(int version)
    {
        // PRAGMA user_version sólo acepta un literal entero; usamos la conexión
        // directamente para evitar falsos positivos de inyección SQL en EF Core.
        using var connection = _dbContext.Database.GetDbConnection();
        EnsureOpen(connection);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }

    private void EnsureUserColumns()
    {
        EnsureColumn("Users", "TwoFactorEnabled");
        EnsureColumn("Users", "TwoFactorSecret");
    }

    private void EnsureSessionColumns()
    {
        EnsureColumn("Sessions", "LastHeartbeatUtc");
    }

    private void EnsureCatalogTables()
    {
        if (!TableExists("CatalogItems"))
        {
            _dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS CatalogItems (
    Id INTEGER NOT NULL CONSTRAINT PK_CatalogItems PRIMARY KEY AUTOINCREMENT,
    Category TEXT NOT NULL,
    Code TEXT NOT NULL,
    Name TEXT NOT NULL,
    Price TEXT NULL,
    IsActive INTEGER NOT NULL,
    SortOrder INTEGER NOT NULL
);");
            _dbContext.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_CatalogItems_Category_Code ON CatalogItems (Category, Code);");
        }
    }

    private void EnsureSalesTables()
    {
        if (!TableExists("Sales"))
        {
            _dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Sales (
    IdV TEXT NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
    NumVenta INTEGER NOT NULL,
    FechaVenta TEXT NOT NULL,
    NombreCliente TEXT NOT NULL,
    Celular TEXT NOT NULL,
    Telefono TEXT NULL,
    Zona TEXT NOT NULL,
    FormaPago TEXT NOT NULL,
    DiaCobro TEXT NOT NULL,
    FotoCliente TEXT NULL,
    FotoFachada TEXT NULL,
    FotoContrato TEXT NULL,
    ObservacionVenta TEXT NULL,
    Vendedor TEXT NOT NULL,
    Usuario TEXT NOT NULL,
    Cobrador TEXT NOT NULL,
    Coordenadas TEXT NOT NULL,
    UrlUbicacion TEXT NULL,
    FechaPrimerCobro TEXT NULL,
    Estado TEXT NOT NULL,
    FechaActu TEXT NOT NULL,
    Cliente TEXT NOT NULL,
    Producto TEXT NOT NULL,
    Estado2 TEXT NOT NULL,
    ComisionVendedorPct TEXT NOT NULL,
    Cobrar TEXT NOT NULL,
    FotoAdd1 TEXT NULL,
    FotoAdd2 TEXT NULL,
    Coordenadas2 TEXT NULL,
    ProductosCantidad INTEGER NOT NULL,
    ImporteTotal TEXT NOT NULL,
    ProductLinesJson TEXT NOT NULL
);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Sales_FechaVenta ON Sales (FechaVenta);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Sales_Zona ON Sales (Zona);");
            
            // Quick Win #1: Additional performance indexes
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Sales_Vendedor ON Sales (Vendedor);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Sales_Estado ON Sales (Estado);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Sales_FechaVenta_Zona ON Sales (FechaVenta, Zona);");
        }

        if (!TableExists("Collections"))
        {
            _dbContext.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS Collections (
    IdCc TEXT NOT NULL CONSTRAINT PK_Collections PRIMARY KEY,
    IdV TEXT NOT NULL,
    NumVenta INTEGER NOT NULL,
    NombreCliente TEXT NOT NULL,
    ImporteCobro TEXT NOT NULL,
    FechaCobro TEXT NOT NULL,
    ObservacionCobro TEXT NULL,
    FechaCaptura TEXT NOT NULL,
    ImporteTotal TEXT NOT NULL,
    ImporteRestante TEXT NOT NULL,
    EstadoCc TEXT NOT NULL,
    Usuario TEXT NOT NULL,
    ImporteAbonado TEXT NOT NULL,
    Estatus TEXT NOT NULL,
    Zona TEXT NOT NULL,
    DiaCobroPrevisto TEXT NOT NULL,
    DiaCobrado TEXT NOT NULL,
    CoordenadasCobro TEXT NULL,
    CONSTRAINT FK_Collections_Sales_IdV FOREIGN KEY (IdV) REFERENCES Sales (IdV) ON DELETE CASCADE
);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_FechaCobro ON Collections (FechaCobro);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_Usuario ON Collections (Usuario);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_IdV ON Collections (IdV);");
            
            // Quick Win #1: Additional performance indexes
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_Zona ON Collections (Zona);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_Estatus ON Collections (Estatus);");
            _dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Collections_FechaCobro_Zona ON Collections (FechaCobro, Zona);");
        }
    }

    private bool TableExists(string tableName)
    {
        using var connection = _dbContext.Database.GetDbConnection();
        EnsureOpen(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        AddParameter(command, "$name", tableName);
        return command.ExecuteScalar() is not null;
    }

    private void EnsureColumn(string tableName, string columnName)
    {
        if (!TableExists(tableName) || ColumnExists(tableName, columnName))
        {
            return;
        }

        var sql = (tableName, columnName) switch
        {
            ("Users", "TwoFactorEnabled") => "ALTER TABLE Users ADD COLUMN TwoFactorEnabled INTEGER NOT NULL DEFAULT 0;",
            ("Users", "TwoFactorSecret") => "ALTER TABLE Users ADD COLUMN TwoFactorSecret TEXT NULL;",
            ("Sessions", "LastHeartbeatUtc") => "ALTER TABLE Sessions ADD COLUMN LastHeartbeatUtc TEXT NULL;",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(sql))
        {
            _dbContext.Database.ExecuteSqlRaw(sql);
        }
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        using var connection = _dbContext.Database.GetDbConnection();
        EnsureOpen(connection);
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureOpen(DbConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private void SeedUsers(DateTime now)
    {
        if (_dbContext.Users.Any())
        {
            var rootAdmin = _dbContext.Users.Include(x => x.Permissions)
                .FirstOrDefault(x => x.Username == "RaizAdmin");

            if (rootAdmin is not null)
            {
                rootAdmin.IsActive = true;
                rootAdmin.Role = AppRoles.Full;
                rootAdmin.RoleLabel = "Acceso total";
                rootAdmin.Theme = string.IsNullOrWhiteSpace(rootAdmin.Theme) ? "root" : rootAdmin.Theme;
                rootAdmin.TwoFactorEnabled = true;
                rootAdmin.UpdatedUtc = now;
            }

            return;
        }

        var hasher = new PasswordHasher<AppUserEntity>();

        var users = new[]
        {
            BuildUser("RaizAdmin", "Raiz Admin", "RaizAdmin#2026!", "root", AppRoles.Full, "Acceso total", "Global", now,
                [AppPermissions.DashboardView, AppPermissions.SalesView, AppPermissions.CollectionsView, AppPermissions.MaintenanceView, AppPermissions.AdministrationView]),
            BuildUser("sale01", "Ventas 01", "Sale01#2026!", "sales", AppRoles.Sales, "Modulo ventas", "Heroes Chalco", now,
                [AppPermissions.DashboardView, AppPermissions.SalesView]),
            BuildUser("gest01", "Cobros 01", "Gest01#2026!", "collections", AppRoles.Collections, "Modulo cobros", "Jardines", now,
                [AppPermissions.DashboardView, AppPermissions.CollectionsView])
        };

        foreach (var user in users)
        {
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
        }

        _dbContext.Users.AddRange(users);
        _dbContext.AuditLogs.Add(new AuditLogEntity
        {
            CreatedUtc = now,
            EventType = "SECURITY_INIT",
            Username = "system",
            Description = "Base de seguridad SQLite inicializada con usuarios semilla.",
            Metadata = "bootstrap"
        });
    }

    private static AppUserEntity BuildUser(
        string username,
        string displayName,
        string password,
        string theme,
        string role,
        string roleLabel,
        string zone,
        DateTime now,
        IReadOnlyList<string> permissions)
    {
        return new AppUserEntity
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = password,
            Theme = theme,
            Role = role,
            RoleLabel = roleLabel,
            Zone = zone,
            IsActive = true,
            TwoFactorEnabled = username.Equals("RaizAdmin", StringComparison.OrdinalIgnoreCase),
            CreatedUtc = now,
            UpdatedUtc = now,
            Permissions = permissions.Select(permission => new AppUserPermissionEntity
            {
                Username = username,
                Permission = permission
            }).ToList()
        };
    }

    private void SeedCatalogs()
    {
        var items = new (string Category, string Code, string Name, decimal? Price, int SortOrder)[]
        {
            ("zone", "HEROES CHALCO", "Heroes Chalco", null, 1),
            ("zone", "JARDINES", "Jardines", null, 2),
            ("zone", "XICO", "Xico", null, 3),
            ("zone", "CENTRO", "Centro", null, 4),
            ("zone", "NORTE", "Norte", null, 5),
            ("payment", "SEMANAL", "Semanal", null, 1),
            ("payment", "QUINCENAL", "Quincenal", null, 2),
            ("payment", "CONTADO", "Contado", null, 3),
            ("collection_day", "LUNES", "Lunes", null, 1),
            ("collection_day", "MARTES", "Martes", null, 2),
            ("collection_day", "MIERCOLES", "Miercoles", null, 3),
            ("collection_day", "JUEVES", "Jueves", null, 4),
            ("collection_day", "VIERNES", "Viernes", null, 5),
            ("collection_day", "SABADO", "Sabado", null, 6),
            ("collection_day", "DOMINGO", "Domingo", null, 7),
            ("product", "GEL TICILT", "Gel Ticilt", 1490m, 1),
            ("product", "VITARAIZ 30", "VitaRaiz 30 caps", 780m, 2),
            ("product", "VITARAIZ 60", "VitaRaiz 60 caps", 1290m, 3),
            ("seller", "JAKE", "Jake", null, 1),
            ("seller", "LUCIA", "Lucia", null, 2),
            ("seller", "PEDRO", "Pedro", null, 3),
            ("collector", "SILVIA", "Silvia", null, 1),
            ("collector", "MARIO", "Mario", null, 2),
            ("collector", "ELENA", "Elena", null, 3),
            ("collector", "jakelinepink88@gmail.com", "jakelinepink88@gmail.com", null, 4),
            ("collector", "ggab75218@gmail.com", "ggab75218@gmail.com", null, 5),
            ("sale_status", "PENDIENTE", "Pendiente", null, 1),
            ("sale_status", "EN COBRO", "En cobro", null, 2),
            ("sale_status", "AL CORRIENTE", "Al corriente", null, 3),
            ("sale_status", "CANCELADO", "Cancelado", null, 4),
            ("sale_status", "LIQUIDADO", "Liquidado", null, 5),
            ("collection_status_group", "pending", "Pendientes hoy", null, 1),
            ("collection_status_group", "promise", "Promesas pago hoy", null, 2),
            ("collection_status_group", "followup", "Reagendados", null, 3),
            ("collection_status_group", "overdue", "Atrasados", null, 4),
            ("collection_status_group", "recovery", "Recuperacion", null, 5),
            ("collection_status_group", "current", "Al corriente", null, 6),
            ("collection_status_group", "liquidated", "Liquidados", null, 7),
            ("collection_status_group", "cancelled", "Cancelados", null, 8)
        };

        foreach (var item in items)
        {
            if (_dbContext.CatalogItems.Any(x => x.Category == item.Category && x.Code == item.Code))
            {
                continue;
            }

            _dbContext.CatalogItems.Add(new CatalogItemEntity
            {
                Category = item.Category,
                Code = item.Code,
                Name = item.Name,
                Price = item.Price,
                SortOrder = item.SortOrder,
                IsActive = true
            });
        }
    }

    private void SeedSalesAndCollections()
    {
        // Datos demo eliminados - el sistema ahora usa Excel como fuente de datos
        // Si necesita datos de prueba, puede agregarlos manualmente a través de la UI
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MÉTODOS DE SEED DEMO REMOVIDOS
    // ═══════════════════════════════════════════════════════════════════════════════
    // Los siguientes métodos han sido removidos porque el sistema ahora utiliza
    // Excel como almacenamiento de datos en lugar de datos hardcodeados:
    // - SeedBaseSaleSample()
    // - SeedDemoSalesMatrix()
    // - BuildDemoCollection()
    // ═══════════════════════════════════════════════════════════════════════════════
}
