using System.ComponentModel.DataAnnotations;
using SalesCobrosGeo.Web.Security;

namespace SalesCobrosGeo.Web.Models.Security;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contrasena es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contrasena")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Mantener sesion")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    public IReadOnlyList<LoginCredentialHint> Hints { get; set; } = [];
}
