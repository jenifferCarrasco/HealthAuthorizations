namespace HealthAuthorization.Application.Features.Intermediaries;

public class IntermediaryInput
{
    public string Name { get; set; }
    public string Identification { get; set; }

    public IntermediaryInput(string Name, string Identification)
    {
        this.Name = Name;
        this.Identification = Identification;
    }
}

public class IntermediaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Identification { get; set; }
    public bool IsActive { get; set; }

    public IntermediaryDto(Guid Id, string Name, string Identification, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Identification = Identification;
        this.IsActive = IsActive;
    }
}
