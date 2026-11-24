using FluentValidation;
using HealthAuthorization.Application.Features.ProviderTypes.Commands;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class UpdateProviderTypeCommandValidator : AbstractValidator<UpdateProviderTypeCommand>
{
    public UpdateProviderTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ProviderTypeInputValidator());
    }
}
