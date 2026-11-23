using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using HealthAuthorization.API.Errors;
using HealthAuthorization.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthAuthorization.IntegrationTests.Api;

public class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ValidationErrorsAreGroupedByProperty()
    {
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Input.Name", "El nombre es obligatorio."),
            new ValidationFailure("Input.Name", "El nombre es demasiado largo.")
        });

        using var response = await HandleAsync(exception, StatusCodes.Status400BadRequest);
        var messages = response.RootElement.GetProperty("errors").GetProperty("Input.Name");

        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("El nombre es obligatorio.", messages[0].GetString());
    }

    [Fact]
    public async Task ValidationWithoutPropertyErrorsRetainsItsMessage()
    {
        var exception = new ValidationException("La autorización debe contener al menos un servicio.");
        using var response = await HandleAsync(exception, StatusCodes.Status400BadRequest);

        Assert.Equal(exception.Message,
            response.RootElement.GetProperty("errors").GetProperty(string.Empty)[0].GetString());
    }

    [Fact]
    public async Task MissingRecordReturnsNotFound()
    {
        var exception = new NotFoundException("El afiliado no existe.");
        using var response = await HandleAsync(exception, StatusCodes.Status404NotFound);

        Assert.Equal(exception.Message, response.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task BusinessConflictReturnsConflict()
    {
        var exception = new ConflictException("El registro cambió durante la operación.");
        using var response = await HandleAsync(exception, StatusCodes.Status409Conflict);

        Assert.Equal(exception.Message, response.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task EfConcurrencyConflictDoesNotExposeInternalDetails()
    {
        var exception = new DbUpdateConcurrencyException("Detalle interno de EF Core");
        using var response = await HandleAsync(exception, StatusCodes.Status409Conflict);

        Assert.DoesNotContain(exception.Message, response.RootElement.GetRawText());
    }

    [Fact]
    public async Task UnexpectedErrorReturnsGenericProblem()
    {
        var exception = new InvalidOperationException("Detalle interno de la excepción");
        using var response = await HandleAsync(exception, StatusCodes.Status500InternalServerError);

        Assert.DoesNotContain(exception.Message, response.RootElement.GetRawText());
        Assert.False(response.RootElement.TryGetProperty("detail", out _));
    }

    private static async Task<JsonDocument> HandleAsync(Exception exception, int expectedStatusCode)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        using var provider = services.BuildServiceProvider();
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = body;

        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);

        body.Position = 0;
        var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(expectedStatusCode, document.RootElement.GetProperty("status").GetInt32());
        return document;
    }
}
