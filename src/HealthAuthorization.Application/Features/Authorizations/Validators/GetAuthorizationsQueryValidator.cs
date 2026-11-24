using FluentValidation;
using HealthAuthorization.Application.Common.Validators;
using HealthAuthorization.Application.Features.Authorizations.Queries;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class GetAuthorizationsQueryValidator : PaginationFilterValidator<GetAuthorizationsQuery>
{
    public GetAuthorizationsQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => query.CreatedFrom is null || query.CreatedBefore is null ||
                query.CreatedFrom < query.CreatedBefore)
            .WithMessage("La fecha inicial debe ser anterior a la fecha final.");
    }
}
