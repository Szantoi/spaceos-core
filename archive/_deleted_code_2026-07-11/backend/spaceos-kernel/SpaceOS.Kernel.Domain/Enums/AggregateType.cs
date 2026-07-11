// SpaceOS.Kernel.Domain/Enums/AggregateType.cs

namespace SpaceOS.Kernel.Domain.Enums;

/// <summary>
/// Identifies the type of aggregate captured in an <c>AggregateSnapshot</c>.
/// Values are stored as strings in the database — do not rename existing members.
/// </summary>
public enum AggregateType
{
    /// <summary>A <c>FlowEpic</c> aggregate.</summary>
    FlowEpic = 1,

    /// <summary>A <c>FlowMilestone</c> aggregate.</summary>
    FlowMilestone = 2,

    /// <summary>A <c>B2BHandshake</c> aggregate.</summary>
    B2BHandshake = 3,

    /// <summary>A <c>SpaceLayer</c> aggregate.</summary>
    SpaceLayer = 4
}
