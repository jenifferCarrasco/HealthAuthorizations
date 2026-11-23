using FluentValidation;
using HealthAuthorization.Application.Features.Providers.Commands;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class ChangeProviderStatusCommandValidator : AbstractValidator<ChangeProviderStatusCommand>
{
    public ChangeProviderStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
