using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Collections;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Models.Shared;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Catalogs;
using SalesCobrosGeo.Web.Services.Sales;
using System.Globalization;
using System.Text;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.CollectionsAccess)]
public sealed class CobrosController : BaseController
{
    private readonly ISalesRepository _repository;
    private readonly IUserSessionTracker _sessionTracker;
    private readonly IApplicationUserService _userService;

    public CobrosController(
        ISalesRepository repository, 
        IUserSessionTracker sessionTracker, 
        IApplicationUserService userService,
        ICatalogViewService catalogViewService)
        : base(catalogViewService)
    {
        _repository = repository;
        _sessionTracker = sessionTracker;
        _userService = userService;
    }

    public IActionResult Index(string? profile = null, DateTime? from = null, DateTime? to = null, string? day = null, string? zone = null)
    {
        // If filters are provided from Dashboard, redirect to appropriate view with filters
        if (from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(day) || !string.IsNullOrWhiteSpace(zone))
        {
            var filterContext = BuildCollectionFilterContext(from, to, day, zone);
            return RedirectToCollectorQueueWithFilters(profile, filterContext);
        }

        // Default behavior: redirect based on role
        return IsSupervisor()
            ? RedirectToAction(nameof(SupervisorDashboard), new { profile })
            : RedirectToAction(nameof(CollectorQueue), new { profile = ResolveCollectorProfile(profile), day = GetTodayKey(), filter = "all", groupBy = "status" });
    }

