namespace SalesCobrosGeo.Web.Data;

public sealed class AppUserEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public List<AppUserPermissionEntity> Permissions { get; set; } = [];
    public List<AppSessionEntity> Sessions { get; set; } = [];
}

public sealed class AppUserPermissionEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    public AppUserEntity? User { get; set; }
}

public sealed class AppSessionEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public bool IsRevoked { get; set; }
    public bool IsActiveUser { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public string LastPath { get; set; } = "-";
    public string LastIp { get; set; } = "-";
    public string LastUserAgent { get; set; } = "-";
    public string LastCoordinates { get; set; } = "-";
    public string LastLocationSource { get; set; } = "Sin traza";

    public AppUserEntity? User { get; set; }
}

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = "-";
    public string Ip { get; set; } = "-";
    public string Coordinates { get; set; } = "-";
    public string Metadata { get; set; } = string.Empty;
}
