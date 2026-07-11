using Ardalis.Result;
using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Application.Common;

/// <summary>
/// Provides the current tenant identity from the HTTP context (JWT claims). SEC-S-07.
/// </summary>
public interface ITenantContext
{
    /// <summary>Tenant ID from the 'tenant_id' JWT claim.</summary>
    Guid TenantId { get; }

    /// <summary>Actor subject from the 'sub' JWT claim (used for audit log — SEC-S-08).</summary>
    string ActorSub { get; }
}

/// <summary>Defence-in-depth tenant guard on top of RLS. SEC-S-07.</summary>
public static class TenantGuardExtensions
{
    /// <summary>
    /// Returns <see cref="Result.Forbidden()"/> if the entity's TenantId does not match
    /// the caller's tenant. Explicit cross-tenant detection on top of the RLS layer.
    /// </summary>
    public static Result EnsureSameTenant(this ITenantContext ctx, TenantScopedEntity entity)
        => entity.TenantId == ctx.TenantId
            ? Result.Success()
            : Result.Forbidden();
}
