using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Services;

public sealed class GraphCalculationEngine : IProductCalculationEngine
{
    public CalculationResult Calculate(
        ProductTemplate template,
        DimensionInput root,
        IReadOnlyDictionary<string, decimal>? parameterOverrides = null)
    {
        var slots = template.Slots;
        var connections = template.Connections;

        // 1. Build adjacency (parent → children)
        var adjacency = BuildAdjacency(connections);

        // 2. Kahn's iterative topological sort — BE-02 (no recursion)
        var sorted = KahnsTopologicalSort(slots, adjacency);

        // 3. Resolve parameters (template defaults + overrides)
        var parameters = ResolveParameters(template.Parameters, parameterOverrides);

        // 4. Dimension propagation
        var dims = new Dictionary<Guid, ResolvedDimensions>();
        if (sorted.Count == 0) return new CalculationResult(template, dims, Array.Empty<CuttingListItem>(), parameters);

        dims[sorted[0].Id] = new ResolvedDimensions(Round(root.Width), Round(root.Height), Round(root.Depth));

        foreach (var slot in sorted.Skip(1))
        {
            var incoming = connections.Where(c => c.ChildSlotId == slot.Id).ToList();
            dims[slot.Id] = ResolveSlotDimensions(slot, incoming, dims, parameters);
        }

        // 5. CuttingOversize from TemplateParameter — BE-04
        var cuttingOversize = parameters.GetValueOrDefault("CuttingOversize", 0m);

        // 6. CuttingList from physical (non-virtual, non-glass) slots only
        // Glass slots are externally ordered — see ManufacturingDerivationService.DeriveGlassOrderItems
        var cuttingList = slots
            .Where(s => !s.IsVirtual && s.ComponentType != "Glass")
            .Where(s => dims.ContainsKey(s.Id))
            .Select(s => ToCuttingListItem(s, dims[s.Id], cuttingOversize))
            .ToList();

        return new CalculationResult(template, dims, cuttingList, parameters);
    }

    // BE-02: Kahn's algorithm — iterative BFS, stack-safe
    private static IReadOnlyList<ComponentSlot> KahnsTopologicalSort(
        IReadOnlyList<ComponentSlot> slots,
        Dictionary<Guid, List<Guid>> adjacency)
    {
        if (slots.Count == 0) return Array.Empty<ComponentSlot>();

        var inDegree = slots.ToDictionary(s => s.Id, _ => 0);
        foreach (var (_, children) in adjacency)
            foreach (var child in children)
                if (inDegree.ContainsKey(child)) inDegree[child]++;

        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<ComponentSlot>(slots.Count);
        var slotMap = slots.ToDictionary(s => s.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(slotMap[current]);
            if (adjacency.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    inDegree[child]--;
                    if (inDegree[child] == 0) queue.Enqueue(child);
                }
            }
        }

        // DB-01: if not all nodes visited → cycle detected
        if (result.Count != slots.Count)
            throw new DomainException("Cycle detected in product graph — template is invalid");

        return result.AsReadOnly();
    }

    private static Dictionary<Guid, List<Guid>> BuildAdjacency(IReadOnlyList<SlotConnection> connections)
    {
        var adj = new Dictionary<Guid, List<Guid>>();
        foreach (var conn in connections)
        {
            if (!adj.TryGetValue(conn.ParentSlotId, out var list))
                adj[conn.ParentSlotId] = list = new List<Guid>();
            list.Add(conn.ChildSlotId);
        }
        return adj;
    }

    private static ComponentSlot FindRoot(ProductTemplate template, Dictionary<Guid, List<Guid>> adjacency)
    {
        var childIds = new HashSet<Guid>(template.Connections.Select(c => c.ChildSlotId));
        return template.Slots.First(s => !childIds.Contains(s.Id));
    }

    private static IReadOnlyDictionary<string, decimal> ResolveParameters(
        IReadOnlyList<Domain.Entities.TemplateParameter> templateParams,
        IReadOnlyDictionary<string, decimal>? overrides)
    {
        var result = templateParams.ToDictionary(p => p.Key, p => p.Value);
        if (overrides != null)
            foreach (var (k, v) in overrides) result[k] = v;
        return result;
    }

    private static ResolvedDimensions ResolveSlotDimensions(
        ComponentSlot slot,
        List<SlotConnection> incoming,
        Dictionary<Guid, ResolvedDimensions> dims,
        IReadOnlyDictionary<string, decimal> parameters)
    {
        var width = 0m;
        var height = 0m;
        var depth = 0m;

        foreach (var conn in incoming)
        {
            if (!dims.TryGetValue(conn.ParentSlotId, out var parentDims)) continue;

            var parentValue = conn.Axis switch
            {
                DimensionAxis.Width  => parentDims.Width,
                DimensionAxis.Height => parentDims.Height,
                DimensionAxis.Depth  => parentDims.Depth,
                _                    => 0m
            };

            var secondaryValue = conn.SecondaryParentSlotId.HasValue
                && dims.TryGetValue(conn.SecondaryParentSlotId.Value, out var secDims)
                ? GetAxisValue(secDims, conn.Axis)
                : 0m;

            var computed = conn.Operator switch
            {
                RuleOperator.Identity  => parentValue,
                RuleOperator.Subtract  => parentValue - conn.Operand,
                RuleOperator.Add       => parentValue + conn.Operand,
                RuleOperator.SubtractN => parentValue - (conn.Operand * (conn.MultiplierCount ?? 1)),
                RuleOperator.Max       => Math.Max(parentValue, secondaryValue) - conn.Operand,
                RuleOperator.Min       => Math.Min(parentValue, secondaryValue) - conn.Operand,
                RuleOperator.Constant  => conn.Operand,
                _                      => throw new DomainException($"Unknown operator: {conn.Operator}") // SEC-03
            };

            switch (conn.Axis)
            {
                case DimensionAxis.Width:  width  = Round(computed); break;
                case DimensionAxis.Height: height = Round(computed); break;
                case DimensionAxis.Depth:  depth  = Round(computed); break;
            }
        }

        return new ResolvedDimensions(width, height, depth);
    }

    // BE-01: explicit MidpointRounding.AwayFromZero
    private static decimal Round(decimal value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static decimal GetAxisValue(ResolvedDimensions d, DimensionAxis axis) => axis switch
    {
        DimensionAxis.Width  => d.Width,
        DimensionAxis.Height => d.Height,
        DimensionAxis.Depth  => d.Depth,
        _                    => 0m
    };

    private static CuttingListItem ToCuttingListItem(ComponentSlot slot, ResolvedDimensions d, decimal oversize) =>
        new(slot.Id, slot.Name, slot.ComponentType,
            Round(d.Width + oversize), Round(d.Height + oversize), Round(d.Depth + oversize),
            slot.Quantity, slot.DefaultMaterial);
}
