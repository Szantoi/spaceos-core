using System;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Entity representing a spatial element within a BVH leaf node.
/// Tracks the trade type, element classification and archival status.
/// </summary>
public sealed class SpatialElement : TenantScopedEntity
{
    /// <summary>Gets the identifier of the BVH leaf node that contains this element.</summary>
    public Guid BvhLeafId { get; private set; }

    /// <summary>Gets the identifier of the <see cref="FlowEpic"/> this element belongs to.</summary>
    public Guid FlowEpicId { get; private set; }

    /// <summary>Gets the construction trade type of this element.</summary>
    public TradeType TradeType { get; private set; }

    /// <summary>Gets the driver-specific element classification.</summary>
    public string ElementType { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether this element has been archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>
    /// Required by EF Core for materialisation. Not for application use.
    /// </summary>
    private SpatialElement() { }

    /// <summary>
    /// Creates a new <see cref="SpatialElement"/> linked to a BVH leaf node.
    /// </summary>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="bvhLeafId">The BVH leaf node that contains this element.</param>
    /// <param name="flowEpicId">The FlowEpic this element belongs to.</param>
    /// <param name="tradeType">The construction trade type.</param>
    /// <param name="elementType">The driver-specific element classification.</param>
    /// <returns>A new <see cref="SpatialElement"/> instance.</returns>
    public static SpatialElement Create(Guid tenantId, Guid bvhLeafId, Guid flowEpicId, TradeType tradeType, string elementType)
    {
        return new SpatialElement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BvhLeafId = bvhLeafId,
            FlowEpicId = flowEpicId,
            TradeType = tradeType,
            ElementType = elementType,
            IsArchived = false
        };
    }
}
