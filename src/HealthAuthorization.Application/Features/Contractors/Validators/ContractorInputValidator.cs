using FluentValidation;
using HealthAuthorization.Application.Common;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

internal class ContractorInputValidator : AbstractValidator<ContractorInput>
{
    public ContractorInputValidator()
    {
        RuleFor(input => input.Name).NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(value => value is null || value.Trim().Length <= InputLimits.Name)
            .WithMessage("El nombre es demasiado largo.");
        RuleFor(input => input.Identification).NotEmpty().WithMessage("La identificación es obligatoria.")
            .Must(value => value is null || value.Trim().Length <= InputLimits.Code)
            .WithMessage("La identificación es demasiado larga.");
    }
}
