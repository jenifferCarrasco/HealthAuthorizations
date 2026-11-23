using FluentValidation;
using HealthAuthorization.Application.Features.ProviderTypes.Commands;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class ChangeProviderTypeStatusCommandValidator : AbstractValidator<ChangeProviderTypeStatusCommand>
{
    public ChangeProviderTypeStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
