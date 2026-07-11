using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Aggregate root representing a tenant (organisation) within SpaceOS.
/// </summary>
public class Tenant : AggregateRoot
{
    /// <summary>Gets the unique identifier of this tenant.</summary>
    public TenantId Id { get; init; }

    /// <summary>Gets the display name of this tenant.</summary>
    public TenantName Name { get; private set; }

    /// <summary>Gets a value indicating whether this tenant has been archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets the brand skin identifier for UI theming. <c>null</c> means the default skin applies.</summary>
    public string? BrandSkinId { get; private set; }

    /// <summary>
    /// Gets the SHA-256 hex-encoded hash of the tenant's primary email address.
    /// Used by internal services (e.g. AttributionWorker) for email-based tenant lookup
    /// without exposing the raw email address. <c>null</c> when not yet set.
    /// </summary>
    public string? EmailHash { get; private set; }

    /// <summary>
    /// Gets the subdomain identifier for this tenant (e.g., "doorstar" for doorstar.joinerytech.hu).
    /// Used by the public-facing Customer Portal for subdomain-based tenant resolution.
    /// Must be unique across all tenants. <c>null</c> when subdomain-based access is not enabled.
    /// </summary>
    public string? Subdomain { get; private set; }

    /// <summary>
    /// Gets the ecosystem actor type for this tenant.
    /// Immutable after creation — changing it would invalidate module permissions and
    /// the B2B handshake graph (enforced by DB trigger SEC-01).
    /// </summary>
    public TenantType TenantType { get; private set; }

    private List<string> _enabledModules = new();

    /// <summary>Gets the list of enabled module names for this tenant (e.g. "door", "cabinet", "window").</summary>
    public IReadOnlyList<string> EnabledModules => _enabledModules.AsReadOnly();

    /// <summary>Parameterless constructor reserved for EF Core materialisation.</summary>
    private Tenant() { }

