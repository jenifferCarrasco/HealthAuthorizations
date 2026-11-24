using FluentValidation;
using HealthAuthorization.Application.Features.ServiceTypes.Queries;

namespace HealthAuthorization.Application.Features.ServiceTypes.Validators;

public class GetServiceTypeByIdQueryValidator : AbstractValidator<GetServiceTypeByIdQuery>
{
    public GetServiceTypeByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
