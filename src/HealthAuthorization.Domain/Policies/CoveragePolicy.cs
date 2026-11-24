namespace HealthAuthorization.Domain.Policies;

public static class CoveragePolicy
{
    private const decimal ArsShare = 0.70m;
    private const int DecimalPlaces = 2;

    public static (decimal ArsAmount, decimal AffiliateAmount) Calculate(decimal claimedAmount)
    {
        if (claimedAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(claimedAmount), "El monto reclamado no puede ser negativo.");
        }

        if (decimal.Round(claimedAmount, DecimalPlaces) != claimedAmount)
        {
            throw new ArgumentException("El monto reclamado debe tener como máximo dos decimales.", nameof(claimedAmount));
        }

        // Los bloques de diez dan una parte entera. Así la multiplicación del
        // resto conserva los decimales requeridos antes de redondear.
        var remainder = claimedAmount % 10m;
        var wholeBlocksShare = (claimedAmount - remainder) / 10m * (10m * ArsShare);
        var remainderShare = decimal.Round(remainder * ArsShare, DecimalPlaces, MidpointRounding.AwayFromZero);
        var arsAmount = AddExact(wholeBlocksShare, remainderShare);
        var affiliateAmount = claimedAmount - arsAmount;
        if (AddExact(arsAmount, affiliateAmount) != claimedAmount)
        {
            throw new OverflowException("La distribución del monto reclamado pierde precisión.");
        }

        return (arsAmount, affiliateAmount);
    }

    internal static decimal AddExact(decimal left, decimal right)
    {
        var total = left + right;
        if (total - left != right || total - right != left)
        {
            throw new OverflowException("El importe excede la precisión que permite decimal.");
        }

        return total;
    }
}
