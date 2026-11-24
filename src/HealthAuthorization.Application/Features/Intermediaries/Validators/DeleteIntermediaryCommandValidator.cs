using FluentValidation;
using HealthAuthorization.Application.Features.Intermediaries.Commands;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class DeleteIntermediaryCommandValidator : AbstractValidator<DeleteIntermediaryCommand>
{
    public DeleteIntermediaryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
