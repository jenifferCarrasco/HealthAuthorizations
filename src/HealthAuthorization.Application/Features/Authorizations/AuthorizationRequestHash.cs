using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HealthAuthorization.Application.Features.Authorizations;

internal static class AuthorizationRequestHash
{
    internal static byte[] Calculate(AuthorizationInput input)
    {
        // La versión fija el formato que se compara con las huellas ya guardadas.
        var data = new StringBuilder("authorization-create:v1\n");
        // El formato D normaliza los GUID con guiones y conserva los campos opcionales vacíos.
        data.Append(input.AffiliateId.ToString("D")).Append('\n');
        data.Append(input.ContractorId?.ToString("D")).Append('\n');
        data.Append(input.IntermediaryId?.ToString("D")).Append('\n');

        // El orden de entrada es irrelevante, pero cada repetición se conserva.
        var services = input.Services.OrderBy(service => service.ProviderId)
            .ThenBy(service => service.ServiceTypeId)
            .ThenBy(service => service.ClaimedAmount);

        foreach (var service in services)
        {
            data.Append(service.ProviderId.ToString("D")).Append('|');
            data.Append(service.ServiceTypeId.ToString("D")).Append('|');
            // G29 elimina ceros finales sin redondear. InvariantCulture fija el separador decimal.
            data.Append(service.ClaimedAmount.ToString("G29", CultureInfo.InvariantCulture)).Append('\n');
        }

        // SHA-256 produce los 32 bytes almacenados en SQL Server a partir del texto UTF-8.
        return SHA256.HashData(Encoding.UTF8.GetBytes(data.ToString()));
    }
}
