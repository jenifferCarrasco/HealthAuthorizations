using HealthAuthorization.Domain.Policies;

namespace HealthAuthorization.UnitTests.Domain.Policies;

public sealed class CoveragePolicyTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 0)]
    [InlineData(2, 1, 1)]
    [InlineData(5, 4, 1)]
    [InlineData(15, 11, 4)]
    [InlineData(10000, 7000, 3000)]
    public void RoundsArsShareAndAssignsExactRemainderToAffiliate(
        int claimedCents, int arsCents, int affiliateCents)
    {
        var claimedAmount = claimedCents / 100m;
        var (arsAmount, affiliateAmount) = CoveragePolicy.Calculate(claimedAmount);

        Assert.Equal(arsCents / 100m, arsAmount);
        Assert.Equal(affiliateCents / 100m, affiliateAmount);
        Assert.Equal(claimedAmount, arsAmount + affiliateAmount);
    }

    [Fact]
    public void RejectsNegativeAmounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>("claimedAmount", () => CoveragePolicy.Calculate(-0.01m));
    }

    [Fact]
    public void RejectsAmountsWithFractionalCents()
    {
        Assert.Throws<ArgumentException>("claimedAmount", () => CoveragePolicy.Calculate(1.001m));
    }

    [Fact]
    public void RejectsDistributionsThatCannotRepresentTheRoundedShares()
    {
        Assert.Throws<OverflowException>(() => CoveragePolicy.Calculate(decimal.MaxValue));
    }

    [Fact]
    public void PreservesMidpointRoundingForLargeRepresentableAmounts()
    {
        const decimal claimed = 100000000000000000000000000.05m;

        var (arsAmount, affiliateAmount) = CoveragePolicy.Calculate(claimed);

        Assert.Equal(70000000000000000000000000.04m, arsAmount);
        Assert.Equal(30000000000000000000000000.01m, affiliateAmount);
    }
}
