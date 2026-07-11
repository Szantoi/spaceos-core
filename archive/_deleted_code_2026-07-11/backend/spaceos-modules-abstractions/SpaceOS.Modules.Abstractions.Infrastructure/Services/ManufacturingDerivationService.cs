using System.Text.RegularExpressions;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Services;

/// <summary>
/// Derives CNC operation plans and process plans from a completed calculation result.
/// </summary>
public sealed class ManufacturingDerivationService : IManufacturingDerivation
{
    private static readonly Regex SlotNameSanitizer =
        new(@"[^a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ0-9 _\-]", RegexOptions.Compiled);

    /// <inheritdoc/>
    public IReadOnlyList<CncOperation> DeriveCncPlan(CalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var operations = new List<CncOperation>();

        foreach (var slotId in result.Dimensions.Keys)
        {
            var slot = result.Template.Slots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null || slot.IsVirtual || slot.ComponentType == "Glass") continue;

            var incomingConnections = result.Template.Connections
                .Where(c => c.ChildSlotId == slot.Id);

            foreach (var conn in incomingConnections)
            {
                if (conn.MachiningOp == MachiningOperation.None) continue;

                operations.Add(new CncOperation(
                    SlotId: slot.Id,
                    SlotName: SanitizeSlotName(slot.Name),
                    Operation: conn.MachiningOp,
                    GrooveDepth: conn.GrooveDepth,
                    GrooveWidth: conn.GrooveWidth,
                    DrillDiameter: conn.DrillDiameter,
                    DrillDepth: conn.DrillDepth,
                    Angle: conn.Angle,
                    Radius: conn.Radius,
                    Note: conn.JointNote));
            }
        }

        return operations;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProductionStep> DeriveProcessPlan(CalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var steps = new List<ProductionStep>();
        var order = 0;

        foreach (var slotId in result.Dimensions.Keys)
        {
            var slot = result.Template.Slots.FirstOrDefault(s => s.Id == slotId);
            if (slot == null) continue;

            var incomingConnections = result.Template.Connections
                .Where(c => c.ChildSlotId == slot.Id)
                .ToList();

            ProcessPhase phase;
            JointType jointType;
            string? note;

            if (incomingConnections.Count == 0)
            {
                // Root slot — no incoming connection
                phase = ProcessPhase.Design;
                jointType = JointType.Offset;
                note = null;
            }
            else
            {
                // Pick first non-Design phase connection, fall back to first connection
                var preferred = incomingConnections.FirstOrDefault(c => c.ProcessPhase != ProcessPhase.Design)
                                ?? incomingConnections[0];
                phase = preferred.ProcessPhase;
                jointType = preferred.JointType;
                note = preferred.JointNote;
            }

            steps.Add(new ProductionStep(
                SlotId: slot.Id,
                SlotName: SanitizeSlotName(slot.Name),
                Phase: phase,
                Order: order++,
                JointType: jointType,
                Note: note));
        }

        return steps;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GlassOrderItem> DeriveGlassOrderItems(CalculationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var items = new List<GlassOrderItem>();

        foreach (var slot in result.Template.Slots)
        {
            if (slot.ComponentType != "Glass") continue;
            if (!result.Dimensions.TryGetValue(slot.Id, out var dims)) continue;

            var description = $"Üveg megrendelés: {dims.Width}×{dims.Height} mm, {slot.Quantity} db";
            items.Add(new GlassOrderItem(
                SlotId: slot.Id,
                SlotName: SanitizeSlotName(slot.Name),
                Width: dims.Width,
                Height: dims.Height,
                Quantity: slot.Quantity,
                OrderDescription: description));
        }

        return items;
    }

    private static string SanitizeSlotName(string name) =>
        SlotNameSanitizer.Replace(name.Length > 100 ? name[..100] : name, string.Empty);
}
