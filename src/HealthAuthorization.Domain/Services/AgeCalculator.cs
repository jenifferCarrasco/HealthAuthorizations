namespace HealthAuthorization.Domain.Services;

public static class AgeCalculator
{
    public static bool IsMinor(DateOnly birthDate, DateOnly referenceDate)
    {
        if (birthDate > referenceDate)
        {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "La fecha de nacimiento no puede ser futura.");
        }

        var years = referenceDate.Year - birthDate.Year;
        return years < 18 || (years == 18 && referenceDate < birthDate.AddYears(18));
    }
}