    [HttpGet]
    public IActionResult CollectorHome(string? profile = null)
    {
        var activeProfile = ResolveCollectorProfile(profile);
        var clients = BuildCollectorClients(activeProfile);
        var todayClients = ApplyCollectorFilter(clients, "today");
        if (todayClients.Count == 0)
        {
            todayClients = clients.Take(6).ToList();
        }

        var model = new CollectorHomeViewModel
        {
            Profile = activeProfile,
            DisplayName = User.GetDisplayName(),
            RouteLabel = string.IsNullOrWhiteSpace(activeProfile) ? "Ruta asignada" : activeProfile,
            HistorySummaryCards = BuildHistorySummaryCards(activeProfile, GetScopedCollections(activeProfile), "pending"),
            Cards = BuildCollectorOperationalCards(clients),
            TodayClients = todayClients
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult CollectionHistory(string? profile = null, string outcome = "paid")
    {
        var activeProfile = ResolveCollectorProfile(profile);
        var history = GetScopedCollections(activeProfile);
        var filtered = FilterHistory(history, outcome);
        var model = new CollectionHistoryViewModel
        {
            Profile = activeProfile,
            Outcome = NormalizeHistoryOutcome(outcome),
            SummaryCards = BuildHistorySummaryCards(activeProfile, history, NormalizeHistoryOutcome(outcome)),
            Records = filtered
        };

        ViewData["MobileContextBar"] = new MobileContextBarModel(
            "Cobros",
            "Historial operativo",
            Url.Action(nameof(Index), new { profile = activeProfile }));
        return View(model);
    }

    [HttpGet]
    public IActionResult CollectorQueue(string? profile = null, string groupBy = "day", string filter = "today", string? day = null, string? status = null, string? zone = null)
    {
        var activeProfile = ResolveCollectorProfile(profile);
        var baseClients = BuildCollectorClients(activeProfile);
        var selectedDay = ResolveSelectedDay(baseClients, day);
        var dayScopedClients = ApplyCollectorDay(baseClients, selectedDay);
        var selectedStatus = NormalizeStatusBucket(status);
        var statusScopedClients = string.IsNullOrWhiteSpace(selectedStatus)
            ? dayScopedClients
            : dayScopedClients.Where(x => MatchesStatusBucket(x, selectedStatus)).ToList();
        var selectedZone = string.IsNullOrWhiteSpace(zone) ? string.Empty : zone.Trim();
        var zoneScopedClients = string.IsNullOrWhiteSpace(selectedZone)
            ? statusScopedClients
            : statusScopedClients.Where(x => string.Equals(x.Zone, selectedZone, StringComparison.OrdinalIgnoreCase)).ToList();
        var filteredClients = ApplyCollectorFilter(zoneScopedClients, filter);
        var mobileStatusGroups = BuildMobileStatusGroups(dayScopedClients);
        var mobileZoneGroups = string.IsNullOrWhiteSpace(selectedStatus)
            ? []
            : mobileStatusGroups.FirstOrDefault(x => x.Key == selectedStatus)?.Zones ?? [];
        var scopedHistory = GetScopedCollections(activeProfile);

        var model = new CollectorQueueViewModel
        {
            Profile = activeProfile,
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.ToLowerInvariant(),
            Filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant(),
            SelectedDay = selectedDay,
            SelectedStatus = selectedStatus,
            SelectedZone = selectedZone,
            SearchPlaceholder = "Buscar por nombre, direccion, telefono o folio",
            HistorySummaryCards = BuildHistorySummaryCards(activeProfile, scopedHistory, "pending"),
            QuickFilters = BuildQuickFilters(baseClients, filter),
            DayTabs = BuildCollectorDayTabs(baseClients, selectedDay),
            MobileStatusGroups = mobileStatusGroups,
            MobileZoneGroups = mobileZoneGroups,
            MobileZoneClients = zoneScopedClients,
            Groups = BuildCollectorGroups(filteredClients, groupBy)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult CollectorRoute(string? profile = null)
    {
        var activeProfile = ResolveCollectorProfile(profile);
        var clients = OptimizeRoute(BuildCollectorClients(activeProfile));
        for (var i = 0; i < clients.Count; i++)
        {
            clients[i].OrderIndex = i + 1;
        }

        var geoClients = clients.Where(x => TryParseCoordinates(x.Coordinates, out _, out _)).ToArray();
        var model = new CollectorRouteViewModel
        {
            Profile = activeProfile,
            ZoneLabel = clients.FirstOrDefault()?.Zone ?? "Ruta sugerida",
            RouteUrl = BuildRouteUrl(geoClients),
            GeoPoints = geoClients.Length,
            TotalStops = clients.Count,
            Clients = clients
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult SupervisorDashboard(string? profile = null, string groupBy = "collector")
    {
        if (!IsSupervisor())
        {
            return RedirectToAction(nameof(CollectorHome), new { profile = ResolveCollectorProfile(profile) });
        }

        var portfolio = GetScopedPortfolio(profile);
        var collections = _repository.GetCollections(profile);
        var clients = BuildCollectorClients(profile);

        var model = new SupervisorCollectionsViewModel
        {
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "collector" : groupBy.ToLowerInvariant(),
            SelectedCollector = profile,
            Metrics =
            [
                new SupervisorMetricCard { Title = "Cuentas activas", Value = portfolio.Count.ToString("0"), Subtitle = "Asignadas y con saldo pendiente", Tone = "neutral" },
                new SupervisorMetricCard { Title = "Cuentas vencidas", Value = portfolio.Count(x => NormalizeKey(x.Estatus) == "ATRASADO").ToString("0"), Subtitle = "Requieren accion inmediata", Tone = "danger" },
                new SupervisorMetricCard { Title = "Recuperado", Value = collections.Sum(x => x.ImporteCobro).ToString("0.00"), Subtitle = "Cobros registrados", Tone = "success" },
                new SupervisorMetricCard { Title = "Pendiente", Value = portfolio.Sum(x => x.ImporteRestante).ToString("0.00"), Subtitle = "Saldo vivo de la cartera", Tone = "warning" },
                new SupervisorMetricCard { Title = "Promesas", Value = clients.Count(x => x.HasPromise).ToString("0"), Subtitle = "Casos con seguimiento comprometido", Tone = "info" },
                new SupervisorMetricCard { Title = "Sin visita", Value = clients.Count(x => !x.WasVisited).ToString("0"), Subtitle = "Clientes aun sin gestion", Tone = "neutral" }
            ],
            Collectors = BuildSupervisorCollectorMonitor(portfolio, collections),
            Groups = BuildCollectorGroups(clients, groupBy)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult SupervisorMonitor(string? profile = null, string groupBy = "zone")
    {
        if (!IsSupervisor())
        {
            return RedirectToAction(nameof(CollectorHome), new { profile = ResolveCollectorProfile(profile) });
        }

        var portfolio = GetScopedPortfolio(profile);
        var collections = _repository.GetCollections(profile);
        var clients = BuildCollectorClients(profile);

        var model = new SupervisorCollectionsViewModel
        {
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "zone" : groupBy.ToLowerInvariant(),
            SelectedCollector = profile,
            Metrics =
            [
                new SupervisorMetricCard { Title = "Visitas realizadas", Value = clients.Count(x => x.WasVisited).ToString("0"), Subtitle = "Clientes con gestion previa", Tone = "success" },
                new SupervisorMetricCard { Title = "Pendientes", Value = clients.Count(x => !x.WasVisited).ToString("0"), Subtitle = "Cuentas aun no abordadas", Tone = "warning" },
                new SupervisorMetricCard { Title = "Promesas vencidas", Value = clients.Count(x => x.HasPromise && NormalizeKey(x.Status) == "ATRASADO").ToString("0"), Subtitle = "Seguir y validar", Tone = "danger" },
                new SupervisorMetricCard { Title = "Cobros capturados", Value = collections.Count.ToString("0"), Subtitle = "Movimientos acumulados", Tone = "info" }
            ],
            Collectors = BuildSupervisorCollectorMonitor(portfolio, collections),
            Groups = BuildCollectorGroups(clients, groupBy)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string id, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        var item = GetScopedPortfolioItem(id, profile);
        if (item is null)
        {
            return NotFound();
        }

        var model = new CollectionRegisterViewModel
        {
            IsEdit = false,
            PortfolioItem = item,
            Sale = _repository.GetById(id),
            Input = new CollectionFormInput
            {
                IdV = item.IdV,
                FechaCobro = DateTime.Today,
                Usuario = string.IsNullOrWhiteSpace(profile) ? item.Cobrador : profile,
                ImporteCobro = item.ImporteRestante > 0 ? item.ImporteRestante : 0,
                CoordenadasCobro = item.Coordenadas,
                ActionStatus = ResolveActionStatus(item.Estatus)
            },
            Historial = _repository.GetCollections(idV: id),
            CollectorProfiles = _repository.GetCollectorProfiles(),
            ReturnProfile = profile,
            ReturnDay = day,
            ReturnStatus = status,
            ReturnZone = zone
        };

        ViewData["CollectionsRoleView"] = IsSupervisor() ? "supervisor" : "collector";
        return View(model);
    }

    [HttpGet]
    public IActionResult EditCollection(string idCc, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        var record = _repository.GetCollectionById(idCc);
        if (record is null)
        {
            return NotFound();
        }

        var item = GetScopedPortfolioItem(record.IdV, profile);
        if (item is null)
        {
            return NotFound();
        }

        var model = new CollectionRegisterViewModel
        {
            IsEdit = true,
            PortfolioItem = item,
            Sale = _repository.GetById(record.IdV),
            Input = new CollectionFormInput
            {
                IdCc = record.IdCc,
                IdV = record.IdV,
                ImporteCobro = record.ImporteCobro,
                FechaCobro = record.FechaCobro,
                ObservacionCobro = record.ObservacionCobro,
                Usuario = record.Usuario,
                CoordenadasCobro = record.CoordenadasCobro,
                ActionStatus = ResolveActionStatus(record.Estatus)
            },
            Historial = _repository.GetCollections(idV: record.IdV),
            CollectorProfiles = _repository.GetCollectorProfiles(),
            ReturnProfile = profile,
            ReturnDay = day,
            ReturnStatus = status,
            ReturnZone = zone
        };

        ViewData["CollectionsRoleView"] = IsSupervisor() ? "supervisor" : "collector";
        return View("Register", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(CollectionFormInput input, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.IdCc))
            {
                _repository.RegisterCollection(input);
            }
            else
            {
                _repository.UpdateCollection(input.IdCc, input);
            }
            _sessionTracker.UpdateCoordinates(User.Identity?.Name ?? string.Empty, input.CoordenadasCobro, "Cobro registrado");
            TempData["CobroMessage"] = "Cobro registrado correctamente.";
            return IsSupervisor()
                ? RedirectToAction(nameof(SupervisorMonitor), new { profile, groupBy = "zone" })
                : RedirectToAction(nameof(CollectorQueue), new { profile = ResolveCollectorProfile(profile), day, status, zone, groupBy = "status", filter = "all" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = new CollectionRegisterViewModel
            {
                IsEdit = !string.IsNullOrWhiteSpace(input.IdCc),
                PortfolioItem = GetScopedPortfolioItem(input.IdV, profile ?? input.Usuario),
                Sale = _repository.GetById(input.IdV),
                Input = input,
                Historial = _repository.GetCollections(idV: input.IdV),
                CollectorProfiles = _repository.GetCollectorProfiles(),
                ReturnProfile = profile,
                ReturnDay = day,
                ReturnStatus = status,
                ReturnZone = zone
            };

            ViewData["CollectionsRoleView"] = IsSupervisor() ? "supervisor" : "collector";
            return View(model);
        }
    }

    private bool IsSupervisor() => User.IsInRole(AppRoles.Full) || User.HasPermission(AppPermissions.AdministrationView);

    private string ResolveCollectorProfile(string? profile)
    {
        if (!string.IsNullOrWhiteSpace(profile))
        {
            return profile;
        }

        if (!IsSupervisor())
        {
            return User.Identity?.Name ?? string.Empty;
        }

        return string.Empty;
    }

    private List<CollectorPortfolioItem> GetScopedPortfolio(string? profile, bool includeLiquidated = false)
    {
        if (IsSupervisor())
        {
            var supervisorPortfolio = _repository.GetCollectorPortfolio(profile).ToList();
            return includeLiquidated
                ? supervisorPortfolio
                : supervisorPortfolio.Where(x => x.ImporteRestante > 0).ToList();
        }

        var allPortfolio = _repository.GetCollectorPortfolio(null).ToList();
        var matchedProfile = ResolveCollectorAssignmentProfile(allPortfolio, profile);
        IEnumerable<CollectorPortfolioItem> scopedPortfolio = allPortfolio;

        if (!string.IsNullOrWhiteSpace(matchedProfile))
        {
            scopedPortfolio = scopedPortfolio.Where(x => string.Equals(x.Cobrador, matchedProfile, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var userZone = GetCurrentUserZone();
            if (!string.IsNullOrWhiteSpace(userZone) && !string.Equals(userZone, "Global", StringComparison.OrdinalIgnoreCase))
            {
                scopedPortfolio = scopedPortfolio.Where(x => string.Equals(x.Zona, userZone, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                scopedPortfolio = Enumerable.Empty<CollectorPortfolioItem>();
            }
        }

        var result = scopedPortfolio.ToList();
        return includeLiquidated
            ? result
            : result.Where(x => x.ImporteRestante > 0).ToList();
    }

    private CollectorPortfolioItem? GetScopedPortfolioItem(string id, string? profile)
    {
        return GetScopedPortfolio(profile, includeLiquidated: true)
            .FirstOrDefault(x => string.Equals(x.IdV, id, StringComparison.OrdinalIgnoreCase));
    }

    private string? ResolveCollectorAssignmentProfile(IEnumerable<CollectorPortfolioItem> portfolio, string? requestedProfile)
    {
        var candidates = new[]
        {
            requestedProfile,
            User.Identity?.Name,
            User.GetDisplayName()
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        foreach (var candidate in candidates)
        {
            if (portfolio.Any(x => string.Equals(x.Cobrador, candidate, StringComparison.OrdinalIgnoreCase)))
            {
                return candidate;
            }
        }

        return null;
    }

    private string GetCurrentUserZone()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
        {
            return string.Empty;
        }

        return _userService.GetUser(username)?.Zone?.Trim() ?? string.Empty;
    }

    private IReadOnlyList<CollectionRecord> GetScopedCollections(string? profile)
    {
        var ids = GetScopedPortfolio(profile, includeLiquidated: true)
            .Select(x => x.IdV)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _repository.GetCollections()
            .Where(x => ids.Contains(x.IdV))
            .OrderByDescending(x => x.FechaCobro)
            .ThenByDescending(x => x.FechaCaptura)
            .ToArray();
    }

    private static string NormalizeHistoryOutcome(string? outcome)
    {
        return (outcome ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "nopay" => "nopay",
            "paid" => "paid",
            _ => "pending"
        };
    }

    private IReadOnlyList<CollectionHistorySummaryCard> BuildHistorySummaryCards(string profile, IReadOnlyList<CollectionRecord> history, string active)
    {
        var pendingCount = GetScopedPortfolio(profile).Count;
        var paidCount = history.Count(x => x.ImporteCobro > 0);
        var noPayCount = history.Count(x => x.ImporteCobro <= 0 || string.Equals(x.EstadoCc, "NO PAGO", StringComparison.OrdinalIgnoreCase));

        return
        [
            new CollectionHistorySummaryCard { Code = "pending", Title = "Pend", Subtitle = "Por cubrir", Count = pendingCount, IsActive = active == "pending" },
            new CollectionHistorySummaryCard { Code = "paid", Title = "Complet. abon", Subtitle = "Con pago", Count = paidCount, IsActive = active == "paid" },
            new CollectionHistorySummaryCard { Code = "nopay", Title = "Complet. 0", Subtitle = "Sin pago", Count = noPayCount, IsActive = active == "nopay" }
        ];
    }

    private IReadOnlyList<CollectionRecord> FilterHistory(IReadOnlyList<CollectionRecord> history, string outcome)
    {
        return NormalizeHistoryOutcome(outcome) switch
        {
            "paid" => history.Where(x => x.ImporteCobro > 0).ToArray(),
            "nopay" => history.Where(x => x.ImporteCobro <= 0 || string.Equals(x.EstadoCc, "NO PAGO", StringComparison.OrdinalIgnoreCase)).ToArray(),
            _ => []
        };
    }

    private static string ResolveActionStatus(string? status)
    {
        return NormalizeKey(status) switch
        {
            "ALCORRIENTE" => "AL CORRIENTE",
            "PROXIMASEMANA" => "PROXIMA SEMANA",
            "PASARMANANA" => "PASAR MAÑANA",
            "ATRASADO" => "ATRASADO",
            "PASARMASTARDE" => "PASAR MAS TARDE",
            "CANCELADO" => "CANCELADO",
            "LIQUIDADO" => "LIQUIDADO",
            _ => "AL CORRIENTE"
        };
    }

    private List<CollectorClientListItem> BuildCollectorClients(string? profile)
    {
        var portfolio = GetScopedPortfolio(profile);
        var sales = _repository.GetAll().ToDictionary(x => x.IdV, StringComparer.OrdinalIgnoreCase);
        var allowedIds = portfolio.Select(x => x.IdV).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var collections = _repository.GetCollections(idV: null)
            .Where(x => allowedIds.Contains(x.IdV))
            .GroupBy(x => x.IdV, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.FechaCobro).ToList(), StringComparer.OrdinalIgnoreCase);

        var list = new List<CollectorClientListItem>(portfolio.Count);
        foreach (var item in portfolio)
        {
            sales.TryGetValue(item.IdV, out var sale);
            collections.TryGetValue(item.IdV, out var history);
            var last = history?.FirstOrDefault();
            var note = last?.ObservacionCobro ?? sale?.ObservacionVenta ?? string.Empty;
            var hasPromise = ContainsKeyword(note, "PROMESA");
            var promiseOverdue = hasPromise && (ContainsKeyword(note, "VENCID") || ContainsKeyword(note, "INCUMPL") || NormalizeKey(item.Estatus) == "ATRASADO");
            var notLocated = ContainsKeyword(note, "NO LOCALIZ") || ContainsKeyword(note, "NO ENCONTR");
            var refused = ContainsKeyword(note, "SE NEGO") || ContainsKeyword(note, "SE NEGÓ") || ContainsKeyword(note, "NEGATIVA") || ContainsKeyword(note, "SE NIEGA");
            var wasVisited = history is { Count: > 0 } || !string.IsNullOrWhiteSpace(note);

            list.Add(new CollectorClientListItem
            {
                IdV = item.IdV,
                NumVenta = item.NumVenta,
                Name = item.NombreCliente,
                Zone = item.Zona,
                Route = item.DiaCobroPrevisto,
                Status = item.Estatus,
                Priority = ComputePriority(item.Estatus, hasPromise, promiseOverdue, notLocated, refused),
                NextAction = ComputeNextAction(item.Estatus, hasPromise, promiseOverdue, notLocated, refused, wasVisited),
                ReferenceText = string.IsNullOrWhiteSpace(sale?.ObservacionVenta) ? $"Zona {item.Zona}" : sale.ObservacionVenta!,
                Phone = item.Celular,
                Coordinates = item.Coordenadas,
                Thumbnail = string.IsNullOrWhiteSpace(item.FotoCliente) ? item.FotoFachada : item.FotoCliente,
                LastPaymentDate = last?.FechaCobro,
                LastNote = string.IsNullOrWhiteSpace(note) ? "Sin gestion reciente" : note,
                SaleState = item.EstadoVenta,
                HasPromise = hasPromise,
                HasPromiseOverdue = promiseOverdue,
                IsNotLocated = notLocated,
                IsRefused = refused,
                WasVisited = wasVisited
            });
        }

        return list;
    }

    private IReadOnlyList<CollectorQuickFilter> BuildQuickFilters(IReadOnlyList<CollectorClientListItem> clients, string activeFilter)
    {
        return
        [
            new CollectorQuickFilter { Code = "today", Label = "Hoy", Count = CountByFilter(clients, "today"), IsActive = activeFilter.Equals("today", StringComparison.OrdinalIgnoreCase) },
            new CollectorQuickFilter { Code = "overdue", Label = "Atrasados", Count = CountByFilter(clients, "overdue"), IsActive = activeFilter.Equals("overdue", StringComparison.OrdinalIgnoreCase) },
            new CollectorQuickFilter { Code = "promise", Label = "Promesa", Count = CountByFilter(clients, "promise"), IsActive = activeFilter.Equals("promise", StringComparison.OrdinalIgnoreCase) },
            new CollectorQuickFilter { Code = "unvisited", Label = "No visitados", Count = CountByFilter(clients, "unvisited"), IsActive = activeFilter.Equals("unvisited", StringComparison.OrdinalIgnoreCase) },
            new CollectorQuickFilter { Code = "notlocated", Label = "No localizado", Count = CountByFilter(clients, "notlocated"), IsActive = activeFilter.Equals("notlocated", StringComparison.OrdinalIgnoreCase) },
            new CollectorQuickFilter { Code = "followup", Label = "Seguimiento", Count = CountByFilter(clients, "followup"), IsActive = activeFilter.Equals("followup", StringComparison.OrdinalIgnoreCase) }
        ];
    }

    private IReadOnlyList<CollectorOperationalCard> BuildCollectorOperationalCards(IReadOnlyList<CollectorClientListItem> clients)
    {
        return _repository.GetMaintenanceCatalog("estatus-cobro-grupos")
            .Where(x => x.IsActive)
            .Select(x => new CollectorOperationalCard
            {
                Title = x.Name,
                Subtitle = BuildCollectorOperationalSubtitle(x.Code),
                Count = clients.Count(client => MatchesStatusBucket(client, NormalizeStatusBucket(x.Code))),
                Accent = ResolveStatusTone(x.Code) switch
                {
                    "warning" => "warning",
                    "info" => "info",
                    "danger" => "danger",
                    _ => "primary"
                },
                StatusKey = NormalizeStatusBucket(x.Code),
                TargetAction = nameof(CollectorQueue)
            })
            .ToArray();
    }

    private static string BuildCollectorOperationalSubtitle(string? code)
    {
        return NormalizeStatusBucket(code) switch
        {
            "promise" => "Seguimiento y confirmacion",
            "followup" => "Clientes con nueva visita",
            "overdue" => "Gestion prioritaria del dia",
            "recovery" => "Cobros parciales por recuperar",
            "current" => "Cuentas al corriente para seguimiento",
            "liquidated" => "Cuentas liquidadas del dia",
            "cancelled" => "Cuentas fuera de operacion",
            _ => "Visitas para arrancar la jornada"
        };
    }

    private string ResolveSelectedDay(IReadOnlyList<CollectorClientListItem> clients, string? day)
    {
        var normalized = NormalizeDayKey(day);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var today = GetTodayKey();
        if (clients.Any(x => NormalizeDayKey(x.Route) == today))
        {
            return today;
        }

        return clients
            .Select(x => NormalizeDayKey(x.Route))
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? "LUNES";
    }

    private List<CollectorClientListItem> ApplyCollectorDay(IEnumerable<CollectorClientListItem> clients, string selectedDay)
    {
        var normalized = NormalizeDayKey(selectedDay);
        return clients
            .Where(x => NormalizeDayKey(x.Route) == normalized)
            .OrderByDescending(x => PriorityRank(x.Priority))
            .ThenBy(x => x.Name)
            .ToList();
    }

    private IReadOnlyList<CollectorDayTab> BuildCollectorDayTabs(IReadOnlyList<CollectorClientListItem> clients, string selectedDay)
    {
        var days = _repository.GetMaintenanceCatalog("dias-cobro")
            .Where(x => x.IsActive)
            .Select(x => new
            {
                Code = NormalizeDayKey(x.Code),
                Label = string.IsNullOrWhiteSpace(x.Name) ? x.Code : x.Name.Trim(),
                Sort = ResolveDaySort(x.Code)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Label)
            .ToArray();

        if (days.Length == 0)
        {
            days = new[]
            {
                new { Code = "LUNES", Label = "Lunes", Sort = 1 },
                new { Code = "MARTES", Label = "Martes", Sort = 2 },
                new { Code = "MIERCOLES", Label = "Miercoles", Sort = 3 },
                new { Code = "JUEVES", Label = "Jueves", Sort = 4 },
                new { Code = "VIERNES", Label = "Viernes", Sort = 5 }
            };
        }

        return days
            .Select(day => new CollectorDayTab
            {
                Code = day.Code,
                ShortLabel = ResolveDayShortLabel(day.Label),
                Label = day.Label,
                Count = clients.Count(x => NormalizeDayKey(x.Route) == day.Code),
                IsActive = NormalizeDayKey(selectedDay) == day.Code
            })
            .ToArray();
    }

    private static int ResolveDaySort(string? code)
    {
        return NormalizeDayKey(code) switch
        {
            "LUNES" => 1,
            "MARTES" => 2,
            "MIERCOLES" => 3,
            "JUEVES" => 4,
            "VIERNES" => 5,
            "SABADO" => 6,
            "DOMINGO" => 7,
            _ => 99
        };
    }

    private static string ResolveDayShortLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "?";
        }

        var normalized = RemoveDiacritics(label.Trim()).ToUpperInvariant();
        return normalized switch
        {
            var x when x.StartsWith("LUN") => "L",
            var x when x.StartsWith("MAR") => "M",
            var x when x.StartsWith("MIE") => "M",
            var x when x.StartsWith("JUE") => "J",
            var x when x.StartsWith("VIE") => "V",
            var x when x.StartsWith("SAB") => "S",
            var x when x.StartsWith("DOM") => "D",
            _ => normalized[..1]
        };
    }

    private IReadOnlyList<CollectorMobileStatusGroup> BuildMobileStatusGroups(IReadOnlyList<CollectorClientListItem> clients)
    {
        var remaining = clients.ToList();
        var definitions = _repository.GetMaintenanceCatalog("estatus-cobro-grupos")
            .Where(x => x.IsActive)
            .Select(x => new
            {
                Key = NormalizeStatusBucket(x.Code),
                Title = x.Name,
                Tone = ResolveStatusTone(x.Code),
                Icon = NormalizeStatusBucket(x.Code)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToArray();

        var result = new List<CollectorMobileStatusGroup>(definitions.Length);
        foreach (var definition in definitions)
        {
            var matched = remaining.Where(x => MatchesStatusBucket(x, definition.Key)).ToList();
            if (matched.Count > 0)
            {
                remaining.RemoveAll(x => matched.Any(y => y.IdV == x.IdV));
            }

            var zoneGroups = matched
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Zone) ? "Sin zona" : x.Zone)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .Select(g => new CollectorMobileZoneGroup
                {
                    Zone = g.Key,
                    Accounts = g.Count(),
                    Clients = g
                        .OrderByDescending(x => PriorityRank(x.Priority))
                        .ThenBy(x => x.Name)
                        .ToArray()
                })
                .ToArray();

            result.Add(new CollectorMobileStatusGroup
            {
                Key = definition.Key,
                Title = definition.Title,
                Tone = definition.Tone,
                Icon = definition.Icon,
                Count = matched.Count,
                IsOpen = result.Count == 0 && matched.Count > 0,
                Zones = zoneGroups
            });
        }

        return result;
    }

    private static string ResolveStatusTone(string? code)
    {
        return NormalizeStatusBucket(code) switch
        {
            "promise" => "warning",
            "followup" => "info",
            "overdue" => "danger",
            "recovery" => "brand",
            "current" => "success",
            "liquidated" or "cancelled" => "muted",
            _ => "neutral"
        };
    }

    private static string NormalizeStatusBucket(string? status)
    {
        return NormalizeKey(status) switch
        {
            "PENDING" or "PENDIENTESHOY" => "pending",
            "PROMISE" or "PROMESASPAGOHOY" => "promise",
            "FOLLOWUP" or "REAGENDADOS" => "followup",
            "OVERDUE" or "ATRASADOS" => "overdue",
            "RECOVERY" or "RECUPERACION" => "recovery",
            "CURRENT" or "ALCORRIENTE" => "current",
            "LIQUIDATED" or "LIQUIDADOS" => "liquidated",
            "CANCELLED" or "CANCELADOS" => "cancelled",
            _ => string.Empty
        };
    }

    private static bool MatchesStatusBucket(CollectorClientListItem item, string bucket)
    {
        var normalizedStatus = NormalizeKey(item.Status);
        var normalizedSaleState = NormalizeKey(item.SaleState);
        return bucket switch
        {
            "cancelled" => normalizedStatus == "CANCELADO" || normalizedSaleState == "CANCELADO",
            "liquidated" => normalizedStatus == "LIQUIDADO",
            "promise" => item.HasPromise,
            "followup" => NormalizeKey(item.NextAction) == "SEGUIMIENTO" || ContainsKeyword(item.LastNote, "REAGEND") || item.IsNotLocated || item.IsRefused,
            "overdue" => normalizedStatus == "ATRASADO",
            "recovery" => normalizedStatus == "PARCIAL",
            "current" => normalizedStatus == "AL CORRIENTE",
            "pending" => normalizedStatus == "POR INICIAR" || !item.WasVisited,
            _ => false
        };
    }

    private List<CollectorClientListItem> ApplyCollectorFilter(List<CollectorClientListItem> clients, string filter)
    {
        var normalized = filter?.Trim().ToLowerInvariant() ?? "today";
        return normalized switch
        {
            "all" => clients.ToList(),
            "overdue" => clients.Where(x => NormalizeKey(x.Status) == "ATRASADO").ToList(),
            "promise" => clients.Where(x => x.HasPromise).ToList(),
            "unvisited" => clients.Where(x => !x.WasVisited).ToList(),
            "notlocated" => clients.Where(x => x.IsNotLocated).ToList(),
            "followup" => clients.Where(x => NormalizeKey(x.NextAction) == NormalizeKey("Seguimiento") || ContainsKeyword(x.LastNote, "REAGEND") || x.IsNotLocated || x.IsRefused).ToList(),
            _ => clients.Where(x => NormalizeKey(x.Route) == GetTodayKey() || NormalizeKey(x.Status) == "ATRASADO").ToList()
        };
    }

    private int CountByFilter(IReadOnlyList<CollectorClientListItem> clients, string filter) => ApplyCollectorFilter(clients.ToList(), filter).Count;

    private IReadOnlyList<CollectorQueueGroupViewModel> BuildCollectorGroups(IReadOnlyList<CollectorClientListItem> clients, string? groupBy)
    {
        var mode = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.ToLowerInvariant();
        IEnumerable<IGrouping<string, CollectorClientListItem>> groups = mode switch
        {
            "status" => clients.GroupBy(x => x.Status),
            "zone" => clients.GroupBy(x => x.Zone),
            "route" => clients.GroupBy(x => x.Zone),
            "collector" => clients.GroupBy(x => x.Zone),
            _ => clients.GroupBy(x => x.Route)
        };

        return groups
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .OrderByDescending(x => x.Count())
            .Select(g => new CollectorQueueGroupViewModel
            {
                Key = g.Key,
                Title = g.Key,
                Accounts = g.Count(),
                UrgentCount = g.Count(x => NormalizeKey(x.Priority) == "ALTA"),
                PromiseCount = g.Count(x => x.HasPromise),
                Priority = g.Any(x => NormalizeKey(x.Priority) == "ALTA") ? "Alta" : g.Any(x => NormalizeKey(x.Priority) == "MEDIA") ? "Media" : "Normal",
                SuggestedAction = g.Any(x => NormalizeKey(x.Status) == "ATRASADO") ? "Visitar urgente" : g.Any(x => x.HasPromise) ? "Confirmar promesas" : "Gestion operativa",
                Subtitle = $"{g.Count()} cuentas · {g.Count(x => NormalizeKey(x.Priority) == "ALTA")} visitas urgentes · {g.Count(x => x.HasPromise)} promesas",
                Clients = g.OrderByDescending(x => PriorityRank(x.Priority)).ThenBy(x => x.Name).ToArray()
            })
            .ToArray();
    }

    private IReadOnlyList<SupervisorCollectorMonitorItem> BuildSupervisorCollectorMonitor(IReadOnlyList<CollectorPortfolioItem> portfolio, IReadOnlyList<CollectionRecord> collections)
    {
        return portfolio
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Cobrador) ? "Sin asignar" : x.Cobrador)
            .OrderBy(x => x.Key)
            .Select(g =>
            {
                var collectorMoves = collections.Where(x => string.Equals(x.Usuario, g.Key, StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.FechaCobro).ToList();
                var lastMove = collectorMoves.FirstOrDefault();
                return new SupervisorCollectorMonitorItem
                {
                    Collector = g.Key,
                    Zone = g.Select(x => x.Zona).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().FirstOrDefault() ?? "Sin zona",
                    Status = g.Any(x => NormalizeKey(x.Estatus) == "ATRASADO") ? "Con alertas" : "Operando",
                    Accounts = g.Count(),
                    VisitsDone = collectorMoves.Count,
                    PendingVisits = g.Count(x => NormalizeKey(x.Estatus) == "POR INICIAR"),
                    Promises = collectorMoves.Count(x => ContainsKeyword(x.ObservacionCobro, "PROMESA")),
                    Overdue = g.Count(x => NormalizeKey(x.Estatus) == "ATRASADO"),
                    RecoveredAmount = collectorMoves.Sum(x => x.ImporteCobro),
                    LastActivity = lastMove is null ? "Sin actividad registrada" : lastMove.FechaCobro.ToString("dd/MM HH:mm"),
                    LastCoordinates = lastMove?.CoordenadasCobro ?? string.Empty
                };
            })
            .ToArray();
    }

    private List<CollectorClientListItem> OptimizeRoute(List<CollectorClientListItem> clients)
    {
        var grouped = clients
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Zone) ? "Sin zona" : x.Zone)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key);

        var ordered = new List<CollectorClientListItem>(clients.Count);
        foreach (var zoneGroup in grouped)
        {
            var withCoords = zoneGroup.Where(x => TryParseCoordinates(x.Coordinates, out _, out _)).ToList();
            var withoutCoords = zoneGroup.Where(x => !TryParseCoordinates(x.Coordinates, out _, out _))
                .OrderByDescending(x => PriorityRank(x.Priority))
                .ThenBy(x => x.Name)
                .ToList();

            if (withCoords.Count > 0)
            {
                var start = withCoords.OrderByDescending(x => PriorityRank(x.Priority)).ThenBy(x => x.Name).First();
                var route = new List<CollectorClientListItem> { start };
                withCoords.Remove(start);

                while (withCoords.Count > 0)
                {
                    var current = route[^1];
                    var next = withCoords
                        .OrderBy(x => DistanceBetween(current.Coordinates, x.Coordinates))
                        .ThenByDescending(x => PriorityRank(x.Priority))
                        .ThenBy(x => x.Name)
                        .First();

                    route.Add(next);
                    withCoords.Remove(next);
                }

                ordered.AddRange(route);
            }

            ordered.AddRange(withoutCoords);
        }

        return ordered;
    }

    private static string ComputePriority(string status, bool hasPromise, bool hasPromiseOverdue, bool notLocated, bool refused)
    {
        if (NormalizeKey(status) == "ATRASADO" || hasPromiseOverdue || notLocated || refused)
        {
            return "Alta";
        }

        if (hasPromise || NormalizeKey(status) == "POR INICIAR")
        {
            return "Media";
        }

        return "Normal";
    }

    private static string ComputeNextAction(string status, bool hasPromise, bool hasPromiseOverdue, bool notLocated, bool refused, bool wasVisited)
    {
        if (notLocated)
        {
            return "Revisar referencia";
        }

        if (refused)
        {
            return "Escalar seguimiento";
        }

        if (hasPromiseOverdue)
        {
            return "Recuperar promesa";
        }

        if (hasPromise)
        {
            return "Confirmar promesa";
        }

        if (NormalizeKey(status) == "ATRASADO")
        {
            return "Registrar visita";
        }

        if (!wasVisited)
        {
            return "Primera visita";
        }

        return "Seguimiento";
    }

    private static int PriorityRank(string priority) => NormalizeKey(priority) switch
    {
        "ALTA" => 3,
        "MEDIA" => 2,
        _ => 1
    };

    private static bool TryParseCoordinates(string? value, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lng);
    }

    private static double DistanceBetween(string? from, string? to)
    {
        if (!TryParseCoordinates(from, out var lat1, out var lng1) || !TryParseCoordinates(to, out var lat2, out var lng2))
        {
            return double.MaxValue;
        }

        const double earthRadiusKm = 6371.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180d;

    private static string BuildRouteUrl(IReadOnlyList<CollectorClientListItem> clients)
    {
        var points = clients
            .Where(x => TryParseCoordinates(x.Coordinates, out _, out _))
            .Select(x => x.Coordinates!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        if (points.Length < 2)
        {
            return string.Empty;
        }

        var origin = Uri.EscapeDataString(points[0]);
        var destination = Uri.EscapeDataString(points[^1]);
        var waypoints = points.Length > 2
            ? "&waypoints=" + string.Join("%7C", points.Skip(1).Take(points.Length - 2).Select(Uri.EscapeDataString))
            : string.Empty;

        return $"https://www.google.com/maps/dir/?api=1&origin={origin}&destination={destination}{waypoints}&travelmode=driving";
    }

    private static bool ContainsKeyword(string? source, string keyword) => NormalizeKey(source).Contains(NormalizeKey(keyword), StringComparison.OrdinalIgnoreCase);

    private static string GetTodayKey()
    {
        var today = DateTime.Today.DayOfWeek;
        return today switch
        {
            DayOfWeek.Monday => "LUNES",
            DayOfWeek.Tuesday => "MARTES",
            DayOfWeek.Wednesday => "MIERCOLES",
            DayOfWeek.Thursday => "JUEVES",
            DayOfWeek.Friday => "VIERNES",
            DayOfWeek.Saturday => "SABADO",
            _ => "DOMINGO"
        };
    }

    private static string NormalizeDayKey(string? value)
    {
        return NormalizeKey(value) switch
        {
            "L" or "LUN" or "LUNES" => "LUNES",
            "M" or "MAR" or "MARTES" => "MARTES",
            "MIE" or "MIERCOLES" => "MIERCOLES",
            "J" or "JUE" or "JUEVES" => "JUEVES",
            "V" or "VIE" or "VIERNES" => "VIERNES",
            "S" or "SAB" or "SABADO" => "SABADO",
            "D" or "DOM" or "DOMINGO" => "DOMINGO",
            _ => NormalizeKey(value)
        };
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    #region Collection Filtering - Dashboard Integration

    /// <summary>
    /// Builds a filter context from Dashboard parameters for collections
    /// </summary>
    private static CollectionFilterContext BuildCollectionFilterContext(DateTime? from, DateTime? to, string? day, string? zone)
    {
        var context = new CollectionFilterContext
        {
            HasFilters = from.HasValue || to.HasValue || !string.IsNullOrWhiteSpace(day) || !string.IsNullOrWhiteSpace(zone)
        };

        // Date range filter
        if (from.HasValue)
        {
            context.DateFrom = from.Value.Date;
        }

        if (to.HasValue)
        {
            context.DateTo = to.Value.Date;
        }

        // Single day filter (overrides date range if provided)
        if (!string.IsNullOrWhiteSpace(day) && DateTime.TryParse(day, out var dayDate))
        {
            context.FilteredDay = dayDate.Date;
            context.DayKey = dayDate.ToString("yyyy-MM-dd");
        }

        // Zone filter
        if (!string.IsNullOrWhiteSpace(zone))
        {
            context.Zone = zone.Trim();
        }

        return context;
    }

    /// <summary>
    /// Redirects to CollectorQueue with appropriate filters from Dashboard
    /// </summary>
    private IActionResult RedirectToCollectorQueueWithFilters(string? profile, CollectionFilterContext context)
    {
        var activeProfile = ResolveCollectorProfile(profile);

        // Build route parameters
        var routeParams = new Dictionary<string, object?>
        {
            ["profile"] = activeProfile,
            ["groupBy"] = "status",
            ["filter"] = "all"
        };

        // Add day filter if provided
        if (!string.IsNullOrWhiteSpace(context.DayKey))
        {
            routeParams["day"] = context.DayKey;
        }

        // Add zone filter if provided
        if (!string.IsNullOrWhiteSpace(context.Zone))
        {
            routeParams["zone"] = context.Zone;
        }

        return RedirectToAction(nameof(CollectorQueue), routeParams);
    }

    /// <summary>
    /// Filter context for collection data
    /// </summary>
    private sealed class CollectionFilterContext
    {
        public bool HasFilters { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? FilteredDay { get; set; }
        public string? DayKey { get; set; }
        public string? Zone { get; set; }
    }
}
    #endregion

