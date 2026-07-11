using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.WorkOrders.Commands.UpdateAssemblySequence;

/// <summary>
/// Validator for UpdateAssemblySequenceCommand.
/// Validates sequence continuity (1, 2, 3, ..., N with no gaps).
/// </summary>
public sealed class UpdateAssemblySequenceCommandValidator : AbstractValidator<UpdateAssemblySequenceCommand>
{
    public UpdateAssemblySequenceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.WorkOrderId).NotEmpty();
        RuleFor(x => x.Operations).NotEmpty().WithMessage("Operations list cannot be empty");

        RuleFor(x => x.Operations)
            .Must(HaveContinuousSequence)
            .WithMessage("Sequence numbers must be continuous (1, 2, 3, ..., N) with no gaps");

        RuleForEach(x => x.Operations)
            .Must(op => op.Sequence > 0)
            .WithMessage("Sequence numbers must be positive integers");

        RuleForEach(x => x.Operations)
            .Must(op => op.Id != Guid.Empty)
            .WithMessage("Operation ID must be valid");
    }

    /// <summary>
    /// Validates that sequence numbers are continuous [1, 2, 3, ..., N].
    /// </summary>
    private static bool HaveContinuousSequence(List<DTOs.OperationSequenceUpdate> operations)
    {
        if (operations == null || operations.Count == 0)
            return false;

        var sequences = operations.Select(o => o.Sequence).OrderBy(s => s).ToList();

        // Check if sequences start at 1 and are continuous
        for (int i = 0; i < sequences.Count; i++)
        {
            if (sequences[i] != i + 1)
                return false;
        }

        return true;
    }
}
