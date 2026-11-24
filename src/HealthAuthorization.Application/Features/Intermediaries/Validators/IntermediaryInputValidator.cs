using FluentValidation;
using HealthAuthorization.Application.Common;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

internal class IntermediaryInputValidator : AbstractValidator<IntermediaryInput>
{
    public IntermediaryInputValidator()
    {
        RuleFor(input => input.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(value => value is null || value.Trim().Length <= InputLimits.Name)
            .WithMessage("El nombre es demasiado largo.");
        RuleFor(input => input.Identification).NotEmpty().WithMessage("La identificación es obligatoria.")
            .Must(value => value is null || value.Trim().Length <= InputLimits.Code)
            .WithMessage("La identificación es demasiado larga.");
    }
}
