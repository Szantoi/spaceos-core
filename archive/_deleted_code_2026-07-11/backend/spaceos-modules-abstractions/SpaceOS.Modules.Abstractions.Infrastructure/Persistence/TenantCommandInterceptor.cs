using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

public sealed class TenantCommandInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextTenantAccessor _tenantAccessor;

    public TenantCommandInterceptor(IHttpContextTenantAccessor tenantAccessor)
        => _tenantAccessor = tenantAccessor;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        SetTenantOnConnection(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        SetTenantOnConnection(command);
        return ValueTask.FromResult(result);
    }

    private void SetTenantOnConnection(DbCommand command)
    {
        var tenantId = _tenantAccessor.TenantId;
        if (tenantId.HasValue && command.Connection?.State == System.Data.ConnectionState.Open)
        {
            using var setCmd = command.Connection.CreateCommand();
            setCmd.CommandText = $"SET app.tenant_id = '{tenantId.Value}'";
            setCmd.ExecuteNonQuery();
        }
    }
}
