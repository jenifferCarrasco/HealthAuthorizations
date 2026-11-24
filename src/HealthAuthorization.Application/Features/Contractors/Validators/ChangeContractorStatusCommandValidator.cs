using FluentValidation;
using HealthAuthorization.Application.Features.Contractors.Commands;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class ChangeContractorStatusCommandValidator : AbstractValidator<ChangeContractorStatusCommand>
{
    public ChangeContractorStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
