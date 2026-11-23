using FluentValidation;
using HealthAuthorization.Application.Features.Authorizations.Commands;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public sealed class DeactivateAuthorizationCommandValidator : AbstractValidator<DeactivateAuthorizationCommand>
{
    public DeactivateAuthorizationCommandValidator() =>
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador de la autorización es obligatorio.");
}
