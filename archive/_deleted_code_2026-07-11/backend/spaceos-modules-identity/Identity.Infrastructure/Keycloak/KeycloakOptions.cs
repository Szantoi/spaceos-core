// Identity.Infrastructure/Keycloak/KeycloakOptions.cs

namespace Identity.Infrastructure.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = "http://localhost:8080/auth";
    public string Realm { get; set; } = "spaceos";
    public string ClientId { get; set; } = "spaceos-identity-service";
    public string ClientSecret { get; set; } = string.Empty;

    public string AdminBaseUrl => $"{BaseUrl.TrimEnd('/')}/admin/realms/{Realm}";
    public string TokenUrl => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}/protocol/openid-connect/token";
}
