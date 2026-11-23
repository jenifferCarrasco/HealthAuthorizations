using HealthAuthorization.Application.Common.Validators;
using HealthAuthorization.Application.Features.Intermediaries.Queries;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class GetIntermediariesQueryValidator : PaginationFilterValidator<GetIntermediariesQuery>
{
}
