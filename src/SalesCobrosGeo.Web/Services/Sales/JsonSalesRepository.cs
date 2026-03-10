using System.Globalization;
using System.Text.Json;
using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Services.Sales;

public interface ISalesRepository
{
    SalesCatalogs GetCatalogs();
    IReadOnlyList<SaleRecord> GetAll();
    SaleRecord? GetById(string idV);
    SaleRecord Create(SaleFormInput input);
    SaleRecord Update(string idV, SaleFormInput input);

    IReadOnlyList<CollectorPortfolioItem> GetCollectorPortfolio(string? profile);
    CollectorPortfolioItem? GetPortfolioItem(string idV, string? profile);
    IReadOnlyList<CollectionRecord> GetCollections(string? profile = null, string? idV = null);
    CollectionRecord RegisterCollection(CollectionFormInput input);
    IReadOnlyList<CatalogOption> GetCollectorProfiles();
}

public sealed class JsonSalesRepository : ISalesRepository
{
    private readonly string _salesFilePath;
    private readonly string _collectionsFilePath;
    private readonly Lock _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonSalesRepository(IWebHostEnvironment env)
    {
        var dataDirectory = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _salesFilePath = Path.Combine(dataDirectory, "ventas.json");
        _collectionsFilePath = Path.Combine(dataDirectory, "cobros.json");
        EnsureSeedData();
    }

    public SalesCatalogs GetCatalogs()
    {
        return new SalesCatalogs(
            Zones:
            [
                new CatalogOption("HEROES CHALCO", "Heroes Chalco"),
                new CatalogOption("JARDINES", "Jardines"),
                new CatalogOption("XICO", "Xico"),
                new CatalogOption("CENTRO", "Centro"),
                new CatalogOption("NORTE", "Norte")
            ],
            PaymentMethods:
            [
                new CatalogOption("SEMANAL", "Semanal"),
                new CatalogOption("QUINCENAL", "Quincenal"),
                new CatalogOption("CONTADO", "Contado")
            ],
            CollectionDays:
            [
                new CatalogOption("LUNES", "Lunes"),
                new CatalogOption("MARTES", "Martes"),
                new CatalogOption("MIERCOLES", "Miercoles"),
                new CatalogOption("JUEVES", "Jueves"),
                new CatalogOption("VIERNES", "Viernes"),
                new CatalogOption("SABADO", "Sabado"),
                new CatalogOption("DOMINGO", "Domingo")
            ],
            Products:
            [
                new ProductOption("GEL TICILT", "Gel Ticilt", 1490m),
                new ProductOption("VITARAIZ 30", "VitaRaiz 30 caps", 780m),
                new ProductOption("VITARAIZ 60", "VitaRaiz 60 caps", 1290m)
            ],
            Sellers:
            [
                new CatalogOption("JAKE", "Jake"),
                new CatalogOption("LUCIA", "Lucia"),
                new CatalogOption("PEDRO", "Pedro")
            ],
            Collectors:
            [
                new CatalogOption("SILVIA", "Silvia"),
                new CatalogOption("MARIO", "Mario"),
                new CatalogOption("ELENA", "Elena"),
                new CatalogOption("jakelinepink88@gmail.com", "jakelinepink88@gmail.com"),
                new CatalogOption("ggab75218@gmail.com", "ggab75218@gmail.com")
            ]);
    }

    public IReadOnlyList<SaleRecord> GetAll()
    {
        lock (_sync)
        {
            return LoadSales().OrderByDescending(x => x.FechaActu).ToArray();
        }
    }

