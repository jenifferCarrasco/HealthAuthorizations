using FluentValidation;
using HealthAuthorization.Application.Common;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

internal class ProviderTypeInputValidator : AbstractValidator<ProviderTypeInput>
{
    public ProviderTypeInputValidator()
    {
        RuleFor(input => input.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(value => value is null || value.Trim().Length <= InputLimits.Name)
            .WithMessage("El nombre es demasiado largo.");
        RuleFor(input => input.Description)
            .Must(value => value is null || value.Trim().Length <= InputLimits.Description)
            .WithMessage("La descripción es demasiado larga.");
    }
}
