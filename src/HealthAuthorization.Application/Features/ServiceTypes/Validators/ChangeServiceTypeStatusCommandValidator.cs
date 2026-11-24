using FluentValidation;
using HealthAuthorization.Application.Features.ServiceTypes.Commands;

namespace HealthAuthorization.Application.Features.ServiceTypes.Validators;

public class ChangeServiceTypeStatusCommandValidator : AbstractValidator<ChangeServiceTypeStatusCommand>
{
    public ChangeServiceTypeStatusCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
