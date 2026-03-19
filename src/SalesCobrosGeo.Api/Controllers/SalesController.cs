using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Data;

namespace SalesCobrosGeo.Api.Controllers;

/// <summary>
/// API Controller para gestión de ventas y cobros
/// Fase 2: Unificación de datos transaccionales en Excel
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SalesController : ControllerBase
{
    private readonly ISalesStore _salesStore;
    private readonly ILogger<SalesController> _logger;

    public SalesController(ISalesStore salesStore, ILogger<SalesController> logger)
    {
        _salesStore = salesStore;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las ventas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSales()
    {
        try
        {
            var sales = await _salesStore.GetAllSalesAsync();
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las ventas");
            return StatusCode(500, new { message = "Error al obtener ventas" });
        }
    }

    /// <summary>
    /// Obtiene una venta por su ID
    /// </summary>
    [HttpGet("{idV}")]
    public async Task<IActionResult> GetSaleById(string idV)
    {
        try
        {
            var sale = await _salesStore.GetSaleByIdAsync(idV);
            if (sale == null)
            {
                return NotFound(new { message = $"Venta {idV} no encontrada" });
            }
            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener venta {IdV}", idV);
            return StatusCode(500, new { message = "Error al obtener venta" });
        }
    }

    /// <summary>
    /// Crea una nueva venta
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] SaleFormInputDto input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sale = await _salesStore.CreateSaleAsync(input);
            return CreatedAtAction(nameof(GetSaleById), new { idV = sale.IdV }, sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear venta");
            return StatusCode(500, new { message = "Error al crear venta" });
        }
    }

    /// <summary>
    /// Actualiza una venta existente
    /// </summary>
    [HttpPut("{idV}")]
    public async Task<IActionResult> UpdateSale(string idV, [FromBody] SaleFormInputDto input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sale = await _salesStore.UpdateSaleAsync(idV, input);
            return Ok(sale);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Venta {idV} no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar venta {IdV}", idV);
            return StatusCode(500, new { message = "Error al actualizar venta" });
        }
    }

    /// <summary>
    /// Obtiene todos los cobros
    /// </summary>
    [HttpGet("collections")]
    public async Task<IActionResult> GetAllCollections()
    {
        try
        {
            var collections = await _salesStore.GetAllCollectionsAsync();
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los cobros");
            return StatusCode(500, new { message = "Error al obtener cobros" });
        }
    }

    /// <summary>
    /// Obtiene los cobros de una venta específica
    /// </summary>
    [HttpGet("{idV}/collections")]
    public async Task<IActionResult> GetCollectionsBySale(string idV)
    {
        try
        {
            var collections = await _salesStore.GetCollectionsBySaleAsync(idV);
            return Ok(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobros de venta {IdV}", idV);
            return StatusCode(500, new { message = "Error al obtener cobros" });
        }
    }

    /// <summary>
    /// Obtiene un cobro por su ID
    /// </summary>
    [HttpGet("collections/{idCc}")]
    public async Task<IActionResult> GetCollectionById(string idCc)
    {
        try
        {
            var collection = await _salesStore.GetCollectionByIdAsync(idCc);
            if (collection == null)
            {
                return NotFound(new { message = $"Cobro {idCc} no encontrado" });
            }
            return Ok(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cobro {IdCc}", idCc);
            return StatusCode(500, new { message = "Error al obtener cobro" });
        }
    }

    /// <summary>
    /// Registra un nuevo cobro
    /// </summary>
    [HttpPost("collections")]
    public async Task<IActionResult> RegisterCollection([FromBody] CollectionFormInputDto input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var collection = await _salesStore.RegisterCollectionAsync(input);
            return CreatedAtAction(nameof(GetCollectionById), new { idCc = collection.IdCc }, collection);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cobro");
            return StatusCode(500, new { message = "Error al registrar cobro" });
        }
    }

    /// <summary>
    /// Actualiza un cobro existente
    /// </summary>
    [HttpPut("collections/{idCc}")]
    public async Task<IActionResult> UpdateCollection(string idCc, [FromBody] CollectionFormInputDto input)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var collection = await _salesStore.UpdateCollectionAsync(idCc, input);
            return Ok(collection);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cobro {IdCc}", idCc);
            return StatusCode(500, new { message = "Error al actualizar cobro" });
        }
    }

    /// <summary>
    /// Obtiene el portafolio de cobros del cobrador autenticado
    /// </summary>
    [HttpGet("portfolio")]
    public async Task<IActionResult> GetMyPortfolio()
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var portfolio = await _salesStore.GetCollectorPortfolioAsync(username);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener portafolio del usuario");
            return StatusCode(500, new { message = "Error al obtener portafolio" });
        }
    }

    /// <summary>
    /// Obtiene el portafolio de un cobrador específico (para supervisores)
    /// </summary>
    [HttpGet("portfolio/{collectorUsername}")]
    public async Task<IActionResult> GetCollectorPortfolio(string collectorUsername)
    {
        try
        {
            var portfolio = await _salesStore.GetCollectorPortfolioAsync(collectorUsername);
            return Ok(portfolio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener portafolio del cobrador {Collector}", collectorUsername);
            return StatusCode(500, new { message = "Error al obtener portafolio" });
        }
    }

    /// <summary>
    /// Obtiene un item específico del portafolio
    /// </summary>
    [HttpGet("portfolio/item/{idV}")]
    public async Task<IActionResult> GetPortfolioItem(string idV, [FromQuery] string? collector = null)
    {
        try
        {
            var item = await _salesStore.GetPortfolioItemAsync(idV, collector);
            if (item == null)
            {
                return NotFound(new { message = $"Item {idV} no encontrado en portafolio" });
            }
            return Ok(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener item {IdV} del portafolio", idV);
            return StatusCode(500, new { message = "Error al obtener item" });
        }
    }
}
