using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Providers.Queries;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class GetProvidersQueryValidator : AbstractValidator<GetProvidersQuery>
{
    public GetProvidersQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0).WithMessage("La página debe ser mayor que cero.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, InputLimits.MaxPageSize)
            .WithMessage("El tamaño de página no es válido.");
        RuleFor(query => query).Must(query => ((long)query.PageNumber - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("La paginación solicitada no es válida.");
        RuleFor(query => query.ProviderTypeId).NotEqual(Guid.Empty).WithMessage("El tipo de prestador no puede estar vacío.");
    }
}
