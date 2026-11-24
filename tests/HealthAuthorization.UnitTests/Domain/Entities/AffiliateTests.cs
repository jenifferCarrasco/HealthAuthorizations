using HealthAuthorization.Domain.Entities;
using HealthAuthorization.Domain.Enums;

namespace HealthAuthorization.UnitTests.Domain.Entities;

public sealed class AffiliateTests
{
    [Fact]
    public void GeneratesDistinctNonEmptyIdentifiers()
    {
        var first = new Affiliate();
        var second = new Affiliate();

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void DefaultsToNoneWhenAnAffiliateHasNoIdentification()
    {
        var affiliate = new Affiliate
        {
            AffiliateNumber = "AF-123",
            FirstName = "Ana",
            LastName = "Pérez",
            BirthDate = new DateOnly(2012, 11, 23)
        };

        Assert.Null(affiliate.Identification);
        Assert.Equal(DocumentType.None, affiliate.DocumentType);
    }

    [Fact]
    public void PreservesTheDocumentTypeCodesIncludingNone()
    {
        Assert.Equal(
            [(DocumentType.None, 0), (DocumentType.Cedula, 1), (DocumentType.Passport, 2), (DocumentType.RNC, 3)],
            Enum.GetValues<DocumentType>().Select(type => (type, (int)type)));
    }
}
