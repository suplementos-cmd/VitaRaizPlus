using System.ComponentModel.DataAnnotations;

namespace SalesCobrosGeo.Api.Contracts.Auth;

public sealed record LoginRequest(
    [Required, MaxLength(64)] string UserName,
    [Required, MaxLength(128)] string Password);
