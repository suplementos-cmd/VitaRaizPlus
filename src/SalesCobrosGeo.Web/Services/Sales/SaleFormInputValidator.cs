using FluentValidation;
using SalesCobrosGeo.Shared;
using SalesCobrosGeo.Web.Models.Sales;

namespace SalesCobrosGeo.Web.Services.Sales;

/// <summary>
/// FluentValidation validator for <see cref="SaleFormInput"/>.
/// Replaces the manual TryValidateInput() method that was in SalesController.
/// Registered automatically via FluentValidation.AspNetCore DI scanning.
/// </summary>
public sealed class SaleFormInputValidator : AbstractValidator<SaleFormInput>
{
    public SaleFormInputValidator()
    {
        RuleFor(x => x.NombreCliente)
            .NotEmpty().WithMessage("Nombre cliente es obligatorio.")
            .MaximumLength(128).WithMessage("Nombre cliente no puede exceder 128 caracteres.");

        RuleFor(x => x.Celular)
            .NotEmpty().WithMessage("Celular es obligatorio.")
            .Matches(@"^\d{7,15}$").WithMessage("Celular debe contener entre 7 y 15 dígitos.");

        RuleFor(x => x.Zona)
            .NotEmpty().WithMessage("Zona es obligatoria.");

        RuleFor(x => x.FormaPago)
            .NotEmpty().WithMessage("Forma de pago es obligatoria.");

        RuleFor(x => x.DiaCobro)
            .NotEmpty().WithMessage("Día de cobro es obligatorio.");

        RuleFor(x => x.Vendedor)
            .NotEmpty().WithMessage("Vendedor es obligatorio.");

        RuleFor(x => x.Coordenadas)
            .NotEmpty().WithMessage("Coordenadas son obligatorias.")
            .Must(BeValidGeoPoint)
            .WithMessage("Coordenadas inválidas. Formato esperado: lat,lng (ej: 19.4326,-99.1332).");

        RuleFor(x => x.Coordenadas2)
            .Must(BeValidGeoPointOrEmpty)
            .WithMessage("Coordenadas secundarias inválidas. Formato esperado: lat,lng.")
            .When(x => !string.IsNullOrWhiteSpace(x.Coordenadas2));

        RuleFor(x => x.Productos)
            .NotEmpty().WithMessage("Debe agregar al menos un producto.")
            .Must(p => p.Any(l => !string.IsNullOrWhiteSpace(l.ProductCode) && l.Quantity > 0))
            .WithMessage("Al menos un producto debe tener código y cantidad mayor a cero.");

        RuleFor(x => x.ComisionVendedorPct)
            .InclusiveBetween(0, 100).WithMessage("La comisión debe estar entre 0 y 100.");

        RuleFor(x => x.FechaVenta)
            .NotEmpty().WithMessage("Fecha de venta es obligatoria.")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("Fecha de venta no puede ser futura.");
    }

    private static bool BeValidGeoPoint(string? raw)
        => GeoPoint.TryParse(raw, out _);

    private static bool BeValidGeoPointOrEmpty(string? raw)
        => string.IsNullOrWhiteSpace(raw) || GeoPoint.TryParse(raw, out _);
}
