using System;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Join entity linking a FlowTask to a <see cref="SpatialElement"/> with a specific <see cref="WorkPhase"/>.
/// </summary>
public sealed class SpatialTaskLink : TenantScopedEntity
{
    /// <summary>Gets the identifier of the linked FlowTask.</summary>
    public Guid FlowTaskId { get; private set; }

    /// <summary>Gets the identifier of the linked <see cref="SpatialElement"/>.</summary>
    public Guid SpatialElementId { get; private set; }

    /// <summary>Gets the manufacturing or installation phase this link tracks.</summary>
    public WorkPhase WorkPhase { get; private set; }

    /// <summary>
    /// Required by EF Core for materialisation. Not for application use.
    /// </summary>
    private SpatialTaskLink() { }

    /// <summary>
    /// Creates a new <see cref="SpatialTaskLink"/> joining a FlowTask to a SpatialElement.
    /// </summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="flowTaskId">The FlowTask identifier.</param>
    /// <param name="spatialElementId">The SpatialElement identifier.</param>
    /// <param name="workPhase">The manufacturing or installation phase.</param>
    /// <returns>A new <see cref="SpatialTaskLink"/> instance.</returns>
    public static SpatialTaskLink Create(Guid tenantId, Guid flowTaskId, Guid spatialElementId, WorkPhase workPhase)
    {
        return new SpatialTaskLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FlowTaskId = flowTaskId,
            SpatialElementId = spatialElementId,
            WorkPhase = workPhase
        };
    }
}
