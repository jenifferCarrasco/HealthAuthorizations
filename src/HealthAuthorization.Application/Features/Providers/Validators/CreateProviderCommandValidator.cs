using FluentValidation;
using HealthAuthorization.Application.Features.Providers.Commands;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public CreateProviderCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ProviderInputValidator());
    }
}