    public SaleRecord? GetById(string idV)
    {
        lock (_sync)
        {
            return LoadSales().FirstOrDefault(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase));
        }
    }

    public SaleRecord Create(SaleFormInput input)
    {
        lock (_sync)
        {
            var data = LoadSales();
            var newId = Guid.NewGuid().ToString("N")[..8];
            var nextNum = data.Count == 0 ? 1 : data.Max(x => x.NumVenta) + 1;
            var record = MapSale(input, newId, nextNum);
            data.Add(record);
            SaveSales(data);
            return record;
        }
    }

    public SaleRecord Update(string idV, SaleFormInput input)
    {
        lock (_sync)
        {
            var data = LoadSales();
            var existing = data.FirstOrDefault(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("No se encontro la venta.");

            var updated = MapSale(input, existing.IdV, existing.NumVenta);
            var index = data.FindIndex(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase));
            data[index] = updated;
            SaveSales(data);
            return updated;
        }
    }

    public IReadOnlyList<CollectorPortfolioItem> GetCollectorPortfolio(string? profile)
    {
        lock (_sync)
        {
            var normalized = NormalizeProfile(profile);
            var sales = LoadSales();
            var collections = LoadCollections();

            var query = sales.Select(sale =>
            {
                var saleCollections = collections.Where(c => c.IdV == sale.IdV).OrderBy(c => c.FechaCobro).ToList();
                var abonado = saleCollections.LastOrDefault()?.ImporteAbonado ?? 0m;
                var restante = Math.Max(0m, sale.ImporteTotal - abonado);
                var estatus = restante <= 0 ? "LIQUIDADO" : (abonado > 0 ? "PARCIAL" : "POR INICIAR");
                return new CollectorPortfolioItem
                {
                    IdV = sale.IdV,
                    NumVenta = sale.NumVenta,
                    NombreCliente = sale.NombreCliente,
                    Zona = sale.Zona,
                    DiaCobroPrevisto = sale.DiaCobro,
                    Cobrador = string.IsNullOrWhiteSpace(sale.Cobrador) ? sale.Usuario : sale.Cobrador,
                    ImporteTotal = sale.ImporteTotal,
                    ImporteAbonado = abonado,
                    ImporteRestante = restante,
                    Estatus = estatus
                };
            });

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                query = query.Where(x => x.Cobrador.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(x => x.Estatus).ThenBy(x => x.NumVenta).ToArray();
        }
    }

    public CollectorPortfolioItem? GetPortfolioItem(string idV, string? profile)
    {
        lock (_sync)
        {
            return GetCollectorPortfolio(profile).FirstOrDefault(x => x.IdV.Equals(idV, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<CollectionRecord> GetCollections(string? profile = null, string? idV = null)
    {
        lock (_sync)
        {
            var normalized = NormalizeProfile(profile);
            var records = LoadCollections().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                records = records.Where(x => x.Usuario.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(idV))
            {
                records = records.Where(x => x.IdV.Equals(idV, StringComparison.OrdinalIgnoreCase));
            }

            return records.OrderByDescending(x => x.FechaCaptura).ToArray();
        }
    }

    public CollectionRecord RegisterCollection(CollectionFormInput input)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(input.IdV))
            {
                throw new InvalidOperationException("Venta invalida.");
            }

            if (input.ImporteCobro <= 0)
            {
                throw new InvalidOperationException("El importe de cobro debe ser mayor a 0.");
            }

            if (string.IsNullOrWhiteSpace(input.Usuario))
            {
                throw new InvalidOperationException("Usuario cobrador es obligatorio.");
            }

            var sales = LoadSales();
            var collections = LoadCollections();
            var sale = sales.FirstOrDefault(x => x.IdV.Equals(input.IdV, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("No se encontro la venta.");

            var totalAbonado = collections.Where(x => x.IdV == sale.IdV).Select(x => x.ImporteCobro).DefaultIfEmpty(0m).Sum();
            var restanteActual = Math.Max(0m, sale.ImporteTotal - totalAbonado);

            if (input.ImporteCobro > restanteActual)
            {
                throw new InvalidOperationException($"El cobro no puede superar el restante ({restanteActual:0.00}).");
            }

            var nuevoAbonado = totalAbonado + input.ImporteCobro;
            var nuevoRestante = Math.Max(0m, sale.ImporteTotal - nuevoAbonado);
            var estatus = nuevoRestante <= 0 ? "LIQUIDADO" : (nuevoAbonado > 0 ? "PARCIAL" : "POR INICIAR");

            var record = new CollectionRecord
            {
                IdCc = Guid.NewGuid().ToString("N")[..8],
                IdV = sale.IdV,
                NumVenta = sale.NumVenta,
                NombreCliente = sale.NombreCliente,
                ImporteCobro = input.ImporteCobro,
                FechaCobro = input.FechaCobro,
                ObservacionCobro = input.ObservacionCobro?.Trim(),
                FechaCaptura = DateTime.Now,
                ImporteTotal = sale.ImporteTotal,
                ImporteRestante = nuevoRestante,
                EstadoCc = "SI PAGO",
                Usuario = input.Usuario.Trim(),
                ImporteAbonado = nuevoAbonado,
                Estatus = estatus,
                Zona = sale.Zona,
                DiaCobroPrevisto = sale.DiaCobro,
                DiaCobrado = GetDayName(input.FechaCobro),
                CoordenadasCobro = input.CoordenadasCobro?.Trim()
            };

            collections.Add(record);
            SaveCollections(collections);

            sale.Estado = estatus == "LIQUIDADO" ? "LIQUIDADO" : "EN COBRO";
            sale.Estado2 = estatus == "LIQUIDADO" ? "CLOSED" : "OPEN";
            sale.FechaActu = DateTime.Now;
            if (sale.FechaPrimerCobro is null)
            {
                sale.FechaPrimerCobro = input.FechaCobro;
            }
            SaveSales(sales);

            return record;
        }
    }

    public IReadOnlyList<CatalogOption> GetCollectorProfiles()
    {
        lock (_sync)
        {
            var fromCatalog = GetCatalogs().Collectors.Select(x => x.Code);
            var fromCollections = LoadCollections().Select(x => x.Usuario);
            var fromSales = LoadSales().Select(x => x.Cobrador);

            return fromCatalog
                .Concat(fromCollections)
                .Concat(fromSales)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .Select(x => new CatalogOption(x, x))
                .ToArray();
        }
    }

    private SaleRecord MapSale(SaleFormInput input, string idV, int numVenta)
    {
        var validLines = input.Productos
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductCode) && p.Quantity > 0)
            .ToList();

        if (validLines.Count == 0)
        {
            throw new InvalidOperationException("Debe agregar al menos un producto.");
        }

        var catalogs = GetCatalogs();
        foreach (var line in validLines)
        {
            var match = catalogs.Products.FirstOrDefault(p => p.Code == line.ProductCode);
            if (match is null)
            {
                throw new InvalidOperationException($"Producto invalido: {line.ProductCode}");
            }

            line.UnitPrice ??= match.Price;
        }

        var total = validLines.Sum(x => x.Quantity * (x.UnitPrice ?? 0m));

        return new SaleRecord
        {
            IdV = idV,
            NumVenta = numVenta,
            FechaVenta = input.FechaVenta,
            NombreCliente = input.NombreCliente.Trim(),
            Celular = input.Celular.Trim(),
            Telefono = input.Telefono?.Trim(),
            Zona = input.Zona,
            FormaPago = input.FormaPago,
            DiaCobro = input.DiaCobro,
            FotoCliente = input.FotoCliente?.Trim(),
            FotoFachada = input.FotoFachada?.Trim(),
            FotoContrato = input.FotoContrato?.Trim(),
            ObservacionVenta = input.ObservacionVenta?.Trim(),
            Vendedor = input.Vendedor,
            Usuario = input.Usuario.Trim(),
            Cobrador = input.Cobrador,
            Coordenadas = input.Coordenadas.Trim(),
            UrlUbicacion = input.UrlUbicacion?.Trim(),
            FechaPrimerCobro = input.FechaPrimerCobro,
            Estado = input.Estado,
            FechaActu = DateTime.Now,
            Cliente = input.NombreCliente.Trim(),
            Producto = string.Join(", ", validLines.Select(x => x.ProductCode)),
            Estado2 = input.Estado2,
            ComisionVendedorPct = input.ComisionVendedorPct,
            Cobrar = input.Cobrar,
            FotoAdd1 = input.FotoAdd1?.Trim(),
            FotoAdd2 = input.FotoAdd2?.Trim(),
            Coordenadas2 = input.Coordenadas2?.Trim(),
            ProductosCantidad = validLines.Sum(x => x.Quantity),
            ImporteTotal = total,
            Productos = validLines
        };
    }

    private void EnsureSeedData()
    {
        if (!File.Exists(_salesFilePath))
        {
            var seedSales = new List<SaleRecord>
            {
                new()
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
                    ImporteTotal = 1490,
                    Productos =
                    [
                        new SaleProductLineInput
                        {
                            ProductCode = "GEL TICILT",
                            Quantity = 1,
                            UnitPrice = 1490
                        }
                    ]
                }
            };

            SaveSales(seedSales);
        }

        if (!File.Exists(_collectionsFilePath))
        {
            var seedCollections = new List<CollectionRecord>
            {
                new()
                {
                    IdCc = "657592be",
                    IdV = "a82ddb1a",
                    NumVenta = 1,
                    NombreCliente = "FILIBERTA ENCARNACION",
                    ImporteCobro = 500,
                    FechaCobro = new DateTime(2024, 10, 20),
                    FechaCaptura = new DateTime(2024, 10, 20),
                    ImporteTotal = 500,
                    ImporteRestante = 0,
                    EstadoCc = "SI PAGO",
                    Usuario = "ggab75218@gmail.com",
                    ImporteAbonado = 500,
                    Estatus = "LIQUIDADO",
                    Zona = "JARDINES",
                    DiaCobroPrevisto = "SABADO",
                    DiaCobrado = "DOMINGO"
                },
                new()
                {
                    IdCc = "e8b67417",
                    IdV = "ff533734",
                    NumVenta = 2,
                    NombreCliente = "MANUEL MONDRAGON AGUILAR",
                    ImporteCobro = 990,
                    FechaCobro = new DateTime(2024, 10, 20),
                    ObservacionCobro = "Antes de un mes $990",
                    FechaCaptura = new DateTime(2024, 10, 20),
                    ImporteTotal = 990,
                    ImporteRestante = 0,
                    EstadoCc = "SI PAGO",
                    Usuario = "jakelinepink88@gmail.com",
                    ImporteAbonado = 990,
                    Estatus = "LIQUIDADO",
                    Zona = "XICO",
                    DiaCobroPrevisto = "VIERNES",
                    DiaCobrado = "DOMINGO"
                },
                new()
                {
                    IdCc = "14ec1b9f",
                    IdV = "a8490ad5",
                    NumVenta = 3,
                    NombreCliente = "EDITH SEGURA LOPEZ",
                    ImporteCobro = 100,
                    FechaCobro = new DateTime(2024, 10, 23),
                    FechaCaptura = new DateTime(2024, 10, 24),
                    ImporteTotal = 1190,
                    ImporteRestante = 1090,
                    EstadoCc = "SI PAGO",
                    Usuario = "jakelinepink88@gmail.com",
                    ImporteAbonado = 100,
                    Estatus = "POR INICIAR",
                    Zona = "CULTURAS",
                    DiaCobroPrevisto = "MIERCOLES",
                    DiaCobrado = "MIERCOLES"
                }
            };

            SaveCollections(seedCollections);
        }
    }

    private List<SaleRecord> LoadSales()
    {
        return LoadJson<List<SaleRecord>>(_salesFilePath) ?? [];
    }

    private List<CollectionRecord> LoadCollections()
    {
        return LoadJson<List<CollectionRecord>>(_collectionsFilePath) ?? [];
    }

    private T? LoadJson<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        var raw = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
    }

    private void SaveSales(List<SaleRecord> data)
    {
        SaveJson(_salesFilePath, data);
    }

    private void SaveCollections(List<CollectionRecord> data)
    {
        SaveJson(_collectionsFilePath, data);
    }

    private void SaveJson<T>(string filePath, T data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(filePath, json);
    }

    private static string NormalizeProfile(string? profile)
    {
        return string.IsNullOrWhiteSpace(profile) ? string.Empty : profile.Trim();
    }

    private static string GetDayName(DateTime date)
    {
        return date.ToString("dddd", new CultureInfo("es-ES")).ToUpperInvariant();
    }
}
