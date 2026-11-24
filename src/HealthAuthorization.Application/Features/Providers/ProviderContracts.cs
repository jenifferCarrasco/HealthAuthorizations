namespace HealthAuthorization.Application.Features.Providers;

public class ProviderInput
{
    public string Name { get; set; }
    public string Identification { get; set; }
    public Guid ProviderTypeId { get; set; }

    public ProviderInput(string Name, string Identification, Guid ProviderTypeId)
    {
        this.Name = Name;
        this.Identification = Identification;
        this.ProviderTypeId = ProviderTypeId;
    }
}

public class ProviderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Identification { get; set; }
    public Guid ProviderTypeId { get; set; }
    public bool IsActive { get; set; }

    public ProviderDto(Guid Id, string Name, string Identification, Guid ProviderTypeId, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Identification = Identification;
        this.ProviderTypeId = ProviderTypeId;
        this.IsActive = IsActive;
    }
}
