using SalesCobrosGeo.Shared.Security;

namespace SalesCobrosGeo.Api.Security;

public interface ITokenService
{
    TokenSession IssueToken(AppUser user);
    bool TryValidate(string token, out TokenSession? session);
    void Revoke(string token);
}
