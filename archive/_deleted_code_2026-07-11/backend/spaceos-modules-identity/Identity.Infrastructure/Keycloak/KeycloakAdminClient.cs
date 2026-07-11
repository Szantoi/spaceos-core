// Identity.Infrastructure/Keycloak/KeycloakAdminClient.cs

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Keycloak.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Keycloak;

public sealed class KeycloakAdminClient : IIdentityProviderClient
{
    private readonly HttpClient _http;
    private readonly IKeycloakTokenProvider _tokenProvider;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakAdminClient> _logger;

    public KeycloakAdminClient(
        HttpClient http,
        IKeycloakTokenProvider tokenProvider,
        IOptions<KeycloakOptions> options,
        ILogger<KeycloakAdminClient> logger)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<KeycloakUserId> CreateUserAsync(
        Guid tenantId,
        Email email,
        DisplayName displayName,
        CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);

        var body = new KcUserRepresentation
        {
            Username = email.Value,
            Email = email.Value,
            FirstName = displayName.FirstName,
            LastName = displayName.LastName,
            Enabled = true,
            EmailVerified = false,
            Attributes = new() { ["tid"] = [tenantId.ToString()] }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.AdminBaseUrl}/users")
        {
            Headers = { { "Authorization", $"Bearer {token}" } },
            Content = JsonContent.Create(body)
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IdentityProviderException("KC CreateUser request failed.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new IdentityProviderException(
                $"KC CreateUser failed: {(int)response.StatusCode}",
                (int)response.StatusCode);

        // KC returns 201 with Location: .../users/{id}
        var location = response.Headers.Location?.ToString()
            ?? throw new IdentityProviderException("KC CreateUser response missing Location header.");

        var kcId = location.Split('/').Last();

        // SEC-01: tid assert — verify the created user's tenant attribute
        var createdUser = await GetKcUserAsync(kcId, token, ct).ConfigureAwait(false);
        var kcTid = createdUser.Attributes?.GetValueOrDefault("tid")?.FirstOrDefault();
        if (kcTid != tenantId.ToString())
        {
            _logger.LogError(
                "KC tid mismatch on CreateUser: expected {Expected}, got {Actual}, kcId={KcId}",
                tenantId, kcTid, kcId);
            throw new IdentityProviderException($"KC tenant id mismatch after user creation. kcId={kcId}");
        }

        return KeycloakUserId.From(kcId);
    }

    public async Task UpdateUserAsync(
        KeycloakUserId keycloakUserId,
        DisplayName displayName,
        CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);

        var body = new KcUserRepresentation
        {
            FirstName = displayName.FirstName,
            LastName = displayName.LastName
        };

