using FluentValidation;
using HealthAuthorization.Application.Features.Contractors.Queries;

namespace HealthAuthorization.Application.Features.Contractors.Validators;

public class GetContractorByIdQueryValidator : AbstractValidator<GetContractorByIdQuery>
{
    public GetContractorByIdQueryValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
