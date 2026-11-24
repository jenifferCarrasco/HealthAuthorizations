using FluentValidation;
using HealthAuthorization.Application.Features.Intermediaries.Commands;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class CreateIntermediaryCommandValidator : AbstractValidator<CreateIntermediaryCommand>
{
    public CreateIntermediaryCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new IntermediaryInputValidator());
    }
}
