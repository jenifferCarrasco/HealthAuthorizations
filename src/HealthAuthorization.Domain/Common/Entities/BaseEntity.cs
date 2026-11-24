namespace HealthAuthorization.Domain.Common.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
}
