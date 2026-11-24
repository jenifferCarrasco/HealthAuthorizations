using FluentValidation;
using HealthAuthorization.Application.Common.Validators;
using HealthAuthorization.Application.Features.Providers.Queries;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class GetProvidersQueryValidator : PaginationFilterValidator<GetProvidersQuery>
{
    public GetProvidersQueryValidator()
    {
        RuleFor(query => query.ProviderTypeId).NotEqual(Guid.Empty).WithMessage("El tipo de prestador no puede estar vacío.");
    }
}
