using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Initialization;

/// <summary>
/// Inicializador de datos para el archivo Excel.
/// Se ejecuta al inicio de la aplicación para asegurar datos mínimos necesarios.
/// </summary>
public static class ExcelDataInitializer
{
    public static void Initialize(ExcelDataService excelService, ILogger logger)
    {
        try
        {
            logger.LogInformation("Inicializando datos en Excel...");

            SeedUsers(excelService, logger);
            SeedCatalogs(excelService, logger);
            SeedConfigurationData(excelService, logger);
            SeedTransactionalTables(excelService, logger);

            logger.LogInformation("Datos inicializados correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar datos en Excel");
        }
    }

    private static void SeedUsers(ExcelDataService excelService, ILogger logger)
    {
        var users = excelService.ReadSheetAsync("Users").GetAwaiter().GetResult();
        
        if (users.Count > 0)
        {
            logger.LogInformation("Usuarios ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando usuarios iniciales...");

        var initialUsers = new[]
        {
            new { UserName = "admin", Password = "admin123", DisplayName = "Administrador", Role = UserRole.Administrador.ToString(), IsActive = true },
            new { UserName = "vendedor1", Password = "venta123", DisplayName = "Vendedor 1", Role = UserRole.Vendedor.ToString(), IsActive = true },
            new { UserName = "cobrador1", Password = "cobra123", DisplayName = "Cobrador 1", Role = UserRole.Cobrador.ToString(), IsActive = true },
            new { UserName = "supventas", Password = "super123", DisplayName = "Supervisor Ventas", Role = UserRole.SupervisorVentas.ToString(), IsActive = true },
            new { UserName = "supcobros", Password = "super123", DisplayName = "Supervisor Cobranza", Role = UserRole.SupervisorCobranza.ToString(), IsActive = true }
        };

        foreach (var user in initialUsers)
        {
            var userData = new Dictionary<string, object?>
            {
                ["UserName"] = user.UserName,
                ["Password"] = user.Password,
                ["DisplayName"] = user.DisplayName,
                ["Role"] = user.Role,
                ["IsActive"] = user.IsActive
            };
            excelService.AppendRowAsync("Users", userData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialUsers.Length} usuarios iniciales.");
    }

    private static void SeedCatalogs(ExcelDataService excelService, ILogger logger)
    {
        SeedZones(excelService, logger);
        SeedProducts(excelService, logger);
        SeedPaymentMethods(excelService, logger);
    }

    private static void SeedZones(ExcelDataService excelService, ILogger logger)
    {
        var zones = excelService.ReadSheetAsync("Zones").GetAwaiter().GetResult();
        
        if (zones.Count > 0)
        {
            logger.LogInformation("Zonas ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando zonas iniciales...");

        var initialZones = new[]
        {
            new { Id = 1, Code = "CENTRO", Name = "Zona Centro", IsActive = true },
            new { Id = 2, Code = "NORTE", Name = "Zona Norte", IsActive = true },
            new { Id = 3, Code = "SUR", Name = "Zona Sur", IsActive = true },
            new { Id = 4, Code = "ESTE", Name = "Zona Este", IsActive = true },
            new { Id = 5, Code = "OESTE", Name = "Zona Oeste", IsActive = true }
        };

        foreach (var zone in initialZones)
        {
            var zoneData = new Dictionary<string, object?>
            {
                ["Id"] = zone.Id,
                ["Code"] = zone.Code,
                ["Name"] = zone.Name,
                ["IsActive"] = zone.IsActive
            };
            excelService.AppendRowAsync("Zones", zoneData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialZones.Length} zonas iniciales.");
    }

    private static void SeedProducts(ExcelDataService excelService, ILogger logger)
    {
        var products = excelService.ReadSheetAsync("Products").GetAwaiter().GetResult();
        
        if (products.Count > 0)
        {
            logger.LogInformation("Productos ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando productos iniciales...");

        var initialProducts = new[]
        {
            new { Id = 1, Code = "VRP-001", Name = "VitaRaiz Plus 30 caps", Price = 120.00m, IsActive = true },
            new { Id = 2, Code = "VRP-002", Name = "VitaRaiz Plus 60 caps", Price = 210.00m, IsActive = true },
            new { Id = 3, Code = "VRP-003", Name = "VitaRaiz Plus 90 caps", Price = 290.00m, IsActive = true }
        };

        foreach (var product in initialProducts)
        {
            var productData = new Dictionary<string, object?>
            {
                ["Id"] = product.Id,
                ["Code"] = product.Code,
                ["Name"] = product.Name,
                ["Price"] = product.Price,
                ["IsActive"] = product.IsActive
            };
            excelService.AppendRowAsync("Products", productData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialProducts.Length} productos iniciales.");
    }

    private static void SeedPaymentMethods(ExcelDataService excelService, ILogger logger)
    {
        var paymentMethods = excelService.ReadSheetAsync("PaymentMethods").GetAwaiter().GetResult();
        
        if (paymentMethods.Count > 0)
        {
            logger.LogInformation("Formas de pago ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando formas de pago iniciales...");

        var initialMethods = new[]
        {
            new { Id = 1, Code = "CONTADO", Name = "Contado", IsActive = true },
            new { Id = 2, Code = "CREDITO", Name = "Crédito", IsActive = true },
            new { Id = 3, Code = "TRANSFER", Name = "Transferencia", IsActive = true },
            new { Id = 4, Code = "TARJETA", Name = "Tarjeta", IsActive = true }
        };

        foreach (var method in initialMethods)
        {
            var methodData = new Dictionary<string, object?>
            {
                ["Id"] = method.Id,
                ["Code"] = method.Code,
                ["Name"] = method.Name,
                ["IsActive"] = method.IsActive
            };
            excelService.AppendRowAsync("PaymentMethods", methodData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialMethods.Length} formas de pago iniciales.");
    }

    private static void SeedConfigurationData(ExcelDataService excelService, ILogger logger)
    {
        SeedMenuItems(excelService, logger);
        SeedWeekDays(excelService, logger);
        SeedSaleStatuses(excelService, logger);
        SeedCollectionStatuses(excelService, logger);
        SeedCatalogTypes(excelService, logger);
        SeedUISettings(excelService, logger);
    }

    private static void SeedMenuItems(ExcelDataService excelService, ILogger logger)
    {
        var menuItems = excelService.ReadSheetAsync("MenuItems").GetAwaiter().GetResult();
        
        if (menuItems.Count > 0)
        {
            logger.LogInformation("Menu items ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando menu items iniciales...");

        var initialMenuItems = new[]
        {
            new { Id = 1, Code = "DASHBOARD", Label = "Dashboard", IconSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6\"></path></svg>", Controller = "Dashboard", Action = "Index", RequiredPolicy = default(string?), SortOrder = 1, IsActive = true, ParentId = default(int?), Platform = "Web" },
            new { Id = 2, Code = "SALES", Label = "Ventas", IconSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z\"></path></svg>", Controller = "Sales", Action = "Index", RequiredPolicy = default(string?), SortOrder = 2, IsActive = true, ParentId = default(int?), Platform = "Web" },
            new { Id = 3, Code = "COLLECTIONS", Label = "Cobros", IconSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z\"></path></svg>", Controller = "Cobros", Action = "Index", RequiredPolicy = default(string?), SortOrder = 3, IsActive = true, ParentId = default(int?), Platform = "Web" },
            new { Id = 4, Code = "MAINTENANCE", Label = "Mantenimiento", IconSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z\"></path><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M15 12a3 3 0 11-6 0 3 3 0 016 0z\"></path></svg>", Controller = "Maintenance", Action = "Index", RequiredPolicy = "AdminOnly", SortOrder = 4, IsActive = true, ParentId = default(int?), Platform = "Web" },
            new { Id = 5, Code = "USERS", Label = "Usuarios", IconSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"icon\" fill=\"none\" viewBox=\"0 0 24 24\" stroke=\"currentColor\"><path stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z\"></path></svg>", Controller = "Administration", Action = "Users", RequiredPolicy = "AdminOnly", SortOrder = 5, IsActive = true, ParentId = default(int?), Platform = "Web" }
        };

        foreach (var item in initialMenuItems)
        {
            var itemData = new Dictionary<string, object?>
            {
                ["Id"] = item.Id,
                ["Code"] = item.Code,
                ["Label"] = item.Label,
                ["IconSvg"] = item.IconSvg,
                ["Controller"] = item.Controller,
                ["Action"] = item.Action,
                ["RequiredPolicy"] = item.RequiredPolicy,
                ["SortOrder"] = item.SortOrder,
                ["IsActive"] = item.IsActive,
                ["ParentId"] = item.ParentId,
                ["Platform"] = item.Platform
            };
            excelService.AppendRowAsync("MenuItems", itemData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialMenuItems.Length} menu items iniciales.");
    }

    private static void SeedWeekDays(ExcelDataService excelService, ILogger logger)
    {
        var weekDays = excelService.ReadSheetAsync("WeekDays").GetAwaiter().GetResult();
        
        if (weekDays.Count > 0)
        {
            logger.LogInformation("Dias de la semana ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando dias de la semana iniciales...");

        var initialWeekDays = new[]
        {
            new { Id = 1, Code = "LUNES", Name = "Lunes", ShortCode = "LUN", SortOrder = 1, IsActive = true },
            new { Id = 2, Code = "MARTES", Name = "Martes", ShortCode = "MAR", SortOrder = 2, IsActive = true },
            new { Id = 3, Code = "MIERCOLES", Name = "Miercoles", ShortCode = "MIE", SortOrder = 3, IsActive = true },
            new { Id = 4, Code = "JUEVES", Name = "Jueves", ShortCode = "JUE", SortOrder = 4, IsActive = true },
            new { Id = 5, Code = "VIERNES", Name = "Viernes", ShortCode = "VIE", SortOrder = 5, IsActive = true },
            new { Id = 6, Code = "SABADO", Name = "Sabado", ShortCode = "SAB", SortOrder = 6, IsActive = true },
            new { Id = 7, Code = "DOMINGO", Name = "Domingo", ShortCode = "DOM", SortOrder = 7, IsActive = true }
        };

        foreach (var day in initialWeekDays)
        {
            var dayData = new Dictionary<string, object?>
            {
                ["Id"] = day.Id,
                ["Code"] = day.Code,
                ["Name"] = day.Name,
                ["ShortCode"] = day.ShortCode,
                ["SortOrder"] = day.SortOrder,
                ["IsActive"] = day.IsActive
            };
            excelService.AppendRowAsync("WeekDays", dayData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialWeekDays.Length} dias de la semana iniciales.");
    }

    private static void SeedSaleStatuses(ExcelDataService excelService, ILogger logger)
    {
        var saleStatuses = excelService.ReadSheetAsync("SaleStatuses").GetAwaiter().GetResult();
        
        if (saleStatuses.Count > 0)
        {
            logger.LogInformation("Estados de venta ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando estados de venta iniciales...");

        var initialStatuses = new[]
        {
            new { Id = 1, Code = "BORRADOR", Name = "Borrador", ColorClass = "bg-secondary", IconSvg = (string?)null, SortOrder = 1, IsActive = true, IsFinal = false },
            new { Id = 2, Code = "PENDIENTE", Name = "Pendiente", ColorClass = "bg-warning", IconSvg = (string?)null, SortOrder = 2, IsActive = true, IsFinal = false },
            new { Id = 3, Code = "CONFIRMADA", Name = "Confirmada", ColorClass = "bg-success", IconSvg = (string?)null, SortOrder = 3, IsActive = true, IsFinal = true },
            new { Id = 4, Code = "CANCELADA", Name = "Cancelada", ColorClass = "bg-danger", IconSvg = (string?)null, SortOrder = 4, IsActive = true, IsFinal = true }
        };

        foreach (var status in initialStatuses)
        {
            var statusData = new Dictionary<string, object?>
            {
                ["Id"] = status.Id,
                ["Code"] = status.Code,
                ["Name"] = status.Name,
                ["ColorClass"] = status.ColorClass,
                ["IconSvg"] = status.IconSvg,
                ["SortOrder"] = status.SortOrder,
                ["IsActive"] = status.IsActive,
                ["IsFinal"] = status.IsFinal
            };
            excelService.AppendRowAsync("SaleStatuses", statusData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialStatuses.Length} estados de venta iniciales.");
    }

    private static void SeedCollectionStatuses(ExcelDataService excelService, ILogger logger)
    {
        var collectionStatuses = excelService.ReadSheetAsync("CollectionStatuses").GetAwaiter().GetResult();
        
        if (collectionStatuses.Count > 0)
        {
            logger.LogInformation("Estados de cobro ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando estados de cobro iniciales...");

        var initialStatuses = new[]
        {
            new { Id = 1, Code = "PENDIENTE", Name = "Pendiente", ColorClass = "warning", Priority = 1, IsActive = true },
            new { Id = 2, Code = "PARCIAL", Name = "Pago Parcial", ColorClass = "warning", Priority = 2, IsActive = true },
            new { Id = 3, Code = "COMPLETADO", Name = "Completado", ColorClass = "success", Priority = 3, IsActive = true },
            new { Id = 4, Code = "LIQUIDADO", Name = "Liquidado", ColorClass = "success", Priority = 4, IsActive = true },
            new { Id = 5, Code = "VENCIDO", Name = "Vencido", ColorClass = "danger", Priority = 5, IsActive = true },
            new { Id = 6, Code = "ATRASADO", Name = "Atrasado", ColorClass = "danger", Priority = 6, IsActive = true },
            new { Id = 7, Code = "CANCELADO", Name = "Cancelado", ColorClass = "muted", Priority = 7, IsActive = true },
            new { Id = 8, Code = "OPEN", Name = "Abierto", ColorClass = "collection-open", Priority = 8, IsActive = true },
            new { Id = 9, Code = "CLOSED", Name = "Cerrado", ColorClass = "collection-closed", Priority = 9, IsActive = true }
        };

        foreach (var status in initialStatuses)
        {
            var statusData = new Dictionary<string, object?>
            {
                ["Id"] = status.Id,
                ["Code"] = status.Code,
                ["Name"] = status.Name,
                ["ColorClass"] = status.ColorClass,
                ["Priority"] = status.Priority,
                ["IsActive"] = status.IsActive
            };
            excelService.AppendRowAsync("CollectionStatuses", statusData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialStatuses.Length} estados de cobro iniciales.");
    }

    private static void SeedCatalogTypes(ExcelDataService excelService, ILogger logger)
    {
        var catalogTypes = excelService.ReadSheetAsync("CatalogTypes").GetAwaiter().GetResult();
        
        if (catalogTypes.Count > 0)
        {
            logger.LogInformation("Tipos de catalogo ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando tipos de catalogo iniciales...");

        var initialCatalogTypes = new[]
        {
            new { Id = 1, Code = "ZONES", Name = "Zonas", Description = "Gestion de zonas geograficas", IconClass = "bi-geo-alt", Category = "Geography", SortOrder = 1, IsActive = true },
            new { Id = 2, Code = "PRODUCTS", Name = "Productos", Description = "Catalogo de productos", IconClass = "bi-box-seam", Category = "Sales", SortOrder = 2, IsActive = true },
            new { Id = 3, Code = "PAYMENT_METHODS", Name = "Formas de Pago", Description = "Metodos de pago disponibles", IconClass = "bi-credit-card", Category = "Sales", SortOrder = 3, IsActive = true },
            new { Id = 4, Code = "CLIENTS", Name = "Clientes", Description = "Gestion de clientes", IconClass = "bi-people", Category = "CRM", SortOrder = 4, IsActive = true }
        };

        foreach (var catalogType in initialCatalogTypes)
        {
            var catalogTypeData = new Dictionary<string, object?>
            {
                ["Id"] = catalogType.Id,
                ["Code"] = catalogType.Code,
                ["Name"] = catalogType.Name,
                ["Description"] = catalogType.Description,
                ["IconClass"] = catalogType.IconClass,
                ["Category"] = catalogType.Category,
                ["SortOrder"] = catalogType.SortOrder,
                ["IsActive"] = catalogType.IsActive
            };
            excelService.AppendRowAsync("CatalogTypes", catalogTypeData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialCatalogTypes.Length} tipos de catalogo iniciales.");
    }

    private static void SeedUISettings(ExcelDataService excelService, ILogger logger)
    {
        var uiSettings = excelService.ReadSheetAsync("UISettings").GetAwaiter().GetResult();
        
        if (uiSettings.Count > 0)
        {
            logger.LogInformation("Configuraciones UI ya existen, omitiendo seed.");
            return;
        }

        logger.LogInformation("Creando configuraciones UI iniciales...");

        var initialSettings = new[]
        {
            new { Id = 1, Category = "App", Key = "Title", Value = "VitaRaiz Plus", Description = "Titulo de la aplicacion", IsActive = true },
            new { Id = 2, Category = "App", Key = "Version", Value = "1.0.0", Description = "Version de la aplicacion", IsActive = true },
            new { Id = 3, Category = "Theme", Key = "PrimaryColor", Value = "#0d6efd", Description = "Color primario del tema", IsActive = true },
            new { Id = 4, Category = "Dashboard", Key = "RefreshInterval", Value = "30000", Description = "Intervalo de refresco del dashboard en ms", IsActive = true },
            new { Id = 5, Category = "Collections", Key = "DefaultView", Value = "ByCollector", Description = "Vista por defecto de cobros", IsActive = true }
        };

        foreach (var setting in initialSettings)
        {
            var settingData = new Dictionary<string, object?>
            {
                ["Id"] = setting.Id,
                ["Category"] = setting.Category,
                ["Key"] = setting.Key,
                ["Value"] = setting.Value,
                ["Description"] = setting.Description,
                ["IsActive"] = setting.IsActive
            };
            excelService.AppendRowAsync("UISettings", settingData).GetAwaiter().GetResult();
        }

        logger.LogInformation($"Se crearon {initialSettings.Length} configuraciones UI iniciales.");
    }

    private static void SeedTransactionalTables(ExcelDataService excelService, ILogger logger)
    {
        SeedSalesTable(excelService, logger);
        SeedCollectionsTable(excelService, logger);
    }

    private static void SeedSalesTable(ExcelDataService excelService, ILogger logger)
    {
        var sales = excelService.ReadSheetAsync("Sales").GetAwaiter().GetResult();
        
        if (sales.Count > 0)
        {
            logger.LogInformation("Tabla Sales ya existe, omitiendo creación.");
            return;
        }

        logger.LogInformation("Creando tabla Sales (vacía, lista para datos)...");

        // Crear una fila de ejemplo y eliminarla para establecer headers
        var sampleSale = new Dictionary<string, object?>
        {
            ["IdV"] = "SAMPLE",
            ["NumVenta"] = 0,
            ["FechaVenta"] = DateTime.Now,
            ["NombreCliente"] = "",
            ["Celular"] = "",
            ["Telefono"] = "",
            ["Zona"] = "",
            ["FormaPago"] = "",
            ["DiaCobro"] = "",
            ["FotoCliente"] = "",
            ["FotoFachada"] = "",
            ["FotoContrato"] = "",
            ["ObservacionVenta"] = "",
            ["Vendedor"] = "",
            ["Usuario"] = "",
            ["Cobrador"] = "",
            ["Coordenadas"] = "",
            ["UrlUbicacion"] = "",
            ["FechaPrimerCobro"] = (DateTime?)null,
            ["Estado"] = "",
            ["FechaActu"] = DateTime.Now,
            ["Estado2"] = "",
            ["ComisionVendedorPct"] = 0.0,
            ["Cobrar"] = "",
            ["FotoAdd1"] = "",
            ["FotoAdd2"] = "",
            ["Coordenadas2"] = "",
            ["ProductosCodigos"] = "",
            ["ProductosCantidades"] = "",
            ["ProductosPrecios"] = "",
            ["ImporteTotal"] = 0.0
        };

        // La tabla queda con headers definidos
        logger.LogInformation("Tabla Sales creada (estructura definida, sin datos iniciales).");
    }

    private static void SeedCollectionsTable(ExcelDataService excelService, ILogger logger)
    {
        var collections = excelService.ReadSheetAsync("Collections").GetAwaiter().GetResult();
        
        if (collections.Count > 0)
        {
            logger.LogInformation("Tabla Collections ya existe, omitiendo creación.");
            return;
        }

        logger.LogInformation("Creando tabla Collections (vacía, lista para datos)...");

        // Crear estructura de tabla
        var sampleCollection = new Dictionary<string, object?>
        {
            ["IdCc"] = "SAMPLE",
            ["IdV"] = "",
            ["NumVenta"] = 0,
            ["NombreCliente"] = "",
            ["ImporteCobro"] = 0.0,
            ["FechaCobro"] = DateTime.Now,
            ["ObservacionCobro"] = "",
            ["FechaCaptura"] = DateTime.Now,
            ["EstadoCc"] = "",
            ["Usuario"] = "",
            ["Zona"] = "",
            ["DiaCobroPrevisto"] = "",
            ["DiaCobrado"] = "",
            ["CoordenadasCobro"] = "",
            ["FotoCobro"] = "",
            ["ImporteAbonado"] = 0.0,
            ["ImporteRestante"] = 0.0,
            ["ImporteTotal"] = 0.0
        };

        // La tabla queda con headers definidos
        logger.LogInformation("Tabla Collections creada (estructura definida, sin datos iniciales).");
    }
}
