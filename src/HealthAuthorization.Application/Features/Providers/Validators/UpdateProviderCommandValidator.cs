using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Providers.Commands;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).NotNull().WithMessage("Los datos son obligatorios.");
        When(command => command.Input is not null, () =>
        {
            RuleFor(command => command.Input.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
                .Must(value => value is null || value.Trim().Length <= InputLimits.Name)
                .WithMessage("El nombre es demasiado largo.");
            RuleFor(command => command.Input.Identification).NotEmpty().WithMessage("La identificación es obligatoria.")
                .Must(value => value is null || value.Trim().Length <= InputLimits.Code)
                .WithMessage("La identificación es demasiado larga.");
            RuleFor(command => command.Input.ProviderTypeId).NotEmpty().WithMessage("El tipo de prestador es obligatorio.");
        });
    }
}
