using FluentValidation;
using HealthAuthorization.Application.Features.Providers.Commands;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
