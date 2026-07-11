namespace SpaceOS.Modules.Abstractions.Domain.Enums;

public enum RuleOperator
{
    Identity,   // child = parent
    Subtract,   // child = parent − operand
    Add,        // child = parent + operand
    SubtractN,  // child = parent − (operand × count)
    Max,        // child = max(parent, secondary) − operand
    Min,        // child = min(parent, secondary) − operand
    Constant    // child = operand (independent of parent)
}
