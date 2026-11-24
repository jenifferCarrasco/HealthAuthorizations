using FluentValidation;
using HealthAuthorization.Application.Features.ProviderTypes.Commands;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class CreateProviderTypeCommandValidator : AbstractValidator<CreateProviderTypeCommand>
{
    public CreateProviderTypeCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ProviderTypeInputValidator());
    }
}
