using FluentValidation;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Contractors.Queries;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class GetContractorsQueryValidator : AbstractValidator<GetContractorsQuery>
{
    public GetContractorsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0).WithMessage("La página debe ser mayor que cero.");
        RuleFor(query => query.PageSize).InclusiveBetween(1, InputLimits.MaxPageSize)
            .WithMessage("El tamaño de página no es válido.");
        RuleFor(query => query).Must(query => ((long)query.PageNumber - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("La paginación solicitada no es válida.");
    }
}
