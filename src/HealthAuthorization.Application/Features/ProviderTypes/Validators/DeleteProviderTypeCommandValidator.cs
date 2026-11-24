using FluentValidation;
using HealthAuthorization.Application.Features.ProviderTypes.Commands;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class DeleteProviderTypeCommandValidator : AbstractValidator<DeleteProviderTypeCommand>
{
    public DeleteProviderTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
