using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

public sealed class TenantSessionInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextTenantAccessor _tenantAccessor;

    public TenantSessionInterceptor(IHttpContextTenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null && _tenantAccessor.TenantId.HasValue)
        {
            var tenantId = _tenantAccessor.TenantId.Value;
            var conn = eventData.Context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SET app.tenant_id = '{tenantId}'";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}

public interface IHttpContextTenantAccessor
{
    Guid? TenantId { get; }
}
