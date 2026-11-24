using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HealthAuthorization.API.Controllers;
using HealthAuthorization.API.Errors;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations.Commands;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorization.IntegrationTests.Api;

public class AuthorizationIdempotencyHttpTests
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly object Input = new
    {
        affiliateId = Guid.NewGuid(),
        services = new[] { new { providerId = Guid.NewGuid(), serviceTypeId = Guid.NewGuid(), claimedAmount = 100m } }
    };

    [Fact]
    public async Task RejectsInvalidHeadersAndDocumentsTheRequiredHeader()
    {
        var handler = new CreateHandlerStub();
        await using var app = await StartApiAsync(handler);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        var invalidHeaders = new[]
        {
            Array.Empty<string>(), new[] { "invalid" }, new[] { Guid.Empty.ToString() },
            new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
            new[] { $"{Guid.NewGuid()}, {Guid.NewGuid()}" }
        };
        foreach (var values in invalidHeaders)
        {
            using var response = await PostAsync(client, values);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(HeaderName, out _));
        }

        Assert.Equal(0, handler.Calls);
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var parameters = json.RootElement.GetProperty("paths").GetProperty("/api/authorizations")
            .GetProperty("post").GetProperty("parameters");
        var header = Assert.Single(parameters.EnumerateArray(), parameter =>
            parameter.GetProperty("name").GetString() == HeaderName);
        Assert.Equal("header", header.GetProperty("in").GetString());
        Assert.True(header.GetProperty("required").GetBoolean());
        Assert.Equal("uuid", header.GetProperty("schema").GetProperty("format").GetString());
    }

    [Fact]
    public async Task ReturnsTheSameCreatedResponseWhenTheHandlerReturnsTheSameId()
    {
        var handler = new CreateHandlerStub();
        await using var app = await StartApiAsync(handler);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var key = Guid.NewGuid();

        using var first = await PostAsync(client, new[] { key.ToString() });
        using var replay = await PostAsync(client, new[] { key.ToString() });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(first.StatusCode, replay.StatusCode);
        Assert.NotNull(first.Headers.Location);
        Assert.Equal(first.Headers.Location, replay.Headers.Location);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
        Assert.Equal(key, handler.LastKey);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task ReturnsProblemDetailsWhenTheHandlerDetectsAConflict()
    {
        var handler = new CreateHandlerStub
        {
            Error = new ConflictException("La clave de idempotencia ya se utilizó con datos diferentes.")
        };
        await using var app = await StartApiAsync(handler);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        using var response = await PostAsync(client, new[] { Guid.NewGuid().ToString() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(handler.Error.Message, problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ValidHeaderDoesNotBypassCreateCommandValidation()
    {
        var handler = new CreateHandlerStub();
        await using var app = await StartApiAsync(handler);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var input = new { affiliateId = Guid.NewGuid(), services = Array.Empty<object>() };

        using var response = await PostAsync(client, new[] { Guid.NewGuid().ToString() }, input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, handler.Calls);
    }

    private static async Task<WebApplication> StartApiAsync(CreateHandlerStub handler)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthorizationsController).Assembly);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddOpenApi();
        builder.Services.AddApplication();
        builder.Services.AddSingleton<IRequestHandler<CreateAuthorizationCommand, Guid>>(handler);
        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapControllers();
        app.MapOpenApi();
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string[] values, object? input = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authorizations");
        request.Content = JsonContent.Create(input ?? Input);
        if (values.Length > 0)
        {
            request.Headers.TryAddWithoutValidation(HeaderName, values);
        }

        return await client.SendAsync(request);
    }

    private class CreateHandlerStub : IRequestHandler<CreateAuthorizationCommand, Guid>
    {
        private readonly Guid _authorizationId = Guid.NewGuid();

        public int Calls { get; private set; }
        public Guid LastKey { get; private set; }
        public ConflictException? Error { get; set; }

        public Task<Guid> Handle(CreateAuthorizationCommand request, CancellationToken cancellationToken)
        {
            Calls++;
            LastKey = request.IdempotencyKey;
            if (Error is not null)
            {
                return Task.FromException<Guid>(Error);
            }

            return Task.FromResult(_authorizationId);
        }
    }
}
