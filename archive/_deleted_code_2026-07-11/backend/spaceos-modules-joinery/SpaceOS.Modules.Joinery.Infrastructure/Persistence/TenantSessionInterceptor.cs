using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence;

/// <summary>
/// Sets the PostgreSQL session variable <c>app.tenant_id</c> on every opened connection
/// so that RLS policies on DoorOrders and DoorItems can filter by tenant.
/// Resets the variable when the connection is returned to the pool.
/// </summary>
internal sealed class TenantSessionInterceptor(IHttpContextAccessor http) : DbConnectionInterceptor
{
    private const string TenantIdClaim = "tenant_id";
    private const string PgConfigKey   = "app.tenant_id";

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct)
    {
        var tenantId = ResolveTenantId();

        // Internal calls have no JWT → tenantId is null.
        // Skip set_config so that any GUC value already set by the endpoint handler
        // (e.g. InternalEndpoints) is not overwritten with an empty string, which
        // would cause RLS UUID cast failure (PostgreSQL error 22P02).
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
            return;
        }

        await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        // Internal calls have no JWT — skip the reset so pool cleanup doesn't
        // interfere with the GUC already set by the endpoint handler.
        var tenantId = ResolveTenantId();
        if (string.IsNullOrWhiteSpace(tenantId))
            return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);

        await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None).ConfigureAwait(false);
        return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
    }

    private string? ResolveTenantId()
    {
        var ctx = http.HttpContext;
        if (ctx is null) return null;

        var claim = ctx.User.FindFirst(TenantIdClaim)?.Value;
        if (string.IsNullOrWhiteSpace(claim)) return null;

        return Guid.TryParse(claim, out var guid) && guid != Guid.Empty
            ? guid.ToString()
            : null;
    }

    private static async Task SetConfigAsync(DbConnection connection, string key, string value, CancellationToken ct)
    {
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
