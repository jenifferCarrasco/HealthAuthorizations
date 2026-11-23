using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations.Queries;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class GetAuthorizationsQueryValidator : AbstractValidator<GetAuthorizationsQuery>
{
    public GetAuthorizationsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0).WithMessage("La página debe ser mayor que cero.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, InputLimits.MaxPageSize)
            .WithMessage("El tamaño de página no es válido.");
        RuleFor(query => query).Must(query => ((long)query.PageNumber - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("La paginación solicitada no es válida.");
        RuleFor(query => query.Filters)
            .Must(filters => filters?.CreatedFrom is null || filters.CreatedBefore is null ||
                filters.CreatedFrom < filters.CreatedBefore)
            .WithMessage("La fecha inicial debe ser anterior a la fecha final.");
    }
}
