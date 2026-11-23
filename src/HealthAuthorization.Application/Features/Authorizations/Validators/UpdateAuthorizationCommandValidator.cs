using FluentValidation;
using HealthAuthorization.Application.Features.Authorizations.Commands;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class UpdateAuthorizationCommandValidator : AbstractValidator<UpdateAuthorizationCommand>
{
    public UpdateAuthorizationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador de la autorización es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos de la autorización son obligatorios.")
            .SetValidator(new AuthorizationInputValidator(isCreation: false));
    }
}
