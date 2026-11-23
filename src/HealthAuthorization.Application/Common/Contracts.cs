namespace HealthAuthorization.Application.Common;

public class Page<T>
{
    public IReadOnlyList<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }

    public Page(IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount)
    {
        this.Items = Items;
        this.PageNumber = PageNumber;
        this.PageSize = PageSize;
        this.TotalCount = TotalCount;
    }
}

public static class InputLimits
{
    public const int Code = 50;
    public const int Name = 150;
    public const int Description = 500;
    public const decimal MaxClaimedAmount = 99999999999999999.99m;
    public const int MaxPageSize = 100;

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
