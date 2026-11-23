using FluentValidation;
using HealthAuthorization.Application.Features.Authorizations.Commands;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class ChangeAuthorizationStatusCommandValidator : AbstractValidator<ChangeAuthorizationStatusCommand>
{
    public ChangeAuthorizationStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
