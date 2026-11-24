using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.UnitTests.Application;

namespace HealthAuthorization.IntegrationTests.Api;

public class CatalogLifecycleTests
{
    [Theory]
    [InlineData("affiliates", "firstName")]
    [InlineData("contractors", "name")]
    [InlineData("intermediaries", "name")]
    [InlineData("providers", "name")]
    [InlineData("provider-types", "name")]
    [InlineData("service-types", "name")]
    public async Task CatalogLifecycleSupportsNormalizationFilteringAndLogicalDeletion(string catalog, string nameProperty)
    {
        var providerType = new ProviderType { Name = "Clínica" };
        var unit = new UnitOfWorkStub();
        var tokens = new DevelopmentJwtTestTokens();
        await using var app = await BusinessApiTestHost.StartAsync(tokens, unit, providerType);

        using var administrator = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        administrator.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", tokens.Create("Administrator", Guid.NewGuid().ToString()));
        var path = $"/api/{catalog}";
        var original = CreateInput(catalog, "  Prueba Alfa  ", "101", providerType.Id);
        var updated = CreateInput(catalog, "  Prueba Beta  ", "101", providerType.Id);

        using var invalid = await administrator.PostAsJsonAsync(path, CreateInput(catalog, " ", "101", providerType.Id));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(0, unit.SaveCount);

        using var created = await administrator.PostAsJsonAsync(path, original);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal($"{path}/{id}", created.Headers.Location?.AbsolutePath);
        var itemPath = created.Headers.Location!;

        var item = await administrator.GetFromJsonAsync<JsonElement>(itemPath);
        Assert.Equal("Prueba Alfa", item.GetProperty(nameProperty).GetString());
        Assert.True(item.GetProperty("isActive").GetBoolean());

        if (catalog == "affiliates")
        {
            using var duplicate = await administrator.PostAsJsonAsync(path, original);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal(1, unit.SaveCount);
        }

        using var update = await administrator.PutAsJsonAsync(itemPath, updated);
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var current = await administrator.GetFromJsonAsync<JsonElement>(itemPath);
        Assert.Equal(id, current.GetProperty("id").GetGuid());
        Assert.Equal("Prueba Beta", current.GetProperty(nameProperty).GetString());

        using var other = await administrator.PostAsJsonAsync(path, CreateInput(catalog, "Prueba Gamma", "102", providerType.Id));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
        var otherId = (await other.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var filter = $"{path}?search=%20Prueba%20&isActive=true&pageSize=1";
        await AssertPageAsync(administrator, $"{filter}&pageNumber=1", 2, id);
        await AssertPageAsync(administrator, $"{filter}&pageNumber=2", 2, otherId);

        var statusPath = $"{path}/{id}/status";
        using var deactivate = await administrator.PatchAsJsonAsync(statusPath, new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        await AssertPageAsync(administrator, $"{path}?search=Prueba&isActive=false", 1, id);

        using var repeat = await administrator.PatchAsJsonAsync(statusPath, new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, repeat.StatusCode);
        Assert.Equal(4, unit.SaveCount);

        using var activate = await administrator.PatchAsJsonAsync(statusPath, new { isActive = true });
        Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
        await AssertPageAsync(administrator, $"{path}?search=Prueba&isActive=false", 0);

        using var delete = await administrator.DeleteAsync(itemPath);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        await AssertPageAsync(administrator, $"{path}?search=Prueba", 1, otherId);
        using var readDeleted = await administrator.GetAsync(itemPath);
        using var updateDeleted = await administrator.PutAsJsonAsync(itemPath, updated);
        using var deleteAgain = await administrator.DeleteAsync(itemPath);
        using var enableDeleted = await administrator.PatchAsJsonAsync(statusPath, new { isActive = true });
        Assert.Equal(HttpStatusCode.NotFound, readDeleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateDeleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, enableDeleted.StatusCode);
        Assert.Equal(6, unit.SaveCount);
    }

    private static object CreateInput(string catalog, string name, string number, Guid providerTypeId)
    {
        return catalog switch
        {
            "affiliates" => new { affiliateNumber = number, firstName = name, lastName = "Pérez", birthDate = "2010-01-01" },
            "providers" => new { name, identification = number, providerTypeId },
            "contractors" or "intermediaries" => new { name, identification = number },
            _ => new { name, description = "  Descripción  " }
        };
    }

    private static async Task AssertPageAsync(HttpClient client, string url, int total, Guid? expectedId = null)
    {
        var page = await client.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(total, page.GetProperty("totalCount").GetInt32());
        var items = page.GetProperty("items").EnumerateArray();
        if (expectedId is null)
        {
            Assert.Empty(items);
            return;
        }

        Assert.Equal(expectedId.Value, Assert.Single(items).GetProperty("id").GetGuid());
    }
}
