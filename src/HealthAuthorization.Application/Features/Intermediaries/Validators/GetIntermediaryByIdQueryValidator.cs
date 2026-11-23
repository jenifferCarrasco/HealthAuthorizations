using FluentValidation;
using HealthAuthorization.Application.Features.Intermediaries.Queries;

namespace HealthAuthorization.Application.Features.Intermediaries.Validators;

public class GetIntermediaryByIdQueryValidator : AbstractValidator<GetIntermediaryByIdQuery>
{
    public GetIntermediaryByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
