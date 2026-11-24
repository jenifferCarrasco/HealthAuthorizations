using HealthAuthorization.Application.Common.Models;
using HealthAuthorization.Application.Features.Affiliates.Queries;
using HealthAuthorization.Application.Features.Affiliates.Validators;
using HealthAuthorization.Application.Features.Authorizations.Queries;
using HealthAuthorization.Application.Features.Authorizations.Validators;
using HealthAuthorization.Application.Features.Contractors.Queries;
using HealthAuthorization.Application.Features.Intermediaries.Queries;
using HealthAuthorization.Application.Features.Providers.Queries;
using HealthAuthorization.Application.Features.ProviderTypes.Queries;
using HealthAuthorization.Application.Features.ServiceTypes.Queries;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.UnitTests.Application;

public class PaginatedQueriesTests
{
    [Fact]
    public void AllQueriesUseTheSharedPaginationDefaults()
    {
        var queries = new PaginationFilter[]
        {
            new GetAffiliatesQuery(), new GetAuthorizationsQuery(), new GetContractorsQuery(),
            new GetIntermediariesQuery(), new GetProvidersQuery(), new GetProviderTypesQuery(),
            new GetServiceTypesQuery()
        };

        Assert.All(queries, query =>
        {
            Assert.Equal(1, query.PageNumber);
            Assert.Equal(20, query.PageSize);
        });
        Assert.All(queries.OfType<SearchablePaginationFilter>(), query => Assert.Null(query.Search));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 20)]
    public void RejectsInvalidPagination(int pageNumber, int pageSize)
    {
        var query = new GetAffiliatesQuery { PageNumber = pageNumber, PageSize = pageSize };
        Assert.False(new GetAffiliatesQueryValidator().Validate(query).IsValid);
    }

    [Fact]
    public async Task SearchesAffiliateFieldsBeforeCountingAndPaging()
    {
        var byNumber = new Affiliate { AffiliateNumber = "AB-1" };
        var byFirstName = new Affiliate { AffiliateNumber = "CD-1", FirstName = "ABEL" };
        var byLastName = new Affiliate { AffiliateNumber = "EF-1", LastName = "ABAD" };
        var byIdentification = new Affiliate { AffiliateNumber = "GH-1", Identification = "AB-2" };
        var inactive = new Affiliate { AffiliateNumber = "AB-3", IsActive = false };
        var deleted = new Affiliate { AffiliateNumber = "AB-4", IsDelete = true };
        var other = new Affiliate { AffiliateNumber = "IJ-1" };
        var repository = new MemoryRepository<Affiliate>(
            byIdentification, byFirstName, deleted, byLastName, other, inactive, byNumber);
        var handler = new GetAffiliatesQueryHandler(repository);
        var query = new GetAffiliatesQuery { Search = " AB ", IsActive = true, PageSize = 2 };

        var first = await handler.Handle(query, CancellationToken.None);
        query.PageNumber = 2;
        var second = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(4, first.TotalCount);
        Assert.Equal(4, second.TotalCount);
        Assert.Equal(new[] { byNumber.Id, byFirstName.Id }, first.Items.Select(item => item.Id));
        Assert.Equal(new[] { byLastName.Id, byIdentification.Id }, second.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task ProvidersCombineSearchWithTypeAndStatus()
    {
        var typeId = Guid.NewGuid();
        var match = new Provider { Identification = "P-100", ProviderTypeId = typeId, IsActive = false };
        var wrongType = new Provider { Name = "P- clínica", ProviderTypeId = Guid.NewGuid(), IsActive = false };
        var wrongStatus = new Provider { Identification = "P-200", ProviderTypeId = typeId };
        var handler = new GetProvidersQueryHandler(new MemoryRepository<Provider>(wrongType, match, wrongStatus));
        var query = new GetProvidersQuery { Search = "P-", ProviderTypeId = typeId, IsActive = false };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(match.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task AuthorizationsKeepFlatFiltersAndExclusiveEndDate()
    {
        var from = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var until = from.AddDays(1);
        var match = new Authorization
        {
            AffiliateId = Guid.NewGuid(), ContractorId = Guid.NewGuid(), IntermediaryId = Guid.NewGuid(),
            IsActive = false, CreatedDate = from
        };
        var outside = new Authorization
        {
            AffiliateId = match.AffiliateId, ContractorId = match.ContractorId,
            IntermediaryId = match.IntermediaryId, IsActive = false, CreatedDate = until
        };
        var wrongAffiliate = new Authorization { AffiliateId = Guid.NewGuid(), IsActive = false, CreatedDate = from };
        var handler = new GetAuthorizationsQueryHandler(new MemoryRepository<Authorization>(outside, wrongAffiliate, match));
        var query = new GetAuthorizationsQuery
        {
            AffiliateId = match.AffiliateId, ContractorId = match.ContractorId, IntermediaryId = match.IntermediaryId,
            IsActive = false, CreatedFrom = from, CreatedBefore = until
        };

        Assert.True(new GetAuthorizationsQueryValidator().Validate(query).IsValid);
        var result = await handler.Handle(query, CancellationToken.None);
        Assert.Equal(match.Id, Assert.Single(result.Items).Id);

        query.CreatedBefore = from;
        Assert.False(new GetAuthorizationsQueryValidator().Validate(query).IsValid);
    }
}