    /// <summary>
    /// Initialises a <see cref="Tenant"/> with an existing identity and name.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The tenant name.</param>
    private Tenant(TenantId id, TenantName name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>Sets the brand skin identifier. Pass <c>null</c> to revert to the default skin.</summary>
    /// <param name="skinId">The skin identifier, or <c>null</c> to use the default skin.</param>
    public void SetBrandSkin(string? skinId)
    {
        BrandSkinId = skinId;
    }

    /// <summary>
    /// Sets the SHA-256 hex-encoded hash of the tenant's primary email address.
    /// </summary>
    /// <param name="emailHash">
    /// A 64-character lowercase hex string, or <c>null</c> to clear the hash.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="emailHash"/> is non-null but not a 64-character hex string.</exception>
    public void SetEmailHash(string? emailHash)
    {
        if (emailHash is not null && (emailHash.Length != 64 || !System.Text.RegularExpressions.Regex.IsMatch(emailHash, @"^[0-9a-f]{64}$")))
            throw new ArgumentException("EmailHash must be a 64-character lowercase hex string (SHA-256).", nameof(emailHash));

        EmailHash = emailHash;
    }

    /// <summary>
    /// Sets the subdomain identifier for this tenant.
    /// </summary>
    /// <param name="subdomain">
    /// A lowercase alphanumeric string (with optional hyphens), or <c>null</c> to disable subdomain-based access.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="subdomain"/> contains invalid characters.</exception>
    public void SetSubdomain(string? subdomain)
    {
        if (subdomain is not null && !System.Text.RegularExpressions.Regex.IsMatch(subdomain, @"^[a-z0-9][a-z0-9\-]*[a-z0-9]$"))
            throw new ArgumentException("Subdomain must be lowercase alphanumeric (with optional hyphens), and cannot start or end with a hyphen.", nameof(subdomain));

        Subdomain = subdomain;
    }

    /// <summary>
    /// Creates a new <see cref="Tenant"/> with a freshly generated identifier and the default
    /// <see cref="TenantType.Manufacturer"/> type. Kept for backwards compatibility.
    /// Prefer <see cref="Register"/> for new code.
    /// Raises a <see cref="TenantCreatedEvent"/>.
    /// </summary>
    /// <param name="name">The display name for the tenant.</param>
    /// <returns>A newly created <see cref="Tenant"/> instance.</returns>
    public static Tenant Create(string name)
    {
        var tenant = new Tenant(TenantId.New(), TenantName.From(name));
        tenant.TenantType = TenantType.Manufacturer;
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, DateTimeOffset.UtcNow));
        return tenant;
    }

    /// <summary>
    /// Registers a new <see cref="Tenant"/> with an explicit ecosystem actor type and an
    /// optional initial set of enabled modules.
    /// Raises a <see cref="TenantCreatedEvent"/>.
    /// </summary>
    /// <param name="name">The display name for the tenant.</param>
    /// <param name="tenantType">
    /// The ecosystem actor type. Defaults to <see cref="TenantType.Manufacturer"/> so existing
    /// callers that supply only a name are not broken (BE-01 mitigation).
    /// </param>
    /// <param name="enabledModules">
    /// The initial set of enabled module names. <c>null</c> or empty means no modules enabled.
    /// Module names are validated by the DB trigger; call <see cref="UpdateEnabledModules"/> with
    /// an <see cref="IModuleRegistryService"/> to enforce Application-layer validation as well.
    /// </param>
    /// <returns>A newly created <see cref="Tenant"/> instance.</returns>
    public static Tenant Register(
        string name,
        TenantType tenantType = TenantType.Manufacturer,
        string[]? enabledModules = null)
    {
        var tenant = new Tenant(TenantId.New(), TenantName.From(name));
        tenant.TenantType = tenantType;
        tenant._enabledModules = (enabledModules ?? Array.Empty<string>()).ToList();
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, DateTimeOffset.UtcNow));
        return tenant;
    }

    /// <summary>
    /// Updates the tenant's display name.
    /// Raises a <see cref="TenantRenamedEvent"/>.
    /// </summary>
    /// <param name="newName">The new display name.</param>
    /// <returns>The same <see cref="Tenant"/> instance for fluent chaining.</returns>
    public Tenant UpdateName(string newName)
    {
        var oldName = Name.Value;
        Name = TenantName.From(newName);
        AddDomainEvent(new TenantRenamedEvent(Id, oldName, Name.Value, DateTimeOffset.UtcNow));
        return this;
    }

    /// <summary>
    /// Replaces the enabled module list after validating it against the
    /// <see cref="IModuleRegistryService"/> rules for this tenant's <see cref="TenantType"/>.
    /// Raises a <see cref="TenantModulesUpdatedEvent"/> on success.
    /// </summary>
    /// <param name="modules">The new set of module name strings (lowercase identifiers).</param>
    /// <param name="registry">The module registry used to validate the module list.</param>
    /// <returns>
    /// A <see cref="ModuleValidationResult"/> that is successful when all constraints are met,
    /// or carries a descriptive error message when validation fails.
    /// </returns>
    public ModuleValidationResult UpdateEnabledModules(string[] modules, IModuleRegistryService registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var validation = registry.ValidateModulesForTenantType(TenantType, modules);
        if (!validation.IsValid)
            return validation;

        _enabledModules = modules.ToList();
        AddDomainEvent(new TenantModulesUpdatedEvent(Id, modules, DateTimeOffset.UtcNow));
        return ModuleValidationResult.Success();
    }

    /// <summary>
    /// Sets the enabled modules for this tenant without module-type validation.
    /// Only legacy module names ("door", "cabinet", "window") are accepted.
    /// <para>
    /// This method is retained for backwards compatibility with existing seed data and tests.
    /// New code should prefer <see cref="UpdateEnabledModules"/>.
    /// </para>
    /// </summary>
    /// <param name="modules">The module names to enable.</param>
    /// <exception cref="DomainException">Thrown when any module name is not in the legacy allowed set.</exception>
    public void SetEnabledModules(IEnumerable<string> modules)
    {
        var valid = new[] { "door", "cabinet", "window" };
        var list = modules.ToList();
        if (list.Any(m => !valid.Contains(m)))
            throw new DomainException($"Invalid module name. Allowed: {string.Join(", ", valid)}");
        _enabledModules = list;
    }

    /// <summary>Archives this tenant, preventing it from appearing in list results.</summary>
    /// <exception cref="DomainException">Thrown when the tenant is already archived.</exception>
    public void Archive()
    {
        if (IsArchived)
            throw new DomainException($"{nameof(Tenant)} is already archived.");
        IsArchived = true;
        AddDomainEvent(new TenantArchivedEvent(Id, DateTimeOffset.UtcNow));
    }
}
