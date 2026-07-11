using System.Text.Json;
using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Snapshots;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Entities;

/// <summary>
/// Aggregate root representing a high-level workflow epic that tracks cross-facility collaboration.
/// A FlowEpic progresses through <see cref="WorkflowPhase"/> states and may be delegated to a guest tenant
/// via a <see cref="B2BHandshake"/>.
/// Implements <see cref="ISnapshotable"/> via an explicit DTO serialisation path (BE-P3B-01).
/// </summary>
public class FlowEpic : AggregateRoot, ISnapshotable
{
    /// <summary>Gets the unique identifier of this epic.</summary>
    public FlowEpicId Id { get; init; }

    /// <summary>Gets the title that describes the scope of this epic.</summary>
    public FlowEpicTitle Title { get; private set; }

    /// <summary>Gets the identifier of the facility targeted by this epic.</summary>
    public FacilityId TargetFacilityId { get; private set; }

    /// <summary>Gets the current workflow phase of this epic.</summary>
    public WorkflowPhase Phase { get; private set; }

    /// <summary>Gets the B2B handshake record when the epic has been delegated to a guest tenant, or <see langword="null"/> otherwise.</summary>
    public B2BHandshake? Handshake { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this flow epic.</summary>
    public TenantId TenantId { get; init; }

    /// <summary>Gets the URL of the uploaded proof document, or <see langword="null"/> if not yet closed.</summary>
    public string? ProofUrl { get; private set; }

    /// <summary>Gets the SHA-256 hex hash of the proof document content, or <see langword="null"/> if not yet closed.</summary>
    public string? ProofHash { get; private set; }

    /// <summary>Gets a value indicating whether this flow epic has been archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets the current stage code of this epic within its assigned chain, or <see langword="null"/> if no chain is assigned.</summary>
    public string? CurrentStageCode { get; private set; }

    /// <summary>Gets the identifier of the <see cref="StageChainTemplate"/> this epic follows, or <see langword="null"/> if not yet assigned.</summary>
    public Guid? StageChainTemplateId { get; private set; }

    /// <summary>Gets the business scope of this epic, or <see langword="null"/> for pre-existing epics without an explicit scope.</summary>
    public FlowEpicScope? Scope { get; private set; }

    /// <summary>Gets the skill level required for this epic (e.g. "Junior", "Senior"), or <see langword="null"/> if not applicable.</summary>
    public string? RequiredSkillLevel { get; private set; }

    private readonly List<FlowEpicRequiredResource> _requiredResources = new();

    /// <summary>Gets the resources required by this epic, or an empty list if none are specified.</summary>
    public IReadOnlyList<FlowEpicRequiredResource> RequiredResources => _requiredResources.AsReadOnly();

    private FlowEpic(FlowEpicId id, FlowEpicTitle title, FacilityId targetFacilityId, TenantId tenantId)
    {
        Id = id;
        Title = title;
        TargetFacilityId = targetFacilityId;
        TenantId = tenantId;
        Phase = WorkflowPhase.Discovery;
    }

    /// <summary>
    /// Creates a new <see cref="FlowEpic"/> in the <see cref="WorkflowPhase.Discovery"/> phase.
    /// Raises a <see cref="FlowEpicCreatedEvent"/>.
    /// </summary>
    /// <param name="title">A non-empty title describing the epic's scope.</param>
    /// <param name="targetFacilityId">The facility this epic targets.</param>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <returns>A newly created <see cref="FlowEpic"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="title"/> is null or whitespace.</exception>
    public static FlowEpic Create(string title, FacilityId targetFacilityId, TenantId tenantId)
    {
        var epicTitle = FlowEpicTitle.From(title);
        var epic = new FlowEpic(FlowEpicId.New(), epicTitle, targetFacilityId, tenantId);
        epic.AddDomainEvent(new FlowEpicCreatedEvent(epic.Id, targetFacilityId, DateTimeOffset.UtcNow));
        return epic;
    }

    /// <summary>
    /// Creates a new <see cref="FlowEpic"/> with an explicit <see cref="FlowEpicScope"/>.
    /// </summary>
    /// <param name="title">A non-empty title describing the epic's scope.</param>
    /// <param name="targetFacilityId">The facility this epic targets.</param>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="scope">The business scope of this epic.</param>
    /// <returns>A newly created <see cref="FlowEpic"/> instance with the given scope.</returns>
    public static FlowEpic Create(string title, FacilityId targetFacilityId, TenantId tenantId, FlowEpicScope scope)
    {
        var epic = Create(title, targetFacilityId, tenantId);
        epic.Scope = scope;
        return epic;
    }

    /// <summary>
    /// Sets the required skill level for this epic.
    /// </summary>
    /// <param name="skillLevel">The skill level (e.g. "Junior", "Senior"), or <see langword="null"/> to clear.</param>
    public void SetRequiredSkillLevel(string? skillLevel)
    {
        RequiredSkillLevel = skillLevel;
    }

    /// <summary>
    /// Adds a required resource to this epic.
    /// </summary>
    /// <param name="resource">The resource to add.</param>
    public void AddRequiredResource(FlowEpicRequiredResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        _requiredResources.Add(resource);
    }

    /// <summary>
    /// Advances this epic to the <see cref="WorkflowPhase.Delivery"/> phase.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the epic is already in the <see cref="WorkflowPhase.Delivery"/> phase.</exception>
    public void StartExecution()
    {
        if (Phase == WorkflowPhase.Delivery)
        {
            throw new DomainException("Workflow is already in Delivery phase.");
        }

        Phase = WorkflowPhase.Delivery;
        AddDomainEvent(new FlowEpicExecutionStartedEvent(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Delegates this epic to a guest tenant, creating a <see cref="B2BHandshake"/> and raising
    /// a <see cref="FlowEpicDelegatedEvent"/>.
    /// </summary>
    /// <param name="guestTenantId">The strongly-typed identifier of the guest tenant that will collaborate on this epic.</param>
    /// <exception cref="DomainException">Thrown when the epic is not in the <see cref="WorkflowPhase.Discovery"/> phase.</exception>
    public void DelegateTo(TenantId guestTenantId)
    {
        if (Phase != WorkflowPhase.Discovery)
        {
            throw new DomainException("An Epic can only be delegated while it is in the Discovery phase.");
        }

        var now = DateTimeOffset.UtcNow;
        Handshake = new B2BHandshake(guestTenantId, now);
        AddDomainEvent(new FlowEpicDelegatedEvent(Id, guestTenantId, now));
    }

    /// <summary>
    /// Updates the title of this epic.
    /// Raises a <see cref="FlowEpicTitleUpdatedEvent"/>.
    /// </summary>
    /// <param name="title">The new non-empty title.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="title"/> is null or whitespace.</exception>
    public void UpdateTitle(string title)
    {
        var oldTitle = Title.Value;
        Title = FlowEpicTitle.From(title);
        AddDomainEvent(new FlowEpicTitleUpdatedEvent(Id, oldTitle, Title.Value, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Closes this epic with a verified proof document, transitioning it to <see cref="WorkflowPhase.ClosedDone"/>.
    /// Raises a <see cref="FlowEpicClosedEvent"/>.
    /// </summary>
    /// <param name="proofUrl">URL of the uploaded proof document.</param>
    /// <param name="proofHash">SHA-256 hex hash of the proof document content.</param>
    /// <exception cref="DomainException">Thrown when the epic is not in the <see cref="WorkflowPhase.Delivery"/> phase.</exception>
    public void Close(string proofUrl, string proofHash)
    {
        if (Phase != WorkflowPhase.Delivery)
        {
            throw new DomainException("A FlowEpic can only be closed from the Delivery phase.");
        }

        Phase     = WorkflowPhase.ClosedDone;
        ProofUrl  = proofUrl;
        ProofHash = proofHash;
        AddDomainEvent(new FlowEpicClosedEvent(Id, TenantId, proofHash, OccurredOn: DateTimeOffset.UtcNow));
    }

    /// <summary>Archives this flow epic, preventing it from appearing in list results.</summary>
    /// <exception cref="DomainException">Thrown when the flow epic is already archived.</exception>
    public void Archive()
    {
        if (IsArchived)
            throw new DomainException($"{nameof(FlowEpic)} is already archived.");
        IsArchived = true;
        AddDomainEvent(new FlowEpicArchivedEvent(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Builds a snapshot DTO capturing all observable state of this <see cref="FlowEpic"/>.
    /// Uses explicit property access so private setters are not bypassed by serialisation.
    /// </summary>
    /// <returns>An anonymous DTO record containing the current aggregate state.</returns>
    public FlowEpicStateSnapshot ToSnapshotDto() => new(
        EpicId:                 Id.Value,
        TenantId:               TenantId.Value,
        Title:                  Title.Value,
        Phase:                  Phase.ToString(),
        IsArchived:             IsArchived,
        ProofUrl:               ProofUrl,
        ProofHash:              ProofHash,
        HandshakeGuestTenantId: Handshake?.GuestTenantId.Value,
        TargetFacilityId:       TargetFacilityId.Value,
        Scope:                  Scope?.ToString(),
        RequiredSkillLevel:     RequiredSkillLevel,
        SnapshotFormatVersion:  2);

    /// <summary>
    /// Assigns a <see cref="StageChainTemplate"/> to this epic and sets its initial stage.
    /// Raises <see cref="FlowEpicStageAdvancedEvent"/> with <c>From = null</c>.
    /// </summary>
    /// <param name="chainTemplateId">The identifier of the chain template to follow.</param>
    /// <param name="firstStageCode">The stage code of the first step in the chain.</param>
    /// <exception cref="DomainException">Thrown when a chain is already assigned to this epic.</exception>
    public void AssignChain(Guid chainTemplateId, string firstStageCode)
    {
        if (StageChainTemplateId.HasValue)
            throw new DomainException("A stage chain is already assigned to this epic.");

        StageChainTemplateId = chainTemplateId;
        CurrentStageCode     = firstStageCode;
        AddDomainEvent(new FlowEpicStageAdvancedEvent(Id.Value, TenantId.Value, null, firstStageCode, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Advances this epic to a new stage within its assigned chain.
    /// The caller is responsible for validating the transition via <see cref="Services.IStageChainValidator"/>
    /// before invoking this method (SEC-03 + BE-01).
    /// Raises <see cref="FlowEpicStageAdvancedEvent"/>.
    /// </summary>
    /// <param name="targetStageCode">The stage code to advance to.</param>
    public void AdvanceToStage(string targetStageCode)
    {
        var previousStage = CurrentStageCode;
        CurrentStageCode  = targetStageCode;
        AddDomainEvent(new FlowEpicStageAdvancedEvent(Id.Value, TenantId.Value, previousStage, targetStageCode, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Records that an optional stage was explicitly skipped.
    /// Raises <see cref="FlowEpicStageSkippedEvent"/>.
    /// </summary>
    /// <param name="stageCode">The stage code of the skipped stage.</param>
    public void SkipOptionalStage(string stageCode)
    {
        AddDomainEvent(new FlowEpicStageSkippedEvent(Id.Value, TenantId.Value, stageCode, DateTimeOffset.UtcNow));
    }

    /// <inheritdoc/>
    string ISnapshotable.ToSnapshotJson() =>
        JsonSerializer.Serialize(ToSnapshotDto());
}
