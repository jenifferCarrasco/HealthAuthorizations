using FluentValidation;
using HealthAuthorization.Application.Features.Contractors.Commands;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class UpdateContractorCommandValidator : AbstractValidator<UpdateContractorCommand>
{
    public UpdateContractorCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ContractorInputValidator());
    }
}
