// Identity.Domain/Interfaces/IIdentityProviderClient.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.Interfaces;

public interface IIdentityProviderClient
{
    Task<KeycloakUserId> CreateUserAsync(
        Guid tenantId,
        Email email,
        DisplayName displayName,
        CancellationToken ct = default);

    Task UpdateUserAsync(
        KeycloakUserId keycloakUserId,
        DisplayName displayName,
        CancellationToken ct = default);

    Task DisableUserAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default);

    Task EnableUserAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default);

    Task ResetPasswordAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default);

    Task<IReadOnlyList<(KeycloakUserId KcId, Email Email, DisplayName DisplayName)>> ListTenantUsersAsync(
        Guid tenantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<(KeycloakUserId KcId, Email Email, DisplayName DisplayName, string Role)>> GetUsersByRoleAsync(
        Guid tenantId,
        string role,
        CancellationToken ct = default);
}
