using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SalesCobrosGeo.Web.Security;

public interface IUserSessionTracker
{
    ClaimsPrincipal AttachSession(ClaimsPrincipal principal, ApplicationUserSummary user, HttpContext httpContext);

    bool IsSessionValid(ClaimsPrincipal principal);

    void TouchRequest(ClaimsPrincipal principal, HttpContext httpContext);

    void SignOut(ClaimsPrincipal principal);

    void ForceLogout(string username);

    void SetUserActive(string username, bool isActive);

    void UpdateCoordinates(string username, string? coordinates, string source);

    IReadOnlyList<UserSessionSnapshot> GetSnapshots(IReadOnlyList<ApplicationUserSummary> users);
}
