namespace SalesCobrosGeo.Api.Security;

public interface IUserStore
{
    AppUser? ValidateCredentials(string userName, string password);
    AppUser? FindByUserName(string userName);
}
