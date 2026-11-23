using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.ProviderTypes.Commands;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class UpdateProviderTypeCommandValidator : AbstractValidator<UpdateProviderTypeCommand>
{
    public UpdateProviderTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).NotNull().WithMessage("Los datos son obligatorios.");
        When(command => command.Input is not null, () =>
        {
            RuleFor(command => command.Input.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
                .Must(value => value is null || value.Trim().Length <= InputLimits.Name)
                .WithMessage("El nombre es demasiado largo.");
            RuleFor(command => command.Input.Description)
                .Must(value => value is null || value.Trim().Length <= InputLimits.Description)
                .WithMessage("La descripción es demasiado larga.");
        });
    }
}
