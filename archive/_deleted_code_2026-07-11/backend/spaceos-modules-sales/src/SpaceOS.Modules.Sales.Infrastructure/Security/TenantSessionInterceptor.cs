using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SpaceOS.Modules.Sales.Infrastructure.Security;

/// <summary>
/// Sets the <c>app.current_tenant_id</c> PostgreSQL GUC on every new connection,
/// enabling RLS to filter rows by tenant. SEC-S-07.
/// </summary>
internal sealed class TenantSessionInterceptor(IHttpContextAccessor http) : DbConnectionInterceptor
{
    private const string PgConfigKey = "app.current_tenant_id";

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken ct = default)
    {
        var tenantId = ResolveTenantId();
        if (tenantId is not null)
            await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        var tenantId = ResolveTenantId();
        if (tenantId is not null)
            await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None)
                .ConfigureAwait(false);
        return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
    }

    private string? ResolveTenantId()
    {
        var ctx = http.HttpContext;
        if (ctx is null) return null;
        var claim = ctx.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var g) && g != Guid.Empty ? g.ToString() : null;
    }

    private static async Task SetConfigAsync(
        DbConnection conn, string key, string value, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT set_config(@key, @value, false)";
        var pk = cmd.CreateParameter(); pk.ParameterName = "@key"; pk.Value = key; cmd.Parameters.Add(pk);
        var pv = cmd.CreateParameter(); pv.ParameterName = "@value"; pv.Value = value; cmd.Parameters.Add(pv);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
