namespace HealthAuthorization.Infrastructure.Persistence.Configurations;

internal static class CatalogSeed
{
    internal static readonly DateTimeOffset CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    internal static readonly Guid ContractorId = Id(201);
    internal static readonly Guid IntermediaryId = Id(202);
    internal static readonly Guid AffiliateId = Id(203);

    internal static readonly (int Number, string Name)[] ProviderTypes =
    {
        (1, "Médico"), (2, "Farmacia"), (3, "Clínica"), (4, "Laboratorio")
    };

    internal static readonly (int Number, string Name)[] ServiceTypes =
    {
        (11, "Consulta"), (12, "Emergencia"), (13, "Hospitalización"), (14, "Laboratorio"), (15, "Imágenes")
    };

    internal static Guid Id(int number)
    {
        return new Guid(number, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
