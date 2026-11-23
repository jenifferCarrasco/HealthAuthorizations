using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

internal sealed class AffiliateInputValidator : AbstractValidator<AffiliateInput>
{
    public AffiliateInputValidator(TimeProvider clock)
    {
        RuleFor(input => input.AffiliateNumber).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El número de afiliado es obligatorio.")
            .Must(value => value.Trim().Length <= InputLimits.Code).WithMessage("El número de afiliado es demasiado largo.");
        RuleFor(input => input.FirstName).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(value => value.Trim().Length <= InputLimits.Name).WithMessage("El nombre es demasiado largo.");
        RuleFor(input => input.LastName).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .Must(value => value.Trim().Length <= InputLimits.Name).WithMessage("El apellido es demasiado largo.");
        RuleFor(input => input.BirthDate).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La fecha de nacimiento es obligatoria.")
            .Must(value => value <= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithMessage("La fecha de nacimiento no puede ser futura.");
        RuleFor(input => input.Identification).Must(value => value is null || value.Trim().Length <= InputLimits.Code)
            .WithMessage("La identificación es demasiado larga.");
        RuleFor(input => input.DocumentType).Cascade(CascadeMode.Stop)
            .IsInEnum().WithMessage("El tipo de documento no es válido.")
            .Must((input, type) => string.IsNullOrWhiteSpace(input.Identification) == (type == DocumentType.None))
            .WithMessage("Indique un tipo de documento válido cuando haya una identificación y use None si no la hay.");
        RuleFor(input => input.ContractorId).NotEqual(Guid.Empty)
            .WithMessage("El identificador del contratante no puede estar vacío.");
    }
}
