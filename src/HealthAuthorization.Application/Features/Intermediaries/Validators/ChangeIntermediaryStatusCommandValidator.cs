using FluentValidation;
using HealthAuthorization.Application.Features.Intermediaries.Commands;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class ChangeIntermediaryStatusCommandValidator : AbstractValidator<ChangeIntermediaryStatusCommand>
{
    public ChangeIntermediaryStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
