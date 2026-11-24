namespace HealthAuthorization.Domain.Common.Interfaces;

public interface IAuditable
{
    DateTimeOffset CreatedDate { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? LastUpdatedDate { get; set; }
    Guid? LastUpdatedBy { get; set; }
}
