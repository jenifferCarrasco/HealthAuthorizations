namespace HealthAuthorization.Application.Features.ProviderTypes;

public class ProviderTypeInput
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public ProviderTypeInput(string Name, string? Description)
    {
        this.Name = Name;
        this.Description = Description;
    }
}

public class ProviderTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ProviderTypeDto(Guid Id, string Name, string? Description, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.IsActive = IsActive;
    }
}
