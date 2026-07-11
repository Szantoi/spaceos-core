using SpaceOS.Modules.Sales.Application.Common;

namespace SpaceOS.Modules.Sales.Infrastructure.Security;

/// <summary>
/// ITenantContext implementation for the SalesIntegrationWorker, which operates
/// outside the HTTP request pipeline (no HttpContext available).
/// </summary>
public sealed class WorkerTenantContext(Guid tenantId) : ITenantContext
{
    /// <inheritdoc/>
    public Guid TenantId { get; } = tenantId;

    /// <inheritdoc/>
    public string ActorSub => "worker:integration";
}
