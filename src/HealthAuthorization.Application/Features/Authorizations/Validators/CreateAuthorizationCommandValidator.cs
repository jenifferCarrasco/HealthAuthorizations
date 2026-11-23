using FluentValidation;
using HealthAuthorization.Application.Features.Authorizations.Commands;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class CreateAuthorizationCommandValidator : AbstractValidator<CreateAuthorizationCommand>
{
    public CreateAuthorizationCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos de la autorización son obligatorios.")
            .SetValidator(new AuthorizationInputValidator(isCreation: true));
    }
}
