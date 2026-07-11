// Identity.Infrastructure/Keycloak/Models/KcTokenResponse.cs

using System.Text.Json.Serialization;

namespace Identity.Infrastructure.Keycloak.Models;

internal sealed class KcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}
