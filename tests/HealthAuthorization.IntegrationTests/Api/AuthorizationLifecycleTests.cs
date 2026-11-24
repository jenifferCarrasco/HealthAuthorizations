using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HealthAuthorization.Application.Abstractions;
using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Authorizations;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.UnitTests.Application;
using Microsoft.Extensions.DependencyInjection;
using Authorization = HealthAuthorization.Domain.Entities.Authorization;

namespace HealthAuthorization.IntegrationTests.Api;

public class AuthorizationLifecycleTests
{
    [Fact]
    public async Task UpdateKeepsHistoryAndReplayUsesTheOriginalRequestWithinEachUserScope()
    {
        var affiliate = new Affiliate { AffiliateNumber = "A-001" };
        var provider = new Provider { Name = "Clínica" };
        var consultation = new ServiceType { Name = "Consulta" };
        var laboratory = new ServiceType { Name = "Laboratorio" };
        var authorizations = new MemoryRepository<Authorization>();
        var unit = new UnitOfWorkStub();
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await BusinessApiTestHost.StartAsync(tokens, unit, new ProviderType(), authorizations, services =>
        {
            services.AddSingleton<IRepositoryAsync<Affiliate>>(new MemoryRepository<Affiliate>(affiliate));
            services.AddSingleton<IRepositoryAsync<Provider>>(new MemoryRepository<Provider>(provider));
            services.AddSingleton<IRepositoryAsync<ServiceType>>(new MemoryRepository<ServiceType>(consultation, laboratory));
        });

        var url = new Uri(app.Urls.Single());
        var userId = Guid.NewGuid();
        using var operatorClient = CreateClient(url, tokens, "Operator", userId);
        using var administrator = CreateClient(url, tokens, "Administrator", Guid.NewGuid());
        const string path = "/api/authorizations";
        var key = Guid.NewGuid();
        var original = new AuthorizationInput(affiliate.Id,
        [
            new AuthorizationServiceInput(provider.Id, consultation.Id, 100.05m),
            new AuthorizationServiceInput(provider.Id, laboratory.Id, 50m)
        ]);

        using var invalidReference = await CreateAsync(operatorClient, key, new AuthorizationInput(affiliate.Id,
            [new AuthorizationServiceInput(provider.Id, Guid.NewGuid(), 10m)]));
        Assert.Equal(HttpStatusCode.NotFound, invalidReference.StatusCode);
        Assert.Empty(authorizations.Items);
        Assert.Equal(0, unit.SaveCount);

        using var created = await CreateAsync(operatorClient, key, original);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location!;
        var before = await operatorClient.GetFromJsonAsync<AuthorizationDto>(location);
        Assert.NotNull(before);
        Assert.Equal(150.05m, before.Summary.TotalClaimedAmount);
        Assert.Equal(105.04m, before.Summary.TotalArsAmount);
        Assert.Equal(45.01m, before.Summary.TotalAffiliateAmount);
        var detail = Assert.Single(before.Details, entry => entry.ServiceTypeId == consultation.Id);
        var update = new AuthorizationInput(affiliate.Id,
            [new AuthorizationServiceInput(provider.Id, consultation.Id, 200m, detail.Id)]);

        using var updated = await operatorClient.PutAsJsonAsync(location, update);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var after = await operatorClient.GetFromJsonAsync<AuthorizationDto>(location);
        Assert.NotNull(after);
        Assert.Equal(detail.Id, Assert.Single(after.Details).Id);
        Assert.Equal(200m, after.Summary.TotalClaimedAmount);
        Assert.Equal(140m, after.Summary.TotalArsAmount);
        Assert.Equal(60m, after.Summary.TotalAffiliateAmount);
        var saved = Assert.Single(authorizations.Items);
        Assert.Equal(userId, saved.IdempotencyScopeId);
        var removed = Assert.Single(saved.Details, entry => entry.IsDelete);
        Assert.Equal(laboratory.Id, removed.ServiceTypeId);
        Assert.Equal(50m, removed.ClaimedAmount);

        using var replay = await CreateAsync(operatorClient, key, original);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.Equal(location, replay.Headers.Location);
        using var conflict = await CreateAsync(operatorClient, key, new AuthorizationInput(affiliate.Id,
            [new AuthorizationServiceInput(provider.Id, consultation.Id, 200m)]));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(2, unit.SaveCount);

        var statusPath = $"{path}/{saved.Id}/status";
        using var denied = await operatorClient.PatchAsJsonAsync(statusPath, new { isActive = false });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var disabled = await administrator.PatchAsJsonAsync(statusPath, new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, disabled.StatusCode);
        using var disabledUpdate = await operatorClient.PutAsJsonAsync(location, update);
        Assert.Equal(HttpStatusCode.Conflict, disabledUpdate.StatusCode);

        using var otherUser = await CreateAsync(administrator, key, original);
        Assert.Equal(HttpStatusCode.Created, otherUser.StatusCode);
        Assert.NotEqual(location, otherUser.Headers.Location);

        var list = await operatorClient.GetFromJsonAsync<Page<AuthorizationSummaryDto>>(
            $"{path}?affiliateId={affiliate.Id}&isActive=false&pageSize=1");
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal(saved.Id, Assert.Single(list.Items).Id);
        using var missing = await operatorClient.GetAsync($"{path}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(4, unit.SaveCount);
    }

    private static HttpClient CreateClient(Uri url, DevelopmentJwtTestTokens tokens, string role, Guid userId)
    {
        var client = new HttpClient { BaseAddress = url };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Create(role, userId.ToString()));
        return client;
    }

    private static async Task<HttpResponseMessage> CreateAsync(HttpClient client, Guid key, AuthorizationInput body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/authorizations");
        request.Headers.Add("Idempotency-Key", key.ToString());
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}
