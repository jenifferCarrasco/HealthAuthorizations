using FluentValidation;
using HealthAuthorization.Application.Features.ServiceTypes.Commands;

namespace HealthAuthorization.Application.Features.ServiceTypes.Validators;

public class UpdateServiceTypeCommandValidator : AbstractValidator<UpdateServiceTypeCommand>
{
    public UpdateServiceTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ServiceTypeInputValidator());
    }
}
