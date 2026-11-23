using FluentValidation;
using HealthAuthorization.Application.Features.ProviderTypes.Queries;

namespace HealthAuthorization.Application.Features.ProviderTypes.Validators;

public class GetProviderTypeByIdQueryValidator : AbstractValidator<GetProviderTypeByIdQuery>
{
    public GetProviderTypeByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
