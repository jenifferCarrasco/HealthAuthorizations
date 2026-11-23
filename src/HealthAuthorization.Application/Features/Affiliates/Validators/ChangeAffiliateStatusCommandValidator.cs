using FluentValidation;
using HealthAuthorization.Application.Features.Affiliates.Commands;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

public class ChangeAffiliateStatusCommandValidator : AbstractValidator<ChangeAffiliateStatusCommand>
{
    public ChangeAffiliateStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