        await SendAdminRequestAsync(
            HttpMethod.Put,
            $"{_options.AdminBaseUrl}/users/{keycloakUserId.Value}",
            token, body, ct).ConfigureAwait(false);
    }

    public async Task DisableUserAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var body = new KcUserRepresentation { Enabled = false };
        await SendAdminRequestAsync(
            HttpMethod.Put,
            $"{_options.AdminBaseUrl}/users/{keycloakUserId.Value}",
            token, body, ct).ConfigureAwait(false);
    }

    public async Task EnableUserAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var body = new KcUserRepresentation { Enabled = true };
        await SendAdminRequestAsync(
            HttpMethod.Put,
            $"{_options.AdminBaseUrl}/users/{keycloakUserId.Value}",
            token, body, ct).ConfigureAwait(false);
    }

    public async Task ResetPasswordAsync(KeycloakUserId keycloakUserId, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);

        var url = $"{_options.AdminBaseUrl}/users/{keycloakUserId.Value}/execute-actions-email";
        var actions = new[] { "UPDATE_PASSWORD" };

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Headers = { { "Authorization", $"Bearer {token}" } },
            Content = JsonContent.Create(actions)
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IdentityProviderException("KC ResetPassword request failed.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new IdentityProviderException(
                $"KC ResetPassword failed: {(int)response.StatusCode}",
                (int)response.StatusCode);
    }

    public async Task<IReadOnlyList<(KeycloakUserId KcId, Email Email, DisplayName DisplayName)>>
        ListTenantUsersAsync(Guid tenantId, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var results = new List<(KeycloakUserId, Email, DisplayName)>();
        int first = 0;
        const int max = 200;

        while (true)
        {
            var url = $"{_options.AdminBaseUrl}/users?q=tid:{tenantId}&first={first}&max={max}";
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new IdentityProviderException("KC ListUsers request failed.", ex);
            }

            if (!response.IsSuccessStatusCode)
                throw new IdentityProviderException(
                    $"KC ListUsers failed: {(int)response.StatusCode}",
                    (int)response.StatusCode);

            var page = await response.Content
                .ReadFromJsonAsync<List<KcUserRepresentation>>(cancellationToken: ct)
                .ConfigureAwait(false) ?? [];

            foreach (var u in page)
            {
                if (u.Id is null || u.Email is null || u.FirstName is null || u.LastName is null)
                    continue;

                try
                {
                    results.Add((
                        KeycloakUserId.From(u.Id),
                        Email.From(u.Email),
                        DisplayName.From(u.FirstName, u.LastName)));
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Skipping invalid KC user {KcId}", u.Id);
                }
            }

            if (page.Count < max) break;
            first += max;
        }

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<(KeycloakUserId KcId, Email Email, DisplayName DisplayName, string Role)>>
        GetUsersByRoleAsync(Guid tenantId, string role, CancellationToken ct = default)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var results = new List<(KeycloakUserId, Email, DisplayName, string)>();
        int first = 0;
        const int max = 200;

        // Query users by role from Keycloak
        // Using /admin/realms/{realm}/roles/{role-name}/users endpoint
        while (true)
        {
            var url = $"{_options.AdminBaseUrl}/roles/{role}/users?first={first}&max={max}";
            var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Headers = { { "Authorization", $"Bearer {token}" } }
            };

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new IdentityProviderException("KC GetUsersByRole request failed.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                // If role doesn't exist, return empty list
                if ((int)response.StatusCode == 404)
                {
                    _logger.LogWarning("KC role not found: {Role}", role);
                    return results.AsReadOnly();
                }

                throw new IdentityProviderException(
                    $"KC GetUsersByRole failed: {(int)response.StatusCode}",
                    (int)response.StatusCode);
            }

            var page = await response.Content
                .ReadFromJsonAsync<List<KcUserRepresentation>>(cancellationToken: ct)
                .ConfigureAwait(false) ?? [];

            foreach (var u in page)
            {
                if (u.Id is null || u.Email is null || u.FirstName is null || u.LastName is null)
                    continue;

                // Filter by tenant (RLS policy)
                var kcTid = u.Attributes?.GetValueOrDefault("tid")?.FirstOrDefault();
                if (kcTid != tenantId.ToString())
                    continue;

                try
                {
                    results.Add((
                        KeycloakUserId.From(u.Id),
                        Email.From(u.Email),
                        DisplayName.From(u.FirstName, u.LastName),
                        role));
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Skipping invalid KC user {KcId}", u.Id);
                }
            }

            if (page.Count < max) break;
            first += max;
        }

        return results.AsReadOnly();
    }

    private async Task<KcUserRepresentation> GetKcUserAsync(
        string kcId, string token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.AdminBaseUrl}/users/{kcId}")
        {
            Headers = { { "Authorization", $"Bearer {token}" } }
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IdentityProviderException($"KC GetUser({kcId}) failed.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new IdentityProviderException(
                $"KC GetUser failed: {(int)response.StatusCode}",
                (int)response.StatusCode);

        return await response.Content.ReadFromJsonAsync<KcUserRepresentation>(
            cancellationToken: ct).ConfigureAwait(false)
            ?? throw new IdentityProviderException($"KC GetUser({kcId}) returned empty body.");
    }

    private async Task SendAdminRequestAsync(
        HttpMethod method, string url, string token,
        object body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Headers = { { "Authorization", $"Bearer {token}" } },
            Content = JsonContent.Create(body)
        };

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IdentityProviderException($"KC admin request to {url} failed.", ex);
        }

        if (!response.IsSuccessStatusCode)
            throw new IdentityProviderException(
                $"KC admin request failed: {(int)response.StatusCode}",
                (int)response.StatusCode);
    }
}
