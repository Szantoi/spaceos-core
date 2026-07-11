// Identity.Infrastructure/Keycloak/Models/KcUserRepresentation.cs

using System.Text.Json.Serialization;

namespace Identity.Infrastructure.Keycloak.Models;

internal sealed class KcUserRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; } = false;

    [JsonPropertyName("attributes")]
    public Dictionary<string, List<string>>? Attributes { get; set; }

    [JsonPropertyName("realmRoles")]
    public List<string>? RealmRoles { get; set; }
}
