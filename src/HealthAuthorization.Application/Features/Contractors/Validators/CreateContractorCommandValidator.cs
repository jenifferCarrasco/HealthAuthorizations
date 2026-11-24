using FluentValidation;
using HealthAuthorization.Application.Features.Contractors.Commands;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class CreateContractorCommandValidator : AbstractValidator<CreateContractorCommand>
{
    public CreateContractorCommandValidator()
    {
        RuleFor(command => command.Input).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los datos son obligatorios.")
            .SetValidator(new ContractorInputValidator());
    }
}
