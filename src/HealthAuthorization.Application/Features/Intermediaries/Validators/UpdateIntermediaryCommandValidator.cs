using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Intermediaries.Commands;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class UpdateIntermediaryCommandValidator : AbstractValidator<UpdateIntermediaryCommand>
{
    public UpdateIntermediaryCommandValidator()
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
        });
    }
}
