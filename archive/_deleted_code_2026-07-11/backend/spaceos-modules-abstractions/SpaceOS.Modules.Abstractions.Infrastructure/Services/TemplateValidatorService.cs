using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Services;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Services;

public sealed class TemplateValidatorService : ITemplateValidator
{
    // BE-03: three checks — connected graph, exactly 1 root, no orphan slots
    public Result Validate(ProductTemplate template)
    {
        var slots = template.Slots;
        var connections = template.Connections;

        if (slots.Count == 0)
            return Result.Success(); // empty template is valid (no structure to validate)

        var slotIds = new HashSet<Guid>(slots.Select(s => s.Id));
        var childIds = new HashSet<Guid>(connections.Select(c => c.ChildSlotId));
        var parentIds = new HashSet<Guid>(connections.Select(c => c.ParentSlotId));

        // Check 1: Exactly 1 root (slot with no incoming edge)
        var roots = slots.Where(s => !childIds.Contains(s.Id)).ToList();
        if (roots.Count == 0)
            return Result.Invalid(new ValidationError("No root slot: every slot has a parent (cycle?)"));
        if (roots.Count > 1)
            return Result.Invalid(new ValidationError($"Multiple root slots found ({roots.Count}): exactly 1 required"));

        // Check 2: No orphan slots (slot with neither incoming nor outgoing edges, except root)
        foreach (var slot in slots)
        {
            var isRoot = !childIds.Contains(slot.Id);
            var hasOutgoing = parentIds.Contains(slot.Id);
            var hasIncoming = childIds.Contains(slot.Id);

            if (!isRoot && !hasIncoming && !hasOutgoing)
                return Result.Invalid(new ValidationError($"Orphan slot detected: {slot.Name} ({slot.Id})"));
        }

        // Check 3: Connected graph — every slot reachable from root via BFS
        var root = roots[0];
        var adjacency = new Dictionary<Guid, List<Guid>>();
        foreach (var conn in connections)
        {
            if (!adjacency.TryGetValue(conn.ParentSlotId, out var list))
                adjacency[conn.ParentSlotId] = list = new List<Guid>();
            list.Add(conn.ChildSlotId);
        }

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(root.Id);
        visited.Add(root.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (adjacency.TryGetValue(current, out var children))
                foreach (var child in children)
                    if (visited.Add(child)) queue.Enqueue(child);
        }

        if (visited.Count != slotIds.Count)
        {
            var disconnected = slotIds.Except(visited).ToList();
            return Result.Invalid(new ValidationError(
                $"Disconnected graph: {disconnected.Count} slot(s) not reachable from root"));
        }

        return Result.Success();
    }
}
