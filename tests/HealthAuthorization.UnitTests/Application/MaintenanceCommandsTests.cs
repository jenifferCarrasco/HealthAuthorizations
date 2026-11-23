using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates.Commands;
using HealthAuthorization.Application.Features.Affiliates.Queries;
using HealthAuthorization.Application.Features.Affiliates.Validators;
using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.UnitTests.Application;

public sealed class MaintenanceCommandsTests
{
    [Fact]
    public async Task StatusAndDeletionAreIndependentAndDeletedRowsStayHidden()
    {
        var affiliate = new Affiliate { AffiliateNumber = "AF-1" };
        var repository = new MemoryRepository<Affiliate>(affiliate);
        var unit = new UnitOfWorkStub();
        var changeStatus = new ChangeAffiliateStatusCommandHandler(repository, unit);
        var delete = new DeleteAffiliateCommandHandler(repository, unit);
        var list = new GetAffiliatesQueryHandler(repository);
        var byId = new GetAffiliateByIdQueryHandler(repository);
        var token = CancellationToken.None;

        await changeStatus.Handle(new ChangeAffiliateStatusCommand(affiliate.Id, false), token);
        Assert.False(affiliate.IsActive);
        Assert.False(affiliate.IsDelete);
        Assert.Equal(1L, (await list.Handle(new GetAffiliatesQuery { IsActive = false }, token)).TotalCount);
        Assert.Equal(0L, (await list.Handle(new GetAffiliatesQuery { IsActive = true }, token)).TotalCount);

        await changeStatus.Handle(new ChangeAffiliateStatusCommand(affiliate.Id, true), token);
        await delete.Handle(new DeleteAffiliateCommand(affiliate.Id), token);
        Assert.True(affiliate.IsActive);
        Assert.True(affiliate.IsDelete);
        Assert.Same(affiliate, Assert.Single(repository.Items));
        Assert.Equal(0L, (await list.Handle(new GetAffiliatesQuery(), token)).TotalCount);
        await Assert.ThrowsAsync<NotFoundException>(() => byId.Handle(new GetAffiliateByIdQuery(affiliate.Id), token));
        await Assert.ThrowsAsync<NotFoundException>(() => changeStatus.Handle(
            new ChangeAffiliateStatusCommand(affiliate.Id, false), token));
        Assert.Equal(3, unit.SaveCount);
    }

    [Fact]
    public void RejectsEmptyIdentifierForStateAndDeletion()
    {
        Assert.False(new ChangeAffiliateStatusCommandValidator()
            .Validate(new ChangeAffiliateStatusCommand(Guid.Empty, false)).IsValid);
        Assert.False(new DeleteAffiliateCommandValidator()
            .Validate(new DeleteAffiliateCommand(Guid.Empty)).IsValid);
    }
}
