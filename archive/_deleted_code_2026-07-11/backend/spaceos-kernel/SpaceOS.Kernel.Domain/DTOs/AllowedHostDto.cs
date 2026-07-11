// SpaceOS.Kernel.Domain/DTOs/AllowedHostDto.cs

namespace SpaceOS.Kernel.Domain.DTOs;

/// <summary>
/// Lightweight projection of an allowed host tenant, used in JWT claim serialisation.
/// </summary>
/// <param name="TenantId">The host tenant identifier.</param>
/// <param name="TenantName">The host tenant display name.</param>
/// <param name="AllowedTradeTypes">The trade types allowed for this host tenant.</param>
public sealed record AllowedHostDto(Guid TenantId, string TenantName, IReadOnlyList<string> AllowedTradeTypes);
