namespace HealthAuthorization.Application.Features.Contractors;

public class ContractorInput
{
    public string Name { get; set; }
    public string Identification { get; set; }

    public ContractorInput(string Name, string Identification)
    {
        this.Name = Name;
        this.Identification = Identification;
    }
}

public class ContractorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Identification { get; set; }
    public bool IsActive { get; set; }

    public ContractorDto(Guid Id, string Name, string Identification, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Identification = Identification;
        this.IsActive = IsActive;
    }
}
