using FluentValidation;
using HealthAuthorization.Application.Features.Affiliates.Commands;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

public sealed class UpdateAffiliateCommandValidator : AbstractValidator<UpdateAffiliateCommand>
{
    public UpdateAffiliateCommandValidator(TimeProvider clock)
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador del afiliado es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos del afiliado son obligatorios.")
            .SetValidator(new AffiliateInputValidator(clock));
    }
}
