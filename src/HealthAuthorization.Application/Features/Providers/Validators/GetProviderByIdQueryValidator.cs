using FluentValidation;
using HealthAuthorization.Application.Features.Providers.Queries;

namespace HealthAuthorization.Application.Features.Providers.Validators;

public class GetProviderByIdQueryValidator : AbstractValidator<GetProviderByIdQuery>
{
    public GetProviderByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
