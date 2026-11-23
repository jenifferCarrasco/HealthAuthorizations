using FluentValidation;
using HealthAuthorization.Application.Features.Authorizations.Queries;

namespace HealthAuthorization.Application.Features.Authorizations.Validators;

public class GetAuthorizationByIdQueryValidator : AbstractValidator<GetAuthorizationByIdQuery>
{
    public GetAuthorizationByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
