using System.Globalization;
using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Application.Features.Authorizations.Queries;
using HealthAuthorization.Application.Features.Providers.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace HealthAuthorization.IntegrationTests.Api;

public class PaginationModelBindingTests
{
    [Fact]
    public async Task BindsInheritedAndProviderSpecificPropertiesFromQueryString()
    {
        var typeId = Guid.NewGuid();
        var query = await BindAsync<GetProvidersQuery>(
            $"?search=Clinica&pageNumber=2&pageSize=7&isActive=false&providerTypeId={typeId}");

        Assert.Equal("Clinica", query.Search);
        Assert.Equal(2, query.PageNumber);
        Assert.Equal(7, query.PageSize);
        Assert.False(query.IsActive);
        Assert.Equal(typeId, query.ProviderTypeId);
    }

    [Fact]
    public async Task BindsFlatAuthorizationFiltersAndPreservesPaginationDefaults()
    {
        var affiliateId = Guid.NewGuid();
        var contractorId = Guid.NewGuid();
        var intermediaryId = Guid.NewGuid();
        var query = await BindAsync<GetAuthorizationsQuery>(
            $"?affiliateId={affiliateId}&contractorId={contractorId}&intermediaryId={intermediaryId}" +
            "&isActive=true&createdFrom=2026-11-01T00%3A00%3A00Z&createdBefore=2026-11-02T00%3A00%3A00Z");

        Assert.Equal(affiliateId, query.AffiliateId);
        Assert.Equal(contractorId, query.ContractorId);
        Assert.Equal(intermediaryId, query.IntermediaryId);
        Assert.True(query.IsActive);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), query.CreatedFrom);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero), query.CreatedBefore);
        Assert.Equal(1, query.PageNumber);
        Assert.Equal(20, query.PageSize);
    }

    private static async Task<TQuery> BindAsync<TQuery>(string queryString) where TQuery : PaginationFilter
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var metadata = provider.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(TQuery));
        var bindingInfo = new BindingInfo { BindingSource = BindingSource.Query };
        var binder = provider.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata,
            BindingInfo = bindingInfo
        });
        var query = new QueryCollection(QueryHelpers.ParseQuery(queryString));
        var values = new QueryStringValueProvider(BindingSource.Query, query, CultureInfo.InvariantCulture);
        var context = DefaultModelBindingContext.CreateBindingContext(actionContext, values, metadata, bindingInfo, "");

        await binder.BindModelAsync(context);

        Assert.Empty(context.ModelState.Values.SelectMany(entry => entry.Errors));
        return Assert.IsType<TQuery>(context.Result.Model);
    }
}
