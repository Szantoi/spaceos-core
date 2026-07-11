// SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs
using System.Data.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.DTOs;

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="DbConnectionInterceptor"/> that sets the PostgreSQL session variable
/// <c>app.current_tenant_id</c> at SESSION level (<c>is_local=false</c>) on every opened
/// connection, and resets it when the connection returns to the pool.
/// </summary>
/// <remarks>
/// <para>
/// Using <c>is_local=false</c> ensures the variable survives across multiple transactions
/// within the same HTTP request, even when <see cref="Data.AppDbContext"/> and
/// <see cref="AuditDbContext"/> open separate transactions.  This closes the RLS bypass
/// identified in BE-P15-03.
/// </para>
/// <para>
/// KC-T2: the interceptor first reads the Keycloak <c>spaceos_tenants</c> JSON claim
/// (a JSON array of <see cref="TenantClaimDto"/>).  When a multi-tenant user provides an
/// <c>X-SpaceOS-Active-Tenant</c> request header the matching entry is used; otherwise the
/// first entry is selected (soft-launch single-tenant behaviour).  If the new claim is
/// absent the interceptor falls back to the legacy flat <c>tenant_id</c> / <c>tid</c> claim
/// for backward compatibility during the migration period.
/// </para>
/// <para>
/// When no HTTP context is present (background jobs, health checks) the interceptor is a
/// no-op — the session variable is left unset and the RLS policy falls back to the nil-UUID
/// sentinel via <c>COALESCE(..., '00000000-...')</c>.
/// </para>
/// <para>
/// Registered as a <see langword="Singleton"/> because <see cref="IHttpContextAccessor"/>
/// is itself a Singleton and the interceptor holds no per-request mutable state.
/// </para>
/// <para>
/// Only registered in production (PostgreSQL) — never in Development/Testing (SQLite).
/// SQLite does not support <c>set_config</c>.
/// </para>
/// </remarks>
internal sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private const string SpaceosTenantsClaim = "spaceos_tenants";
    private const string LegacyTenantIdClaim = "tenant_id"; // backward compat — remove after full migration
    private const string ActiveTenantHeader  = "X-SpaceOS-Active-Tenant";
    private const string PgConfigKey         = "app.current_tenant_id";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpContextAccessor _http;
    private readonly ILogger<TenantSessionInterceptor> _logger;

    /// <summary>Initialises a new <see cref="TenantSessionInterceptor"/>.</summary>
    /// <param name="http">Provides access to the current HTTP context.</param>
    /// <param name="logger">Logger for diagnostic warnings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> or <paramref name="logger"/> is <c>null</c>.</exception>
    public TenantSessionInterceptor(IHttpContextAccessor http, ILogger<TenantSessionInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(logger);
        _http   = http;
        _logger = logger;
    }

    /// <summary>
    /// Called after a connection is opened.  Sets <c>app.current_tenant_id</c> at SESSION
    /// level using a parameterised <c>set_config</c> call so the value is visible to all
    /// transactions on this connection for the duration of the request.
    /// </summary>
    /// <param name="connection">The opened database connection.</param>
    /// <param name="eventData">Event metadata provided by EF Core.</param>
    /// <param name="ct">Cancellation token.</param>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct)
    {
        // Always set app.current_tenant_id. When no tid claim is present (admin token, background
        // jobs, health checks) fall back to the sentinel UUID so that RLS policies using
        // current_setting('app.current_tenant_id') do not throw "unrecognized configuration parameter".
        // The sentinel '00000000-0000-0000-0000-000000000001' is matched by the
        // "OR ... = sentinel" branch in all RLS USING clauses, granting full read access.
        var tenantId = ResolveValidTenantId() ?? "00000000-0000-0000-0000-000000000001";

        await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);

        await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Called before a connection is returned to the pool.  Resets <c>app.current_tenant_id</c>
    /// to an empty string so pooled connections do not leak the previous request's tenant context.
    /// </summary>
    /// <param name="connection">The connection being closed.</param>
    /// <param name="eventData">Event metadata provided by EF Core.</param>
    /// <param name="result">The current interception result.</param>
    /// <returns>The interception result to pass to the next interceptor in the chain.</returns>
    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        // Reset regardless of whether we set it — prevents any residual context from
        // leaking to the next request that picks up this pooled connection.
        await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None).ConfigureAwait(false);

        return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private string? ResolveValidTenantId()
    {
        var context = _http.HttpContext;
        if (context is null)
            return null;

        // Priority 1: tid flat claim — matches ClaimsTenantResolver priority so that
        // app.current_tenant_id agrees with the EF global query filter (CurrentTenantGuid).
        // Mismatched UUIDs cause RLS violations on AggregateSnapshots and OutboxMessages
        // (FORCE RLS: "TenantId" = current_setting('app.current_tenant_id')::uuid).
        var tidClaim = context.User.FindFirst("tid")?.Value;
        if (ValidateGuid(tidClaim) is { } tidGuid)
            return tidGuid;

        // Priority 2: KC-T2 — parse Keycloak spaceos_tenants claim (JSON array).
        // Used when tid is absent (pure Keycloak production tokens without legacy tid).
        var tenantsClaim = context.User.FindFirst(SpaceosTenantsClaim)?.Value;
        if (tenantsClaim is not null)
        {
            List<TenantClaimDto>? tenants;
            try
            {
                // BE-01: double-deserialization — Keycloak Script Mapper JSON.stringify() may wrap the array
                var json = tenantsClaim.TrimStart().StartsWith('[')
                    ? tenantsClaim
                    : JsonSerializer.Deserialize<string>(tenantsClaim, _jsonOptions) ?? tenantsClaim;
                tenants = JsonSerializer.Deserialize<List<TenantClaimDto>>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize spaceos_tenants claim for user {Sub}",
                    context.User.FindFirst("sub")?.Value);
                tenants = null;
            }

            if (tenants is { Count: > 0 })
            {
                var activeTenantHeader = context.Request.Headers[ActiveTenantHeader].FirstOrDefault();
                if (activeTenantHeader is not null)
                {
                    var match = tenants.FirstOrDefault(t =>
                        string.Equals(t.TenantId, activeTenantHeader, StringComparison.OrdinalIgnoreCase));
                    if (match is null)
                    {
                        _logger.LogWarning(
                            "Active tenant header {Header} not in user's tenant list. Sub={Sub}",
                            activeTenantHeader, context.User.FindFirst("sub")?.Value);
                        throw new UnauthorizedAccessException(
                            "Active tenant not in user's authorized tenant list");
                    }
                    return ValidateGuid(match.TenantId);
                }
                // Soft launch: single-tenant — use first entry
                return ValidateGuid(tenants[0].TenantId);
            }
        }

        // Priority 3: legacy tenant_id flat claim (remove after full Keycloak migration)
        return ValidateGuid(context.User.FindFirst(LegacyTenantIdClaim)?.Value);
    }

    private static string? ValidateGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value, out var guid) && guid != Guid.Empty
            ? guid.ToString()
            : null;
    }

    private static async Task SetConfigAsync(
        DbConnection connection,
        string key,
        string value,
        CancellationToken ct)
    {
        // Parameterised set_config call — prevents SQL injection via claim manipulation.
        // set_config(setting_name text, new_value text, is_local boolean)
        // is_local=false → session-level, survives transaction boundaries.
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config(@key, @value, false)";

        var pKey = cmd.CreateParameter();
        pKey.ParameterName = "@key";
        pKey.Value = key;
        cmd.Parameters.Add(pKey);

        var pValue = cmd.CreateParameter();
        pValue.ParameterName = "@value";
        pValue.Value = value;
        cmd.Parameters.Add(pValue);

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
