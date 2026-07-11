using System;
using System.Security.Cryptography;
using System.Text;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Aggregates;

/// <summary>
/// Aggregate root representing a physical space within a facility.
/// Tracks dimensions, origin, type, cell size and a deterministic registration hash (SEC-P3A-05).
/// No navigation to BvhNode children — tree traversal is via <c>IBvhRepository</c> (BE-P3A-01).
/// </summary>
public sealed class PhysicalSpace : AggregateRoot
{
    /// <summary>Gets the unique identifier of this physical space.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this physical space.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the identifier of the facility this space belongs to.</summary>
    public FacilityId FacilityId { get; private set; }

    /// <summary>Gets the width, height and depth dimensions in millimetres.</summary>
    public DimensionVector Dimensions { get; private set; }

    /// <summary>Gets the origin point of this space in millimetres.</summary>
    public Point3D Origin { get; private set; }

    /// <summary>Gets the classification of this physical space.</summary>
    public SpaceType SpaceType { get; private set; }

    /// <summary>Gets the spatial grid cell size in millimetres. Immutable after the first BvhNode is inserted.</summary>
    public int CellSizeMm { get; private set; }

    /// <summary>Gets the SHA-256 registration hash computed at creation time (SEC-P3A-05).</summary>
    public string RegistrationHash { get; private set; } = string.Empty;

    /// <summary>
    /// Required by EF Core for materialisation. Not for application use.
    /// </summary>
    private PhysicalSpace() { }

    /// <summary>
    /// Registers a new <see cref="PhysicalSpace"/> with a freshly generated identifier.
    /// Raises a <see cref="PhysicalSpaceRegisteredEvent"/>.
    /// </summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="facilityId">The facility this space belongs to.</param>
    /// <param name="dims">Width, height and depth in millimetres.</param>
    /// <param name="origin">The origin point in millimetres.</param>
    /// <param name="type">The classification of the space.</param>
    /// <param name="cellSizeMm">The spatial grid cell size in millimetres (default 500).</param>
    /// <returns>A newly registered <see cref="PhysicalSpace"/> instance.</returns>
    public static PhysicalSpace Register(
        Guid tenantId,
        FacilityId facilityId,
        DimensionVector dims,
        Point3D origin,
        SpaceType type,
        int cellSizeMm = 500)
    {
        var now = DateTimeOffset.UtcNow;
        var space = new PhysicalSpace
        {
            Id               = Guid.NewGuid(),
            TenantId         = tenantId,
            FacilityId       = facilityId,
            Dimensions       = dims,
            Origin           = origin,
            SpaceType        = type,
            CellSizeMm      = cellSizeMm,
            RegistrationHash = ComputeRegistrationHash(
                tenantId, facilityId, dims, origin, type, cellSizeMm, now)
        };

        space.AddDomainEvent(new PhysicalSpaceRegisteredEvent(
            space.Id,
            tenantId,
            facilityId.Value,
            type.ToString(),
            dims.WidthMm,
            dims.HeightMm,
            dims.DepthMm,
            now));

        return space;
    }

    /// <summary>
    /// Computes a deterministic SHA-256 hash over all registration parameters (SEC-P3A-05).
    /// </summary>
    private static string ComputeRegistrationHash(
        Guid tenantId,
        FacilityId facilityId,
        DimensionVector dims,
        Point3D origin,
        SpaceType type,
        int cellSizeMm,
        DateTimeOffset createdAt)
    {
        var input = string.Join("|",
            tenantId.ToString("D"),
            facilityId.Value.ToString("D"),
            dims.WidthMm,
            dims.HeightMm,
            dims.DepthMm,
            origin.X,
            origin.Y,
            origin.Z,
            type.ToString(),
            cellSizeMm,
            createdAt.ToUnixTimeMilliseconds());

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
