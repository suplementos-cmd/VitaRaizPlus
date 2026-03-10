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
}

public sealed class JsonSalesRepository : ISalesRepository
{
    private readonly string _filePath;
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
        _filePath = Path.Combine(dataDirectory, "ventas.json");
        EnsureSeedData();
    }

    public SalesCatalogs GetCatalogs()
    {
        return new SalesCatalogs(
            Zones:
            [
                new CatalogOption("HEROES CHALCO", "Heroes Chalco"),
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
                new CatalogOption("SABADO", "Sabado")
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
                new CatalogOption("ELENA", "Elena")
            ]);
    }

    public IReadOnlyList<SaleRecord> GetAll()
    {
        lock (_sync)
        {
            return Load().OrderByDescending(x => x.FechaActu).ToArray();
        }
    }

    public SaleRecord? GetById(string idV)
    {
        lock (_sync)
        {
            return Load().FirstOrDefault(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase));
        }
    }

    public SaleRecord Create(SaleFormInput input)
    {
        lock (_sync)
        {
            var data = Load();
            var newId = Guid.NewGuid().ToString("N")[..8];
            var nextNum = data.Count == 0 ? -100 : data.Min(x => x.NumVenta) - 1;
            var record = Map(input, newId, nextNum);
            data.Add(record);
            Save(data);
            return record;
        }
    }

    public SaleRecord Update(string idV, SaleFormInput input)
    {
        lock (_sync)
        {
            var data = Load();
            var existing = data.FirstOrDefault(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("No se encontro la venta.");

            var updated = Map(input, existing.IdV, existing.NumVenta);
            var index = data.FindIndex(x => string.Equals(x.IdV, idV, StringComparison.OrdinalIgnoreCase));
            data[index] = updated;
            Save(data);
            return updated;
        }
    }

    private SaleRecord Map(SaleFormInput input, string idV, int numVenta)
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
        if (File.Exists(_filePath))
        {
            return;
        }

        var seed = new List<SaleRecord>
        {
            new()
            {
                IdV = "addcdb49",
                NumVenta = -137,
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
                Cobrador = "SILVIA",
                Coordenadas = "19.260839,-98.831437",
                FechaPrimerCobro = new DateTime(2024, 12, 13),
                Estado = "LIQUIDADO BUEN CLIENTE",
                FechaActu = new DateTime(2024, 12, 10, 10, 54, 30),
                Cliente = "ALEJANDRO GARCIA",
                Producto = "GEL TICILT",
                Estado2 = "CLOSED",
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

        Save(seed);
    }

    private List<SaleRecord> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var raw = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var data = JsonSerializer.Deserialize<List<SaleRecord>>(raw, _jsonOptions);
        return data ?? [];
    }

    private void Save(List<SaleRecord> data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
