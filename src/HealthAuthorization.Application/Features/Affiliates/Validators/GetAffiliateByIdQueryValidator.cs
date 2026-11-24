using FluentValidation;
using HealthAuthorization.Application.Features.Affiliates.Queries;

namespace HealthAuthorization.Application.Features.Affiliates.Validators;

public class GetAffiliateByIdQueryValidator : AbstractValidator<GetAffiliateByIdQuery>
{
    public GetAffiliateByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
