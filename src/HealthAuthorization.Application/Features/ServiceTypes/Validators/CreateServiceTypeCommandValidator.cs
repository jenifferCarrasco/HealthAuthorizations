using FluentValidation;
using HealthAuthorization.Application.Features.ServiceTypes.Commands;

namespace HealthAuthorization.Application.Features.ServiceTypes.Validators;

public class CreateServiceTypeCommandValidator : AbstractValidator<CreateServiceTypeCommand>
{
    public CreateServiceTypeCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ServiceTypeInputValidator());
    }
}
