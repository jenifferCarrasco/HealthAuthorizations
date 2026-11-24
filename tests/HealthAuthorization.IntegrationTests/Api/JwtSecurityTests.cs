using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using HealthAuthorization.API.Controllers;
using HealthAuthorization.API.Hubs;
using HealthAuthorization.API.Notifications;
using HealthAuthorization.API.Security;
using HealthAuthorization.Application;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Application.Features.Authorizations.Commands;
using HealthAuthorization.Application.Features.Authorizations.Notifications;
using HealthAuthorization.Application.Features.Authorizations.Queries;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Infrastructure;
using HealthAuthorization.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorization.IntegrationTests.Api;

public class JwtSecurityTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private readonly DevelopmentJwtTestTokens _tokens = new DevelopmentJwtTestTokens();

    [Fact]
    public async Task RejectsMissingInvalidExpiredAndMisdirectedTokens()
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        var tokens = new[]
        {
            null, "invalid", _tokens.Create("Operator", UserId.ToString(), expired: true),
            _tokens.Create("Operator", UserId.ToString(), invalidSignature: true),
            _tokens.Create("Operator", UserId.ToString(), issuer: "otro-emisor"),
            _tokens.Create("Operator", UserId.ToString(), audience: "otra-api")
        };

        foreach (var token in tokens)
        {
            using var response = await SendAsync(client, HttpMethod.Get, "/api/authorizations", token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Contains(response.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
        }
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("not-a-guid", null)]
    [InlineData("00000000-0000-0000-0000-000000000000", null)]
    [InlineData("11111111-1111-4111-8111-111111111111", "22222222-2222-4222-8222-222222222222")]
    public async Task RejectsMissingInvalidEmptyAndAmbiguousIdentity(string? userId, string? secondUserId)
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var token = _tokens.Create("Administrator", userId, secondUserId: secondUserId);

        using var response = await SendAsync(client, HttpMethod.Get, "/api/authorizations", token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnforcesTheRoleMatrixOnTheBusinessControllers()
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var operatorToken = _tokens.Create("Operator", UserId.ToString());
        var administratorToken = _tokens.Create("Administrator", UserId.ToString());
        var viewerToken = _tokens.Create("Viewer", UserId.ToString());
        using var operatorRead = await SendAsync(client, HttpMethod.Get, "/api/authorizations", operatorToken);
        using var administratorRead = await SendAsync(client, HttpMethod.Get, "/api/authorizations", administratorToken);
        using var viewerRead = await SendAsync(client, HttpMethod.Get, "/api/authorizations", viewerToken);
        Assert.Equal(HttpStatusCode.OK, operatorRead.StatusCode);
        Assert.Equal(HttpStatusCode.OK, administratorRead.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewerRead.StatusCode);

        foreach (var catalog in new[] { "affiliates", "contractors", "intermediaries", "providers", "provider-types", "service-types" })
        {
            var collection = $"/api/{catalog}";
            var item = $"{collection}/{Guid.NewGuid()}";
            var operations = new[]
            {
                (HttpMethod.Post, collection), (HttpMethod.Put, item),
                (HttpMethod.Delete, item), (HttpMethod.Patch, $"{item}/status")
            };
            foreach (var (method, path) in operations)
            {
                using var response = await SendAsync(client, method, path, operatorToken);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }

        var status = $"/api/authorizations/{Guid.NewGuid()}/status";
        var change = new { isActive = false };
        using var operatorStatus = await SendAsync(client, HttpMethod.Patch, status, operatorToken, change);
        using var administratorStatus = await SendAsync(client, HttpMethod.Patch, status, administratorToken, change);
        Assert.Equal(HttpStatusCode.Forbidden, operatorStatus.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, administratorStatus.StatusCode);

        var update = new
        {
            affiliateId = Guid.NewGuid(),
            services = new[] { new { providerId = Guid.NewGuid(), serviceTypeId = Guid.NewGuid(), claimedAmount = 100m } }
        };
        using var operatorUpdate = await SendAsync(client, HttpMethod.Put, $"/api/authorizations/{Guid.NewGuid()}",
            operatorToken, update);
        Assert.Equal(HttpStatusCode.NoContent, operatorUpdate.StatusCode);
    }

    [Fact]
    public async Task HubRequiresAuthorizationAndOnlyItsTransportCanReadQueryTokens()
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        var operatorToken = _tokens.Create("Operator", UserId.ToString());
        var viewerToken = _tokens.Create("Viewer", UserId.ToString());
        var route = AuthorizationHub.Route;
        var negotiate = $"{route}/negotiate?negotiateVersion=1";
        using var noToken = await SendAsync(client, HttpMethod.Post, negotiate, null);
        using var viewer = await SendAsync(client, HttpMethod.Post, negotiate, viewerToken);
        using var authorized = await SendAsync(client, HttpMethod.Post, negotiate, operatorToken);
        using var queryOnly = await SendAsync(client, HttpMethod.Get,
            $"/api/authorizations?access_token={operatorToken}", null);
        using var negotiateQueryOnly = await SendAsync(client, HttpMethod.Post,
            $"{negotiate}&access_token={operatorToken}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, queryOnly.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, negotiateQueryOnly.StatusCode);

        using var socket = new ClientWebSocket();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var address = new UriBuilder(client.BaseAddress!) { Scheme = "ws", Path = route, Query = $"access_token={operatorToken}" };
        await socket.ConnectAsync(address.Uri, timeout.Token);
        Assert.Equal(WebSocketState.Open, socket.State);
        socket.Abort();
    }

    [Fact]
    public async Task StampsCreateAndUpdateWithTheAuthenticatedGuid()
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var response = await SendAsync(client, HttpMethod.Get, "/tests/audit",
            _tokens.Create("Operator", UserId.ToString()));

        response.EnsureSuccessStatusCode();
        var audit = await response.Content.ReadFromJsonAsync<Guid?[]>();
        Assert.Equal(new Guid?[] { UserId, UserId }, audit);
    }

    [Fact]
    public async Task OpenApiDocumentsBearerAndProtectedResponses()
    {
        await using var app = await StartApiAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var root = json.RootElement;
        var bearer = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.True(root.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        var responses = root.GetProperty("paths").GetProperty("/api/authorizations").GetProperty("post").GetProperty("responses");
        Assert.True(responses.TryGetProperty("401", out _));
        Assert.True(responses.TryGetProperty("403", out _));
    }

    [Fact]
    public async Task ProductionAcceptsTheExternalIssuerAndRejectsDevelopmentTokens()
    {
        const string issuer = "https://identity.example.test";
        const string audience = "health-authorization";
        using var rsa = RSA.Create(2048);
        var publicKey = new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "external-test" };
        var signingKey = new RsaSecurityKey(rsa) { KeyId = publicKey.KeyId };
        var external = new JwtSecurityToken(issuer, audience,
            new[] { new Claim("healthauthorization_user_id", UserId.ToString()), new Claim("roles", "Operator") },
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
        var metadata = new OpenIdConnectConfiguration { Issuer = issuer };
        metadata.SigningKeys.Add(publicKey);

        await using var app = await StartApiAsync("Production", builder =>
        {
            builder.Configuration["Jwt:Authority"] = issuer;
            builder.Configuration["Jwt:Audience"] = audience;
            builder.Services.PostConfigure<JwtBearerOptions>("ProductionJwt", options =>
            {
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
            });
        });
        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Null(await schemes.GetSchemeAsync(SecurityConfiguration.DevelopmentScheme));

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var local = await SendAsync(client, HttpMethod.Get, "/api/authorizations",
            _tokens.Create("Operator", UserId.ToString()));
        using var externalResponse = await SendAsync(client, HttpMethod.Get, "/api/authorizations",
            new JwtSecurityTokenHandler().WriteToken(external));
        Assert.Equal(HttpStatusCode.Unauthorized, local.StatusCode);
        Assert.Equal(HttpStatusCode.OK, externalResponse.StatusCode);
    }

    private async Task<WebApplication> StartApiAsync(string environment = "Development",
        Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        _tokens.Configure(builder);
        configure?.Invoke(builder);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthorizationsController).Assembly);
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure("Server=localhost;Database=HealthAuthorizationModelOnly;Integrated Security=true;Encrypt=true");
        builder.Services.ConfigureDbContext<HealthAuthorizationDbContext>(
            options => options.AddInterceptors(new SuppressDatabaseWrite()));
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IAuthorizationNotificationService, AuthorizationNotificationService>();
        builder.Services.AddApiOpenApi();
        builder.Services.AddSingleton<IRequestHandler<GetAuthorizationsQuery, Page<AuthorizationSummaryDto>>, AuthorizationListStub>();
        builder.Services.AddSingleton<IRequestHandler<ChangeAuthorizationStatusCommand>, ChangeAuthorizationStatusStub>();
        builder.Services.AddSingleton<IRequestHandler<UpdateAuthorizationCommand>, UpdateAuthorizationStub>();
        builder.AddApiSecurity();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapOpenApi();
        app.MapHub<AuthorizationHub>(AuthorizationHub.Route);
        app.MapGet("/tests/audit", AuditAsync).RequireAuthorization(SecurityConfiguration.BusinessAccess);
        await app.StartAsync();
        return app;
    }

    private static async Task<Guid?[]> AuditAsync(HealthAuthorizationDbContext context, CancellationToken cancellationToken)
    {
        var affiliate = new Affiliate { AffiliateNumber = "TEST" };
        context.Affiliates.Add(affiliate);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.AcceptAllChanges();
        affiliate.FirstName = "Prueba";
        await context.SaveChangesAsync(cancellationToken);
        return new Guid?[] { affiliate.CreatedBy, affiliate.LastUpdatedBy };
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }

    private class AuthorizationListStub : IRequestHandler<GetAuthorizationsQuery, Page<AuthorizationSummaryDto>>
    {
        public Task<Page<AuthorizationSummaryDto>> Handle(GetAuthorizationsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Page<AuthorizationSummaryDto>(
                Array.Empty<AuthorizationSummaryDto>(), request.PageNumber, request.PageSize, 0));
        }
    }

    private class SuppressDatabaseWrite : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }

    private class ChangeAuthorizationStatusStub : IRequestHandler<ChangeAuthorizationStatusCommand>
    {
        public Task Handle(ChangeAuthorizationStatusCommand request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class UpdateAuthorizationStub : IRequestHandler<UpdateAuthorizationCommand>
    {
        public Task Handle(UpdateAuthorizationCommand request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
