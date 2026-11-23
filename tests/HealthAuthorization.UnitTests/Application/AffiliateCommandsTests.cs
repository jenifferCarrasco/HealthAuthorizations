using HealthAuthorization.Application.Common;
using HealthAuthorization.Application.Features.Affiliates;
using HealthAuthorization.Application.Features.Affiliates.Commands;
using HealthAuthorization.Application.Features.Affiliates.Validators;
using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.UnitTests.Application;

public sealed class AffiliateCommandsTests
{
    private static readonly AffiliateInput ValidInput = new("AF-123", "Ana", "Pérez", new DateOnly(2010, 1, 1));

    [Fact]
    public async Task CreatesAffiliateAndNormalizesIdentification()
    {
        var contractor = new Contractor();
        var affiliates = new AffiliateRepositoryStub();
        var unit = new UnitOfWorkStub();
        var handler = new CreateAffiliateCommandHandler(affiliates, new MemoryRepository<Contractor>(contractor), unit);
        var command = new CreateAffiliateCommand(ValidInput with
        {
            Identification = "  001-1234567-8  ", DocumentType = DocumentType.Cedula, ContractorId = contractor.Id
        });

        Assert.True(new CreateAffiliateCommandValidator(TimeProvider.System).Validate(command).IsValid);
        var id = await handler.Handle(command, TestContextToken);
        var saved = Assert.Single(affiliates.Items);
        Assert.Equal(saved.Id, id);
        Assert.Equal("001-1234567-8", saved.Identification);
        Assert.Equal(contractor.Id, saved.ContractorId);
        Assert.Equal(1, unit.SaveCount);
    }

    [Fact]
    public void RejectsEmptyNumbersAndFutureBirthDates()
    {
        var command = new CreateAffiliateCommand(ValidInput with
        {
            AffiliateNumber = " ", BirthDate = new DateOnly(2026, 11, 24)
        });
        var validator = new CreateAffiliateCommandValidator(new FixedClock());
        var errors = validator.Validate(command).Errors;

        Assert.Contains(errors, error => error.PropertyName.EndsWith(nameof(AffiliateInput.AffiliateNumber)));
        Assert.Contains(errors, error => error.PropertyName.EndsWith(nameof(AffiliateInput.BirthDate)));
    }

    [Theory]
    [InlineData(null, DocumentType.None, true)]
    [InlineData(" ", DocumentType.None, true)]
    [InlineData(null, DocumentType.Cedula, false)]
    [InlineData("001", DocumentType.None, false)]
    [InlineData("001", DocumentType.Passport, true)]
    [InlineData("001", (DocumentType)9, false)]
    public void ValidatesIdentificationAndTypeTogether(string? identification, DocumentType documentType, bool isValid)
    {
        var command = new CreateAffiliateCommand(ValidInput with { Identification = identification, DocumentType = documentType });
        Assert.Equal(isValid, new CreateAffiliateCommandValidator(new FixedClock()).Validate(command).IsValid);
    }

    [Fact]
    public async Task RejectsDuplicatesBeforeAddingOrSaving()
    {
        var existing = new Affiliate { AffiliateNumber = "AF-123" };
        var affiliates = new AffiliateRepositoryStub(existing);
        var unit = new UnitOfWorkStub();
        var handler = new CreateAffiliateCommandHandler(affiliates, new MemoryRepository<Contractor>(), unit);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new CreateAffiliateCommand(ValidInput), TestContextToken));
        Assert.Same(existing, Assert.Single(affiliates.Items));
        Assert.Equal(0, unit.SaveCount);
    }

    [Fact]
    public async Task UpdateCanKeepItsOwnUniqueValues()
    {
        var existing = new Affiliate { AffiliateNumber = "AF-123", Identification = "001" };
        var affiliates = new AffiliateRepositoryStub(existing);
        var unit = new UnitOfWorkStub();
        var handler = new UpdateAffiliateCommandHandler(affiliates, new MemoryRepository<Contractor>(), unit);
        var input = ValidInput with { Identification = "001", DocumentType = DocumentType.Cedula };

        await handler.Handle(new UpdateAffiliateCommand(existing.Id, input), TestContextToken);

        Assert.Equal("Ana", existing.FirstName);
        Assert.Equal(1, affiliates.Updates);
        Assert.Equal(1, unit.SaveCount);
    }

    private static CancellationToken TestContextToken => CancellationToken.None;

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 23, 0, 0, 0, TimeSpan.Zero);
    }
}
