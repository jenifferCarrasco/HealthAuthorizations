using FluentValidation;
using HealthAuthorization.Application.Features.Contractors.Commands;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class DeleteContractorCommandValidator : AbstractValidator<DeleteContractorCommand>
{
    public DeleteContractorCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
