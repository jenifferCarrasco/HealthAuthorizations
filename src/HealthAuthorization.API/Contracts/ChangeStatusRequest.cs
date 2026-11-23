using System.Text.Json.Serialization;

namespace HealthAuthorization.API.Contracts;

public class ChangeStatusRequest
{
    [JsonRequired]
    public bool IsActive { get; set; }
}
