using FluentValidation;
using HealthAuthorization.Application.Features.Providers.Commands;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ProviderInputValidator());
    }
}
