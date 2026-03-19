using SalesCobrosGeo.Api.Business;
using System.Globalization;

namespace SalesCobrosGeo.Api.Data;

/// <summary>
/// Implementación de ISalesStore usando Excel como almacenamiento
/// Maneja ventas (Sales) y cobros (Collections)
/// </summary>
public sealed class ExcelSalesStore : ISalesStore
{
    private readonly ExcelDataService _excelService;
    private readonly ILogger<ExcelSalesStore> _logger;

    public ExcelSalesStore(ExcelDataService excelService, ILogger<ExcelSalesStore> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    #region Sales

    public async Task<IReadOnlyList<SaleRecordDto>> GetAllSalesAsync()
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync("Sales");
            return rows.Select(MapRowToSaleRecord).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las ventas desde Excel");
            return Array.Empty<SaleRecordDto>();
        }
    }

    public async Task<SaleRecordDto?> GetSaleByIdAsync(string idV)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync("Sales");
            var saleRow = rows.FirstOrDefault(row => GetString(row, "IdV") == idV);
            return saleRow != null ? MapRowToSaleRecord(saleRow) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener venta {IdV} desde Excel", idV);
            return null;
        }
    }

    public async Task<SaleRecordDto> CreateSaleAsync(SaleFormInputDto input)
    {
        try
        {
            // Generar IdV y NumVenta
            var allSales = await _excelService.ReadSheetAsync("Sales");
            var maxNumVenta = allSales.Count > 0
                ? allSales.Max(row => GetInt(row, "NumVenta") ?? 0)
                : 0;
            
            var numVenta = maxNumVenta + 1;
            var idV = $"V{numVenta:D6}";

            // Serializar productos
            var (productsCodes, productsQuantities, productsPrices, totalAmount, productsCount) = SerializeProducts(input.Productos);

            var saleData = new Dictionary<string, object?>
            {
                ["IdV"] = idV,
                ["NumVenta"] = numVenta,
                ["FechaVenta"] = input.FechaVenta,
                ["NombreCliente"] = input.NombreCliente,
                ["Celular"] = input.Celular,
                ["Telefono"] = input.Telefono,
                ["Zona"] = input.Zona,
                ["FormaPago"] = input.FormaPago,
                ["DiaCobro"] = input.DiaCobro,
                ["FotoCliente"] = input.FotoCliente,
                ["FotoFachada"] = input.FotoFachada,
                ["FotoContrato"] = input.FotoContrato,
                ["ObservacionVenta"] = input.ObservacionVenta,
                ["Vendedor"] = input.Vendedor,
                ["Usuario"] = input.Usuario,
                ["Cobrador"] = input.Cobrador,
                ["Coordenadas"] = input.Coordenadas,
                ["UrlUbicacion"] = input.UrlUbicacion,
                ["FechaPrimerCobro"] = input.FechaPrimerCobro,
                ["Estado"] = input.Estado,
                ["FechaActu"] = DateTime.Now,
                ["Estado2"] = input.Estado2,
                ["ComisionVendedorPct"] = input.ComisionVendedorPct,
                ["Cobrar"] = input.Cobrar,
                ["FotoAdd1"] = input.FotoAdd1,
                ["FotoAdd2"] = input.FotoAdd2,
                ["Coordenadas2"] = input.Coordenadas2,
                ["ProductosCodigos"] = productsCodes,
                ["ProductosCantidades"] = productsQuantities,
                ["ProductosPrecios"] = productsPrices,
                ["ImporteTotal"] = totalAmount
            };

            await _excelService.AppendRowAsync("Sales", saleData);
            _logger.LogInformation("Venta creada: {IdV}", idV);

            return await GetSaleByIdAsync(idV) ?? MapInputToRecord(input, idV, numVenta, totalAmount, productsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear venta en Excel");
            throw;
        }
    }

    public async Task<SaleRecordDto> UpdateSaleAsync(string idV, SaleFormInputDto input)
    {
        try
        {
            var existingSale = await GetSaleByIdAsync(idV);
            if (existingSale == null)
            {
                throw new KeyNotFoundException($"Venta {idV} no encontrada");
            }

            var numVenta = existingSale.NumVenta;

            // Serializar productos
            var (productsCodes, productsQuantities, productsPrices, totalAmount, productsCount) = SerializeProducts(input.Productos);

            await _excelService.UpdateRowsAsync("Sales",
                row => GetString(row, "IdV") == idV,
                row =>
                {
                    row["FechaVenta"] = input.FechaVenta;
                    row["NombreCliente"] = input.NombreCliente;
                    row["Celular"] = input.Celular;
                    row["Telefono"] = input.Telefono;
                    row["Zona"] = input.Zona;
                    row["FormaPago"] = input.FormaPago;
                    row["DiaCobro"] = input.DiaCobro;
                    row["FotoCliente"] = input.FotoCliente;
                    row["FotoFachada"] = input.FotoFachada;
                    row["FotoContrato"] = input.FotoContrato;
                    row["ObservacionVenta"] = input.ObservacionVenta;
                    row["Vendedor"] = input.Vendedor;
                    row["Usuario"] = input.Usuario;
                    row["Cobrador"] = input.Cobrador;
                    row["Coordenadas"] = input.Coordenadas;
                    row["UrlUbicacion"] = input.UrlUbicacion;
                    row["FechaPrimerCobro"] = input.FechaPrimerCobro;
                    row["Estado"] = input.Estado;
                    row["FechaActu"] = DateTime.Now;
                    row["Estado2"] = input.Estado2;
                    row["ComisionVendedorPct"] = input.ComisionVendedorPct;
                    row["Cobrar"] = input.Cobrar;
                    row["FotoAdd1"] = input.FotoAdd1;
                    row["FotoAdd2"] = input.FotoAdd2;
                    row["Coordenadas2"] = input.Coordenadas2;
                    row["ProductosCodigos"] = productsCodes;
                    row["ProductosCantidades"] = productsQuantities;
                    row["ProductosPrecios"] = productsPrices;
                    row["ImporteTotal"] = totalAmount;
                });

            _logger.LogInformation("Venta actualizada: {IdV}", idV);

            return await GetSaleByIdAsync(idV) ?? MapInputToRecord(input, idV, numVenta, totalAmount, productsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar venta {IdV} en Excel", idV);
            throw;
        }
    }

    #endregion

    #region Collections

    public async Task<IReadOnlyList<CollectionRecordDto>> GetAllCollectionsAsync()
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync("Collections");
            return rows.Select(MapRowToCollectionRecord).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los cobros desde Excel");
            return Array.Empty<CollectionRecordDto>();
        }
    }

    public async Task<IReadOnlyList<CollectionRecordDto>> GetCollectionsBySaleAsync(string idV)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync("Collections");
            return rows
                .Where(row => GetString(row, "IdV") == idV)
                .Select(MapRowToCollectionRecord)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobros de venta {IdV} desde Excel", idV);
            return Array.Empty<CollectionRecordDto>();
        }
    }

    public async Task<CollectionRecordDto?> GetCollectionByIdAsync(string idCc)
    {
        try
        {
            var rows = await _excelService.ReadSheetAsync("Collections");
            var collectionRow = rows.FirstOrDefault(row => GetString(row, "IdCc") == idCc);
            return collectionRow != null ? MapRowToCollectionRecord(collectionRow) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobro {IdCc} desde Excel", idCc);
            return null;
        }
    }

    public async Task<CollectionRecordDto> RegisterCollectionAsync(CollectionFormInputDto input)
    {
        try
        {
            // Obtener datos de la venta
            var sale = await GetSaleByIdAsync(input.IdV);
            if (sale == null)
            {
                throw new KeyNotFoundException($"Venta {input.IdV} no encontrada");
            }

            // Generar IdCc
            var allCollections = await _excelService.ReadSheetAsync("Collections");
            var maxIdNum = allCollections
                .Select(row => GetString(row, "IdCc"))
                .Where(id => id.StartsWith("C"))
                .Select(id => int.TryParse(id.Substring(1), out var num) ? num : 0)
                .DefaultIfEmpty(0)
                .Max();
            
            var idCc = $"C{(maxIdNum + 1):D6}";

            // Calcular importes
            var previousCollections = await GetCollectionsBySaleAsync(input.IdV);
            var importeAbonado = previousCollections.Sum(c => c.ImporteCobro);
            var importeRestante = sale.ImporteTotal - importeAbonado - input.ImporteCobro;

            var collectionData = new Dictionary<string, object?>
            {
                ["IdCc"] = idCc,
                ["IdV"] = input.IdV,
                ["NumVenta"] = sale.NumVenta,
                ["NombreCliente"] = sale.NombreCliente,
                ["ImporteCobro"] = input.ImporteCobro,
                ["FechaCobro"] = input.FechaCobro,
                ["ObservacionCobro"] = input.ObservacionCobro,
                ["FechaCaptura"] = DateTime.Now,
                ["EstadoCc"] = importeRestante <= 0 ? "COMPLETADO" : "PARCIAL",
                ["Usuario"] = input.Usuario,
                ["Zona"] = sale.Zona,
                ["DiaCobroPrevisto"] = sale.DiaCobro,
                ["DiaCobrado"] = input.FechaCobro.ToString("dddd", new CultureInfo("es-ES")),
                ["CoordenadasCobro"] = input.CoordenadasCobro,
                ["FotoCobro"] = input.FotoCobro,
                ["ImporteAbonado"] = importeAbonado + input.ImporteCobro,
                ["ImporteRestante"] = importeRestante,
                ["ImporteTotal"] = sale.ImporteTotal
            };

            await _excelService.AppendRowAsync("Collections", collectionData);
            _logger.LogInformation("Cobro registrado: {IdCc} para venta {IdV}", idCc, input.IdV);

            return await GetCollectionByIdAsync(idCc) ?? MapCollectionInputToRecord(input, idCc, sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cobro en Excel");
            throw;
        }
    }

    public async Task<CollectionRecordDto> UpdateCollectionAsync(string idCc, CollectionFormInputDto input)
    {
        try
        {
            var existingCollection = await GetCollectionByIdAsync(idCc);
            if (existingCollection == null)
            {
                throw new KeyNotFoundException($"Cobro {idCc} no encontrado");
            }

            var idV = existingCollection.IdV;
            var sale = await GetSaleByIdAsync(idV);
            
            if (sale == null)
            {
                throw new KeyNotFoundException($"Venta {idV} no encontrada");
            }

            // Recalcular importes excluyendo este cobro
            var otherCollections = await GetCollectionsBySaleAsync(idV);
            var importeOtherCollections = otherCollections
                .Where(c => c.IdCc != idCc)
                .Sum(c => c.ImporteCobro);
            var importeRestante = sale.ImporteTotal - importeOtherCollections - input.ImporteCobro;

            await _excelService.UpdateRowsAsync("Collections",
                row => GetString(row, "IdCc") == idCc,
                row =>
                {
                    row["ImporteCobro"] = input.ImporteCobro;
                    row["FechaCobro"] = input.FechaCobro;
                    row["ObservacionCobro"] = input.ObservacionCobro;
                    row["EstadoCc"] = importeRestante <= 0 ? "COMPLETADO" : "PARCIAL";
                    row["Usuario"] = input.Usuario;
                    row["DiaCobrado"] = input.FechaCobro.ToString("dddd", new CultureInfo("es-ES"));
                    row["CoordenadasCobro"] = input.CoordenadasCobro;
                    row["FotoCobro"] = input.FotoCobro;
                    row["ImporteAbonado"] = importeOtherCollections + input.ImporteCobro;
                    row["ImporteRestante"] = importeRestante;
                });

            _logger.LogInformation("Cobro actualizado: {IdCc}", idCc);

            return await GetCollectionByIdAsync(idCc) ?? MapCollectionInputToRecord(input, idCc, sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cobro {IdCc} en Excel", idCc);
            throw;
        }
    }

    #endregion

    #region Collector Portfolio

    public async Task<IReadOnlyList<CollectorPortfolioItemDto>> GetCollectorPortfolioAsync(string? collectorUsername = null)
    {
        try
        {
            var sales = await GetAllSalesAsync();
            var collections = await GetAllCollectionsAsync();

            // Filtrar ventas por cobrador si se especifica
            var filteredSales = string.IsNullOrEmpty(collectorUsername)
                ? sales
                : sales.Where(s => s.Cobrador.Equals(collectorUsername, StringComparison.OrdinalIgnoreCase));

            var portfolio = filteredSales
                .Where(s => s.Estado2 == "OPEN" && s.Cobrar == "OK")
                .Select(sale =>
                {
                    var saleCollections = collections.Where(c => c.IdV == sale.IdV).ToList();
                    var importeAbonado = saleCollections.Sum(c => c.ImporteCobro);
                    var importeRestante = sale.ImporteTotal - importeAbonado;
                    var ultimaFechaCobro = saleCollections.OrderByDescending(c => c.FechaCobro).FirstOrDefault()?.FechaCobro;

                    return new CollectorPortfolioItemDto
                    {
                        IdV = sale.IdV,
                        NumVenta = sale.NumVenta,
                        NombreCliente = sale.NombreCliente,
                        ImporteTotal = sale.ImporteTotal,
                        ImporteAbonado = importeAbonado,
                        ImporteRestante = importeRestante,
                        Zona = sale.Zona,
                        DiaCobroPrevisto = sale.DiaCobro,
                        UltimaFechaCobro = ultimaFechaCobro,
                        EstadoCobro = importeRestante <= 0 ? "COMPLETADO" : "PENDIENTE"
                    };
                })
                .Where(p => p.ImporteRestante > 0)
                .ToList();

            return portfolio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener portafolio del cobrador desde Excel");
            return Array.Empty<CollectorPortfolioItemDto>();
        }
    }

    public async Task<CollectorPortfolioItemDto?> GetPortfolioItemAsync(string idV, string? collectorUsername = null)
    {
        try
        {
            var portfolio = await GetCollectorPortfolioAsync(collectorUsername);
            return portfolio.FirstOrDefault(p => p.IdV == idV);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener item del portafolio {IdV} desde Excel", idV);
            return null;
        }
    }

    #endregion

    #region Mapping Helpers

    private SaleRecordDto MapRowToSaleRecord(Dictionary<string, object?> row)
    {
        var productsCodes = GetString(row, "ProductosCodigos") ?? "";
        var productsQuantities = GetString(row, "ProductosCantidades") ?? "";
        var productsPrices = GetString(row, "ProductosPrecios") ?? "";

        var productos = DeserializeProducts(productsCodes, productsQuantities, productsPrices);

        return new SaleRecordDto
        {
            IdV = GetString(row, "IdV") ?? "",
            NumVenta = GetInt(row, "NumVenta") ?? 0,
            FechaVenta = GetDateTime(row, "FechaVenta") ?? DateTime.Today,
            NombreCliente = GetString(row, "NombreCliente") ?? "",
            Celular = GetString(row, "Celular") ?? "",
            Telefono = GetString(row, "Telefono"),
            Zona = GetString(row, "Zona") ?? "",
            FormaPago = GetString(row, "FormaPago") ?? "",
            DiaCobro = GetString(row, "DiaCobro") ?? "",
            FotoCliente = GetString(row, "FotoCliente"),
            FotoFachada = GetString(row, "FotoFachada"),
            FotoContrato = GetString(row, "FotoContrato"),
            ObservacionVenta = GetString(row, "ObservacionVenta"),
            Vendedor = GetString(row, "Vendedor") ?? "",
            Usuario = GetString(row, "Usuario") ?? "",
            Cobrador = GetString(row, "Cobrador") ?? "",
            Coordenadas = GetString(row, "Coordenadas") ?? "",
            UrlUbicacion = GetString(row, "UrlUbicacion"),
            FechaPrimerCobro = GetDateTime(row, "FechaPrimerCobro"),
            Estado = GetString(row, "Estado") ?? "",
            FechaActu = GetDateTime(row, "FechaActu") ?? DateTime.Now,
            Estado2 = GetString(row, "Estado2") ?? "",
            ComisionVendedorPct = GetDecimal(row, "ComisionVendedorPct") ?? 0,
            Cobrar = GetString(row, "Cobrar") ?? "",
            FotoAdd1 = GetString(row, "FotoAdd1"),
            FotoAdd2 = GetString(row, "FotoAdd2"),
            Coordenadas2 = GetString(row, "Coordenadas2"),
            ProductosCantidad = productos.Count,
            ImporteTotal = GetDecimal(row, "ImporteTotal") ?? 0,
            Productos = productos
        };
    }

    private CollectionRecordDto MapRowToCollectionRecord(Dictionary<string, object?> row)
    {
        return new CollectionRecordDto
        {
            IdCc = GetString(row, "IdCc") ?? "",
            IdV = GetString(row, "IdV") ?? "",
            NumVenta = GetInt(row, "NumVenta") ?? 0,
            NombreCliente = GetString(row, "NombreCliente") ?? "",
            ImporteCobro = GetDecimal(row, "ImporteCobro") ?? 0,
            FechaCobro = GetDateTime(row, "FechaCobro") ?? DateTime.Today,
            ObservacionCobro = GetString(row, "ObservacionCobro"),
            FechaCaptura = GetDateTime(row, "FechaCaptura") ?? DateTime.Now,
            EstadoCc = GetString(row, "EstadoCc") ?? "",
            Usuario = GetString(row, "Usuario") ?? "",
            Zona = GetString(row, "Zona") ?? "",
            DiaCobroPrevisto = GetString(row, "DiaCobroPrevisto") ?? "",
            DiaCobrado = GetString(row, "DiaCobrado") ?? "",
            CoordenadasCobro = GetString(row, "CoordenadasCobro"),
            ImporteAbonado = GetDecimal(row, "ImporteAbonado") ?? 0,
            ImporteRestante = GetDecimal(row, "ImporteRestante") ?? 0,
            ImporteTotal = GetDecimal(row, "ImporteTotal") ?? 0,
            Estatus = GetString(row, "EstadoCc") ?? ""
        };
    }

    private SaleRecordDto MapInputToRecord(SaleFormInputDto input, string idV, int numVenta, decimal totalAmount, int productsCount)
    {
        return new SaleRecordDto
        {
            IdV = idV,
            NumVenta = numVenta,
            FechaVenta = input.FechaVenta,
            NombreCliente = input.NombreCliente,
            Celular = input.Celular,
            Telefono = input.Telefono,
            Zona = input.Zona,
            FormaPago = input.FormaPago,
            DiaCobro = input.DiaCobro,
            FotoCliente = input.FotoCliente,
            FotoFachada = input.FotoFachada,
            FotoContrato = input.FotoContrato,
            ObservacionVenta = input.ObservacionVenta,
            Vendedor = input.Vendedor,
            Usuario = input.Usuario,
            Cobrador = input.Cobrador,
            Coordenadas = input.Coordenadas,
            UrlUbicacion = input.UrlUbicacion,
            FechaPrimerCobro = input.FechaPrimerCobro,
            Estado = input.Estado,
            FechaActu = DateTime.Now,
            Estado2 = input.Estado2,
            ComisionVendedorPct = input.ComisionVendedorPct,
            Cobrar = input.Cobrar,
            FotoAdd1 = input.FotoAdd1,
            FotoAdd2 = input.FotoAdd2,
            Coordenadas2 = input.Coordenadas2,
            ProductosCantidad = productsCount,
            ImporteTotal = totalAmount,
            Productos = input.Productos
        };
    }

    private CollectionRecordDto MapCollectionInputToRecord(CollectionFormInputDto input, string idCc, SaleRecordDto sale)
    {
        return new CollectionRecordDto
        {
            IdCc = idCc,
            IdV = input.IdV,
            NumVenta = sale.NumVenta,
            NombreCliente = sale.NombreCliente,
            ImporteCobro = input.ImporteCobro,
            FechaCobro = input.FechaCobro,
            ObservacionCobro = input.ObservacionCobro,
            FechaCaptura = DateTime.Now,
            EstadoCc = "PARCIAL",
            Usuario = input.Usuario,
            Zona = sale.Zona,
            DiaCobroPrevisto = sale.DiaCobro,
            DiaCobrado = input.FechaCobro.ToString("dddd", new CultureInfo("es-ES")),
            CoordenadasCobro = input.CoordenadasCobro,
            ImporteAbonado = input.ImporteCobro,
            ImporteRestante = sale.ImporteTotal - input.ImporteCobro,
            ImporteTotal = sale.ImporteTotal,
            Estatus = "PARCIAL"
        };
    }

    private (string codes, string quantities, string prices, decimal total, int count) SerializeProducts(List<SaleProductLineDto> productos)
    {
        if (productos == null || productos.Count == 0)
        {
            return ("", "", "", 0, 0);
        }

        var codes = string.Join("|", productos.Select(p => p.ProductCode));
        var quantities = string.Join("|", productos.Select(p => p.Quantity));
        var prices = string.Join("|", productos.Select(p => (p.UnitPrice ?? 0).ToString("F2", CultureInfo.InvariantCulture)));
        var total = productos.Sum(p => p.Quantity * (p.UnitPrice ?? 0));
        var count = productos.Count;

        return (codes, quantities, prices, total, count);
    }

    private List<SaleProductLineDto> DeserializeProducts(string codes, string quantities, string prices)
    {
        if (string.IsNullOrWhiteSpace(codes))
        {
            return [];
        }

        var codeArray = codes.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var quantityArray = quantities.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var priceArray = prices.Split('|', StringSplitOptions.RemoveEmptyEntries);

        var productos = new List<SaleProductLineDto>();
        for (int i = 0; i < codeArray.Length; i++)
        {
            productos.Add(new SaleProductLineDto
            {
                ProductCode = codeArray[i],
                Quantity = int.TryParse(quantityArray.ElementAtOrDefault(i), out var qty) ? qty : 1,
                UnitPrice = decimal.TryParse(priceArray.ElementAtOrDefault(i), NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : null
            });
        }

        return productos;
    }

    private string? GetString(Dictionary<string, object?> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private int? GetInt(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is int intValue)
            return intValue;

        return int.TryParse(value.ToString(), out var result) ? result : null;
    }

    private decimal? GetDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is decimal decValue)
            return decValue;

        if (value is double dblValue)
            return (decimal)dblValue;

        return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private DateTime? GetDateTime(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is DateTime dtValue)
            return dtValue;

        return DateTime.TryParse(value.ToString(), out var result) ? result : null;
    }

    #endregion
}
