// SpaceOS.Kernel.Domain/Auth/ITenantResolver.cs
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Auth;

/// <summary>
/// Resolves the current tenant from the ambient context (e.g. HTTP request claims).
/// Returns <c>null</c> when no tenant context is available (admin / background operations).
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Attempts to resolve the current tenant identifier.
    /// </summary>
    /// <returns>
    /// The <see cref="TenantId"/> extracted from the ambient context,
    /// or <c>null</c> if no tenant context is present.
    /// </returns>
    TenantId? TryResolve();
}
