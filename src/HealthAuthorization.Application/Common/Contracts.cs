namespace HealthAuthorization.Application.Common;

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount);

public static class InputLimits
{
    public const int Code = 50;
    public const int Name = 150;
    public const int Description = 500;
    public const decimal MaxClaimedAmount = 99999999999999999.99m;
    public const int MaxPageSize = 100;

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize is < 1 or > MaxPageSize ||
            (long)(pageNumber - 1) * pageSize > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "La paginación solicitada no es válida.");
        }
    }
}

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
