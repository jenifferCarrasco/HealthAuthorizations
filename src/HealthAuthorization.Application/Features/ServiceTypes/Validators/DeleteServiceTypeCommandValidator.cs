using FluentValidation;
using HealthAuthorization.Application.Features.ServiceTypes.Commands;

namespace HealthAuthorization.Application.Features.ServiceTypes.Validators;

public class DeleteServiceTypeCommandValidator : AbstractValidator<DeleteServiceTypeCommand>
{
    public DeleteServiceTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithMessage("El identificador es obligatorio.");
    }
}
