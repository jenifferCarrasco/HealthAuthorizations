using FluentValidation;
using HealthAuthorization.Application.Common;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

internal sealed class AuthorizationInputValidator : AbstractValidator<AuthorizationInput>
{
    public AuthorizationInputValidator(bool isCreation)
    {
        RuleFor(input => input.AffiliateId).NotEmpty().WithMessage("El identificador del afiliado es obligatorio.");
        RuleFor(input => input.ContractorId).NotEqual(Guid.Empty).WithMessage("El identificador del contratante no puede estar vacío.");
        RuleFor(input => input.IntermediaryId).NotEqual(Guid.Empty).WithMessage("El identificador del intermediario no puede estar vacío.");
        RuleFor(input => input.Services).NotEmpty().WithMessage("La autorización debe contener al menos un servicio.");
        RuleForEach(input => input.Services).NotNull().WithMessage("El servicio no puede estar vacío.");
        RuleForEach(input => input.Services).ChildRules(service =>
        {
            service.RuleFor(value => value.ProviderId).NotEmpty().WithMessage("El identificador del prestador es obligatorio.");
            service.RuleFor(value => value.ServiceTypeId).NotEmpty().WithMessage("El identificador del tipo de servicio es obligatorio.");
            service.RuleFor(value => value.ClaimedAmount).Cascade(CascadeMode.Stop)
                .GreaterThanOrEqualTo(0m).WithMessage("El monto reclamado no puede ser negativo.")
                .LessThanOrEqualTo(InputLimits.MaxClaimedAmount).WithMessage("El monto reclamado supera la precisión admitida.")
                .Must(amount => decimal.Round(amount, 2) == amount).WithMessage("El monto reclamado debe tener como máximo dos decimales.");
            if (isCreation)
            {
                service.RuleFor(value => value.DetailId).Null().WithMessage("Un servicio nuevo no debe incluir identificador de detalle.");
            }
            else
            {
                service.RuleFor(value => value.DetailId).NotEqual(Guid.Empty).WithMessage("El identificador de detalle no puede estar vacío.");
            }
        });
    }
}
