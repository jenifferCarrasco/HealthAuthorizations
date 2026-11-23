using HealthAuthorization.Domain.Services;

namespace HealthAuthorization.UnitTests.Domain.Services;

public sealed class AgeCalculatorTests
{
    [Fact]
    public void DeterminesMinorityAtTheEighteenthBirthday()
    {
        var birthDate = new DateOnly(2008, 11, 23);
        var eighteenthBirthday = new DateOnly(2026, 11, 23);

        Assert.True(AgeCalculator.IsMinor(birthDate, eighteenthBirthday.AddDays(-1)));
        Assert.False(AgeCalculator.IsMinor(birthDate, eighteenthBirthday));
        Assert.False(AgeCalculator.IsMinor(birthDate, eighteenthBirthday.AddDays(1)));
    }

    [Fact]
    public void RejectsCalculatingAgeBeforeBirth()
    {
        var referenceDate = new DateOnly(2026, 11, 23);

        Assert.Throws<ArgumentOutOfRangeException>("birthDate",
            () => AgeCalculator.IsMinor(referenceDate.AddDays(1), referenceDate));
    }
}
