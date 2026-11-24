namespace HealthAuthorization.Application.Common.Models;

public class SearchablePaginationFilter : PaginationFilter
{
    public string? Search { get; set; }
}
