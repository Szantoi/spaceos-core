// SpaceOS.Kernel.Domain/Entities/StageDefinition.cs
using System;
using System.Text.RegularExpressions;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Represents the registration of a Stage Module within the Kernel Stage Registry.
/// Each stage definition links a tenant-scoped <see cref="StageCode"/> to a loopback
/// <see cref="ModuleEndpoint"/> that the Orchestrator uses to dispatch work.
/// </summary>
/// <remarks>
/// StageCode is immutable after creation (DB-10). ModuleEndpoint must target a loopback
/// address on port 5000-5099 (SEC-01). Deactivated definitions are excluded from active
/// chain templates via the partial index on <c>IsActive</c>.
/// </remarks>
public sealed class StageDefinition : TenantScopedAggregateRoot
{
    private static readonly Regex StageCodeRegex =
        new(@"^[a-z][a-z0-9_]{1,28}[a-z0-9]$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    // SEC-01: ModuleEndpoint must target a loopback address on port 5000-5099 only.
    // This prevents SSRF attacks where arbitrary external hosts could be specified.
    private static readonly Regex ModuleEndpointRegex =
        new(@"^https?://(127\.0\.0\.1|localhost):(50[0-9]{2})$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>Gets the immutable stage code that identifies this stage module.</summary>
    public string StageCode { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable display name for this stage.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Gets the loopback URL of the Stage Module (SEC-01: port 5000-5099).</summary>
    public string ModuleEndpoint { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this stage definition is currently active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the UTC timestamp when this definition was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this definition was last modified.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core parameterless constructor
    private StageDefinition() { }

    /// <summary>
    /// Registers a new <see cref="StageDefinition"/> for a tenant.
    /// Raises <see cref="StageDefinitionRegisteredEvent"/>.
    /// </summary>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="stageCode">The lowercase stage code — must match <c>^[a-z][a-z0-9_]{1,28}[a-z0-9]$</c>.</param>
    /// <param name="displayName">A human-readable name (max 100 characters).</param>
    /// <param name="moduleEndpoint">The loopback URL of the Stage Module (SEC-01: port 5000-5099).</param>
    /// <returns>A newly created and active <see cref="StageDefinition"/>.</returns>
    /// <exception cref="DomainException">Thrown when any argument fails validation.</exception>
    public static StageDefinition Register(
        Guid tenantId,
        string stageCode,
        string displayName,
        string moduleEndpoint)
    {
        if (string.IsNullOrWhiteSpace(stageCode))
            throw new DomainException("StageCode is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName is required.");
        if (string.IsNullOrWhiteSpace(moduleEndpoint))
            throw new DomainException("ModuleEndpoint is required.");

        var normalised = stageCode.ToLowerInvariant();
        if (!StageCodeRegex.IsMatch(normalised))
            throw new DomainException($"Invalid StageCode format: '{stageCode}'. Must match ^[a-z][a-z0-9_]{{1,28}}[a-z0-9]$");

        // SEC-01: enforce loopback-only, port 5000-5099 constraint at domain level.
        if (!ModuleEndpointRegex.IsMatch(moduleEndpoint))
            throw new DomainException($"Invalid ModuleEndpoint: '{moduleEndpoint}'. Must be loopback (127.0.0.1 or localhost) on port 5000-5099.");

        var now = DateTimeOffset.UtcNow;
        var sd = new StageDefinition
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId,
            StageCode      = normalised,
            DisplayName    = displayName,
            ModuleEndpoint = moduleEndpoint,
            IsActive       = true,
            CreatedAt      = now,
            UpdatedAt      = now
        };
        sd.AddDomainEvent(new StageDefinitionRegisteredEvent(sd.Id, tenantId, normalised, now));
        return sd;
    }

    /// <summary>
    /// Updates the module endpoint URL for this stage definition.
    /// Raises <see cref="StageDefinitionUpdatedEvent"/>.
    /// </summary>
    /// <param name="moduleEndpoint">The new loopback URL (SEC-01: port 5000-5099).</param>
    /// <exception cref="DomainException">Thrown when <paramref name="moduleEndpoint"/> is null or whitespace.</exception>
    public void UpdateEndpoint(string moduleEndpoint)
    {
        if (string.IsNullOrWhiteSpace(moduleEndpoint))
            throw new DomainException("ModuleEndpoint is required.");

        // SEC-01: same constraint as Register — loopback + port 5000-5099.
        if (!ModuleEndpointRegex.IsMatch(moduleEndpoint))
            throw new DomainException($"Invalid ModuleEndpoint: '{moduleEndpoint}'. Must be loopback (127.0.0.1 or localhost) on port 5000-5099.");

        ModuleEndpoint = moduleEndpoint;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new StageDefinitionUpdatedEvent(Id, TenantId, StageCode, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Deactivates this stage definition, excluding it from active chain lookups.
    /// Raises <see cref="StageDefinitionDeactivatedEvent"/>.
    /// </summary>
    public void Deactivate()
    {
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new StageDefinitionDeactivatedEvent(Id, TenantId, StageCode, DateTimeOffset.UtcNow));
    }
}
