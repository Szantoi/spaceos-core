// Identity.Infrastructure/Keycloak/IKeycloakTokenProvider.cs

namespace Identity.Infrastructure.Keycloak;

public interface IKeycloakTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}
