using FluentValidation;
using HealthAuthorization.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorization.API.Errors;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var result = CreateResult(exception, httpContext.TraceIdentifier);
        await result.ExecuteAsync(httpContext);
        return true;
    }

    private IResult CreateResult(Exception exception, string traceIdentifier)
    {
        switch (exception)
        {
            case ValidationException validation:
                return CreateValidationResult(validation);

            case BadHttpRequestException badRequest:
                return Results.Problem(
                    statusCode: badRequest.StatusCode,
                    title: "La solicitud no es válida.");

            case NotFoundException notFound:
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Registro no encontrado.",
                    detail: notFound.Message);

            case ConflictException conflict:
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflicto al procesar la solicitud.",
                    detail: conflict.Message);

            case DbUpdateConcurrencyException:
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflicto de concurrencia.",
                    detail: "El registro cambió durante la operación. Vuelva a consultarlo antes de reintentar.");

            default:
                _logger.LogError(exception, "Error inesperado al procesar la solicitud {TraceIdentifier}.", traceIdentifier);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Ocurrió un error interno al procesar la solicitud.");
        }
    }

    private static IResult CreateValidationResult(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        if (errors.Count == 0)
        {
            errors.Add(string.Empty, new[] { exception.Message });
        }

        return Results.ValidationProblem(errors, title: "La solicitud contiene errores de validación.");
    }
}
