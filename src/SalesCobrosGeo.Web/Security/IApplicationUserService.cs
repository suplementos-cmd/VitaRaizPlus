using System.Security.Claims;
using SalesCobrosGeo.Web.Models.Administration;

namespace SalesCobrosGeo.Web.Security;

public interface IApplicationUserService
{
    ClaimsPrincipal? ValidateCredentials(string username, string password);

    IReadOnlyList<ApplicationUserSummary> GetUsers();

    IReadOnlyList<LoginCredentialHint> GetLoginHints();

    bool SetActive(string username, bool isActive);

    ApplicationUserSummary? GetUser(string username);

    ApplicationUserSummary SaveUser(UserAdminInput input);

    bool ResetPassword(string username, string newPassword);
}
