using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Entities;

public sealed class SlotConnection : TenantScopedEntity
{
    public Guid TemplateId { get; private set; }
    public Guid ParentSlotId { get; private set; }
    public Guid ChildSlotId { get; private set; }
    public DimensionAxis Axis { get; private set; }
    public RuleOperator Operator { get; private set; }
    public decimal Operand { get; private set; }
    public int? MultiplierCount { get; private set; }
    public Guid? SecondaryParentSlotId { get; private set; }
    public JointType JointType { get; private set; }
    public MachiningOperation MachiningOp { get; private set; }
    public ProcessPhase ProcessPhase { get; private set; }
    public decimal? GrooveDepth { get; private set; }
    public decimal? GrooveWidth { get; private set; }
    public decimal? DrillDiameter { get; private set; }
    public decimal? DrillDepth { get; private set; }
    public decimal? Angle { get; private set; }
    public decimal? Radius { get; private set; }
    public string? JointNote { get; private set; }

    private SlotConnection() { }

    public static Result<SlotConnection> Create(
        Guid templateId, Guid tenantId,
        Guid parentSlotId, Guid childSlotId,
        DimensionAxis axis, RuleOperator op, decimal operand, int? multiplierCount,
        Guid? secondaryParentSlotId,
        JointType jointType, MachiningOperation machiningOp, ProcessPhase processPhase,
        decimal? grooveDepth, decimal? grooveWidth,
        decimal? drillDiameter, decimal? drillDepth,
        decimal? angle, decimal? radius,
        string? jointNote = null)
    {
        if (parentSlotId == childSlotId)
            return Result<SlotConnection>.Error("Self-loop forbidden (DB-02)");

        if (!Enum.IsDefined(op))
            throw new DomainException($"Unknown RuleOperator: {op}"); // SEC-03
        if (!Enum.IsDefined(axis))
            throw new DomainException($"Unknown DimensionAxis: {axis}");

        return Result<SlotConnection>.Success(new SlotConnection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = templateId,
            ParentSlotId = parentSlotId,
            ChildSlotId = childSlotId,
            Axis = axis,
            Operator = op,
            Operand = operand,
            MultiplierCount = multiplierCount,
            SecondaryParentSlotId = secondaryParentSlotId,
            JointType = jointType,
            MachiningOp = machiningOp,
            ProcessPhase = processPhase,
            GrooveDepth = grooveDepth,
            GrooveWidth = grooveWidth,
            DrillDiameter = drillDiameter,
            DrillDepth = drillDepth,
            Angle = angle,
            Radius = radius,
            JointNote = jointNote
        });
    }
}
