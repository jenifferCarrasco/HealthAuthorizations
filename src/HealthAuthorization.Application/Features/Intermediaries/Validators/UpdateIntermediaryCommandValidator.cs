using FluentValidation;
using HealthAuthorization.Application.Features.Intermediaries.Commands;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class UpdateIntermediaryCommandValidator : AbstractValidator<UpdateIntermediaryCommand>
{
    public UpdateIntermediaryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new IntermediaryInputValidator());
    }
}
