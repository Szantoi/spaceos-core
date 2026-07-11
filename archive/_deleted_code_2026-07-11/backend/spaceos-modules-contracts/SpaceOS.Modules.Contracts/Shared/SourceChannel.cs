namespace SpaceOS.Modules.Contracts.Shared;

/// <summary>
/// Identifies the channel through which a request entered the system.
/// Used for audit trail, RBAC differentiation, and rate-limit policies.
/// v1.3.0 — introduced for FreeTier/PartnerTier extension points.
/// </summary>
public enum SourceChannel
{
    /// <summary>Authenticated tenant user via Portal or BFF.</summary>
    Direct = 0,

    /// <summary>Anonymous workspace (FreeTier) — no tenant context, rate-limited by IP/session.</summary>
    FreeTier = 1,

    /// <summary>B2B2C embedded flow via partner integration (PartnerTier).</summary>
    Partner = 2,

    /// <summary>Programmatic ERP/API integration (future).</summary>
    Api = 3,
}
