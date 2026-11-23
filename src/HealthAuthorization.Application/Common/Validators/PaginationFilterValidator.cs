using FluentValidation;
using HealthAuthorization.Application.Common.Models;

namespace HealthAuthorization.Application.Common.Validators;

public abstract class PaginationFilterValidator<TFilter> : AbstractValidator<TFilter>
    where TFilter : PaginationFilter
{
    protected PaginationFilterValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0).WithMessage("La página debe ser mayor que cero.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, InputLimits.MaxPageSize)
            .WithMessage("El tamaño de página no es válido.");
        RuleFor(query => query).Must(query => ((long)query.PageNumber - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("La paginación solicitada no es válida.");
    }
}
