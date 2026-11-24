using HealthAuthorization.Domain.Entities;

namespace HealthAuthorization.UnitTests.Domain.Entities;

public sealed class AuthorizationTests
{
    [Fact]
    public void NewAuthorizationsStartActiveWithIndependentDetails()
    {
        var first = new Authorization();
        var second = new Authorization();

        first.Details.Add(new AuthorizationDetail());

        Assert.True(first.IsActive);
        Assert.Single(first.Details);
        Assert.Empty(second.Details);
    }

    [Fact]
    public void DerivesTotalsFromTheCurrentDetails()
    {
        var first = new AuthorizationDetail { ClaimedAmount = 100m, ArsAmount = 70m, AffiliateAmount = 30m };
        var second = new AuthorizationDetail { ClaimedAmount = 50m, ArsAmount = 35m, AffiliateAmount = 15m };
        var authorization = new Authorization { Details = { first } };

        AssertTotals(authorization, 100m, 70m, 30m);

        authorization.Details.Add(second);
        AssertTotals(authorization, 150m, 105m, 45m);

        first.ClaimedAmount = 200m;
        first.ArsAmount = 140m;
        first.AffiliateAmount = 60m;
        AssertTotals(authorization, 250m, 175m, 75m);

        authorization.Details.Remove(first);
        AssertTotals(authorization, 50m, 35m, 15m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void RejectsTotalsThatOverflowOrLoseCents(int cents)
    {
        var authorization = new Authorization
        {
            Details =
            {
                new AuthorizationDetail { ClaimedAmount = decimal.MaxValue },
                new AuthorizationDetail { ClaimedAmount = cents / 100m }
            }
        };

        Assert.Throws<OverflowException>(() => authorization.TotalClaimedAmount);
    }

    private static void AssertTotals(Authorization authorization, decimal claimed, decimal ars, decimal affiliate)
    {
        Assert.Equal(claimed, authorization.TotalClaimedAmount);
        Assert.Equal(ars, authorization.TotalArsAmount);
        Assert.Equal(affiliate, authorization.TotalAffiliateAmount);
    }
}
