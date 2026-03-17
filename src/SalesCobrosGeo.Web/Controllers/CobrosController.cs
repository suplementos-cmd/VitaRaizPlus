using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesCobrosGeo.Web.Models.Collections;
using SalesCobrosGeo.Web.Models.Sales;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;
using System.Globalization;
using System.Text;

namespace SalesCobrosGeo.Web.Controllers;

[Authorize(Policy = AppPolicies.CollectionsAccess)]
public sealed class CobrosController : Controller
{
    private readonly ISalesRepository _repository;
    private readonly IUserSessionTracker _sessionTracker;
    private readonly IApplicationUserService _userService;

    public CobrosController(ISalesRepository repository, IUserSessionTracker sessionTracker, IApplicationUserService userService)
    {
        _repository = repository;
        _sessionTracker = sessionTracker;
        _userService = userService;
    }

    public IActionResult Index(string? profile = null)
    {
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
            Cards = BuildCollectorOperationalCards(clients),
            TodayClients = todayClients
        };

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

        var model = new CollectorQueueViewModel
        {
            Profile = activeProfile,
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "day" : groupBy.ToLowerInvariant(),
            Filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.ToLowerInvariant(),
            SelectedDay = selectedDay,
            SelectedStatus = selectedStatus,
            SelectedZone = selectedZone,
            SearchPlaceholder = "Buscar por nombre, direccion, telefono o folio",
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
            PortfolioItem = item,
            Sale = _repository.GetById(id),
            Input = new CollectionFormInput
            {
                IdV = item.IdV,
                FechaCobro = DateTime.Today,
                Usuario = string.IsNullOrWhiteSpace(profile) ? item.Cobrador : profile,
                ImporteCobro = item.ImporteRestante > 0 ? item.ImporteRestante : 0,
                CoordenadasCobro = item.Coordenadas
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(CollectionFormInput input, string? profile = null, string? day = null, string? status = null, string? zone = null)
    {
        try
        {
            _repository.RegisterCollection(input);
            _sessionTracker.UpdateCoordinates(User.Identity?.Name ?? string.Empty, input.CoordenadasCobro, "Cobro registrado");
            TempData["CobroMessage"] = "Cobro registrado correctamente.";
            return IsSupervisor()
                ? RedirectToAction(nameof(SupervisorMonitor), new { profile, groupBy = "zone" })
                : RedirectToAction(nameof(CollectorQueue), new { profile = ResolveCollectorProfile(profile), groupBy = "day", filter = "today" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var model = new CollectionRegisterViewModel
            {
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
            var notLocated = ContainsKeyword(note, "NO LOCALIZ") || ContainsKeyword(note, "NO ENCONTR");
            var wasVisited = history is { Count: > 0 } || !string.IsNullOrWhiteSpace(note);

            list.Add(new CollectorClientListItem
            {
                IdV = item.IdV,
                NumVenta = item.NumVenta,
                Name = item.NombreCliente,
                Zone = item.Zona,
                Route = item.DiaCobroPrevisto,
                Status = item.Estatus,
                Priority = ComputePriority(item.Estatus, hasPromise, notLocated),
                NextAction = ComputeNextAction(item.Estatus, hasPromise, notLocated, wasVisited),
                ReferenceText = string.IsNullOrWhiteSpace(sale?.ObservacionVenta) ? $"Zona {item.Zona}" : sale.ObservacionVenta!,
                Phone = item.Celular,
                Coordinates = item.Coordenadas,
                Thumbnail = string.IsNullOrWhiteSpace(item.FotoCliente) ? item.FotoFachada : item.FotoCliente,
                LastPaymentDate = last?.FechaCobro,
                LastNote = string.IsNullOrWhiteSpace(note) ? "Sin gestion reciente" : note,
                SaleState = item.EstadoVenta,
                HasPromise = hasPromise,
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
        var days = new[]
        {
            ("LUNES", "L", "Lunes"),
            ("MARTES", "M", "Martes"),
            ("MIERCOLES", "M", "Miercoles"),
            ("JUEVES", "J", "Jueves"),
            ("VIERNES", "V", "Viernes")
        };

        return days
            .Select(day => new CollectorDayTab
            {
                Code = day.Item1,
                ShortLabel = day.Item2,
                Label = day.Item3,
                Count = clients.Count(x => NormalizeDayKey(x.Route) == day.Item1),
                IsActive = NormalizeDayKey(selectedDay) == day.Item1
            })
            .ToArray();
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
            "followup" => NormalizeKey(item.NextAction) == "SEGUIMIENTO" || ContainsKeyword(item.LastNote, "REAGEND"),
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
            "notlocated" => clients.Where(x => ContainsKeyword(x.LastNote, "NO LOCALIZ") || ContainsKeyword(x.LastNote, "NO ENCONTR")).ToList(),
            "followup" => clients.Where(x => NormalizeKey(x.NextAction) == NormalizeKey("Seguimiento") || ContainsKeyword(x.LastNote, "REAGEND")).ToList(),
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

    private static string ComputePriority(string status, bool hasPromise, bool notLocated)
    {
        if (NormalizeKey(status) == "ATRASADO" || notLocated)
        {
            return "Alta";
        }

        if (hasPromise || NormalizeKey(status) == "POR INICIAR")
        {
            return "Media";
        }

        return "Normal";
    }

    private static string ComputeNextAction(string status, bool hasPromise, bool notLocated, bool wasVisited)
    {
        if (notLocated)
        {
            return "Revisar referencia";
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
}

