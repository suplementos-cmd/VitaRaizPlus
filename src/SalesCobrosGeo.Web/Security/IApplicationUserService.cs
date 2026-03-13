using System.Security.Claims;

namespace SalesCobrosGeo.Web.Security;

public interface IApplicationUserService
{
    ClaimsPrincipal? ValidateCredentials(string username, string password);

    IReadOnlyList<ApplicationUserSummary> GetUsers();

    IReadOnlyList<LoginCredentialHint> GetLoginHints();

    bool SetActive(string username, bool isActive);
}
