namespace HealthAuthorization.Application.Features.ServiceTypes;

public class ServiceTypeInput
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public ServiceTypeInput(string Name, string? Description)
    {
        this.Name = Name;
        this.Description = Description;
    }
}

public class ServiceTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ServiceTypeDto(Guid Id, string Name, string? Description, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.IsActive = IsActive;
    }
}
