using System.Net.Http.Json;
using Ardalis.Result;
using Microsoft.Extensions.Configuration;
using SpaceOS.Modules.Sales.Abstractions.Ports;

namespace SpaceOS.Modules.Sales.Infrastructure.Adapters;

/// <summary>
/// HTTP adapter that queries the Kernel tenant actor directory endpoint.
/// SEC-S-09: returns minimal actor info only (no contact/billing data).
/// </summary>
internal sealed class KernelActorDirectoryClient(
    HttpClient http,
    IConfiguration cfg) : IActorDirectoryPort
{
    /// <inheritdoc/>
    public async Task<Result<ActorDirectoryEntry>> GetTenantActorAsync(
        Guid requesterTenantId,
        Guid platformTenantId,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"http://127.0.0.1:5000/api/internal/tenants/{platformTenantId}");

        req.Headers.TryAddWithoutValidation(
            "X-SpaceOS-Internal", cfg["SpaceOS:InternalSecret"]);
        req.Headers.TryAddWithoutValidation(
            "X-SpaceOS-TenantId", requesterTenantId.ToString());

        var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return Result.NotFound($"Tenant {platformTenantId} not found.");

        var entry = await resp.Content
            .ReadFromJsonAsync<ActorDirectoryEntry>(cancellationToken: ct)
            .ConfigureAwait(false);

        return Result.Success(entry!);
    }
}
