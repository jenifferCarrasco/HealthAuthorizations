using FluentValidation;
using HealthAuthorization.Application.Features.Affiliates.Commands;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

public class CreateAffiliateCommandValidator : AbstractValidator<CreateAffiliateCommand>
{
    public CreateAffiliateCommandValidator(TimeProvider clock)
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos del afiliado son obligatorios.")
            .SetValidator(new AffiliateInputValidator(clock));
    }
}
