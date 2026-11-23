using FluentValidation;
using HealthAuthorization.Application.Features.Affiliates.Commands;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

public class DeleteAffiliateCommandValidator : AbstractValidator<DeleteAffiliateCommand>
{
    public DeleteAffiliateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
