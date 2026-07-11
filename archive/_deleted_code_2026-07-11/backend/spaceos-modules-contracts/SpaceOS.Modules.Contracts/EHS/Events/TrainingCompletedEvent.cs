using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.EHS.Events;

/// <summary>
/// Published when an employee completes EHS training.
/// Consumed by HR module to update employee competency matrix.
/// </summary>
public record TrainingCompletedEvent : ModuleEvent
{
    /// <summary>
    /// The employee who completed the training.
    /// </summary>
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// The training type/course identifier.
    /// </summary>
    public Guid TrainingTypeId { get; init; }

    /// <summary>
    /// Human-readable training name.
    /// </summary>
    public string TrainingName { get; init; } = string.Empty;

    /// <summary>
    /// Certification level achieved (e.g., "Level 1", "Level 2", "Advanced").
    /// </summary>
    public string CertificationLevel { get; init; } = string.Empty;

    /// <summary>
    /// When the training was completed.
    /// </summary>
    public DateTime CompletionDate { get; init; }

    /// <summary>
    /// When the certification expires (if applicable).
    /// </summary>
    public DateTime? CertificationExpiry { get; init; }
}
