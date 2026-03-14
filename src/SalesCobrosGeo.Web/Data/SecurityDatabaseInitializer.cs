using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Data;

public sealed class SecurityDatabaseInitializer
{
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
        EnsureUserColumns();
        EnsureSessionColumns();
        EnsureCatalogTables();
        EnsureSalesTables();
    }

    private void EnsureUserColumns()
    {
        EnsureColumn("Users", "TwoFactorEnabled", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn("Users", "TwoFactorSecret", "TEXT NULL");
    }

    private void EnsureSessionColumns()
    {
        EnsureColumn("Sessions", "LastHeartbeatUtc", "TEXT NULL");
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

    private void EnsureColumn(string tableName, string columnName, string definition)
    {
        if (!TableExists(tableName) || ColumnExists(tableName, columnName))
        {
            return;
        }

        _dbContext.Database.ExecuteSqlRaw($"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};");
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
        if (_dbContext.CatalogItems.Any())
        {
            return;
        }

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
            ("collector", "ggab75218@gmail.com", "ggab75218@gmail.com", null, 5)
        };

        foreach (var item in items)
        {
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
        if (_dbContext.Sales.Any())
        {
            return;
        }

        var productLines = JsonSerializer.Serialize(new List<SaleProductLineInput>
        {
            new() { ProductCode = "GEL TICILT", Quantity = 1, UnitPrice = 1490m }
        });

        _dbContext.Sales.Add(new SaleEntity
        {
            IdV = "addcdb49",
            NumVenta = 1,
            FechaVenta = new DateTime(2024, 12, 10),
            NombreCliente = "ALEJANDRO GARCIA",
            Celular = "5586785348",
            Zona = "HEROES CHALCO",
            FormaPago = "SEMANAL",
            DiaCobro = "LUNES",
            FotoCliente = "VENTAS_Images/addcdb49.FOTO CLIENTE.165310.jpg",
            FotoFachada = "IMG_VENTAS/addcdb49.FOTO FACHADA.165310.jpg",
            FotoContrato = "VENTAS_Images/addcdb49.FOTO CONTRATO.165310.jpg",
            ObservacionVenta = "Producto precontado en dos semanas",
            Vendedor = "JAKE",
            Usuario = "avedanojenny6@gmail.com",
            Cobrador = "ggab75218@gmail.com",
            Coordenadas = "19.260839,-98.831437",
            FechaPrimerCobro = new DateTime(2024, 12, 13),
            Estado = "EN COBRO",
            FechaActu = new DateTime(2024, 12, 10, 10, 54, 30),
            Cliente = "ALEJANDRO GARCIA",
            Producto = "GEL TICILT",
            Estado2 = "OPEN",
            ComisionVendedorPct = 0,
            Cobrar = "OK",
            ProductosCantidad = 1,
            ImporteTotal = 1490m,
            ProductLinesJson = productLines
        });

        _dbContext.Collections.Add(new CollectionEntity
        {
            IdCc = "657592be",
            IdV = "addcdb49",
            NumVenta = 1,
            NombreCliente = "ALEJANDRO GARCIA",
            ImporteCobro = 500,
            FechaCobro = new DateTime(2024, 12, 13),
            FechaCaptura = new DateTime(2024, 12, 13),
            ImporteTotal = 1490,
            ImporteRestante = 990,
            EstadoCc = "SI PAGO",
            Usuario = "ggab75218@gmail.com",
            ImporteAbonado = 500,
            Estatus = "PARCIAL",
            Zona = "HEROES CHALCO",
            DiaCobroPrevisto = "LUNES",
            DiaCobrado = "VIERNES"
        });
    }
}
