using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.Events;

namespace SpaceOS.Modules.Abstractions.Domain.Aggregates;

public sealed class ProductTemplate : TenantScopedEntity
{
    private readonly List<ComponentSlot> _slots = new();
    private readonly List<SlotConnection> _connections = new();
    private readonly List<TemplateParameter> _parameters = new();

    public string TradeType { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ComponentSlot> Slots => _slots.AsReadOnly();
    public IReadOnlyList<SlotConnection> Connections => _connections.AsReadOnly();
    public IReadOnlyList<TemplateParameter> Parameters => _parameters.AsReadOnly();

    private static readonly HashSet<string> AllowedTradeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "door", "cabinet", "window", "generic"
    };

    private ProductTemplate() { }

    public static Result<ProductTemplate> Create(Guid tenantId, string tradeType, string name)
    {
        if (string.IsNullOrWhiteSpace(tradeType))
            return Result<ProductTemplate>.Invalid(new ValidationError("TradeType required"));
        if (!AllowedTradeTypes.Contains(tradeType))
            return Result<ProductTemplate>.Invalid(new ValidationError("TradeType must be one of: door, cabinet, window, generic"));
        if (string.IsNullOrWhiteSpace(name))
            return Result<ProductTemplate>.Invalid(new ValidationError("Name required"));

        var template = new ProductTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TradeType = tradeType.ToLowerInvariant(),
            Name = name,
            Version = 1,
            IsActive = true,
            IsArchived = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        template.AddDomainEvent(new ProductTemplateCreated(template.Id, tenantId, tradeType, name));
        return Result<ProductTemplate>.Success(template);
    }

    public Result<ComponentSlot> AddSlot(
        string name, string componentType, string? defaultMaterial,
        decimal? defaultThickness, int quantity, bool isVirtual,
        SemanticRole? semanticRole, int sortOrder)
    {
        if (_slots.Count >= 200)
            return Result<ComponentSlot>.Error("Maximum 200 slots per template");

        var slot = ComponentSlot.Create(Id, TenantId, name, componentType,
            defaultMaterial, defaultThickness, quantity, isVirtual, semanticRole, sortOrder);
        if (!slot.IsSuccess) return slot;
        _slots.Add(slot.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        return slot;
    }

    public Result<SlotConnection> AddConnection(
        Guid parentSlotId, Guid childSlotId, DimensionAxis axis,
        RuleOperator op, decimal operand, int? multiplierCount,
        Guid? secondaryParentSlotId,
        JointType jointType, MachiningOperation machiningOp, ProcessPhase processPhase,
        decimal? grooveDepth = null, decimal? grooveWidth = null,
        decimal? drillDiameter = null, decimal? drillDepth = null,
        decimal? angle = null, decimal? radius = null,
        string? jointNote = null)
    {
        if (parentSlotId == childSlotId)
            return Result<SlotConnection>.Error("Self-loop forbidden (DB-02)");
        if (!_slots.Any(s => s.Id == parentSlotId))
            return Result<SlotConnection>.Error("Parent slot not in template");
        if (!_slots.Any(s => s.Id == childSlotId))
            return Result<SlotConnection>.Error("Child slot not in template");
        if (_connections.Count >= 500)
            return Result<SlotConnection>.Error("Maximum 500 connections per template");

        // BE-02: write-time iterative BFS cycle check — prevents invalid graphs reaching the DB
        if (HasPath(childSlotId, parentSlotId, _connections))
            return Result<SlotConnection>.Error("Cycle detected at write-time (DB-01)");

        var conn = SlotConnection.Create(Id, TenantId, parentSlotId, childSlotId, axis,
            op, operand, multiplierCount, secondaryParentSlotId,
            jointType, machiningOp, processPhase,
            grooveDepth, grooveWidth, drillDiameter, drillDepth, angle, radius, jointNote);
        if (!conn.IsSuccess) return conn;
        _connections.Add(conn.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        return conn;
    }

    public Result SetParameter(string key, decimal value, string? description = null)
    {
        var existing = _parameters.FirstOrDefault(p => p.Key == key);
        if (existing != null)
        {
            existing.UpdateValue(value);
            UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success();
        }
        if (_parameters.Count >= 100)
            return Result.Error("Maximum 100 parameters per template");

        _parameters.Add(TemplateParameter.Create(Id, TenantId, key, value, description));
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void LoadCollections(
        IEnumerable<ComponentSlot> slots,
        IEnumerable<SlotConnection> connections,
        IEnumerable<TemplateParameter> parameters)
    {
        _slots.AddRange(slots);
        _connections.AddRange(connections);
        _parameters.AddRange(parameters);
    }

    // BE-02: iterative BFS path check — no recursion, no stack overflow risk
    private static bool HasPath(Guid from, Guid to, IReadOnlyList<SlotConnection> connections)
    {
        if (connections.Count == 0) return false;

        var adj = new Dictionary<Guid, List<Guid>>();
        foreach (var c in connections)
        {
            if (!adj.TryGetValue(c.ParentSlotId, out var list))
                adj[c.ParentSlotId] = list = new List<Guid>();
            list.Add(c.ChildSlotId);
        }

        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == to) return true;
            if (!visited.Add(current)) continue;
            if (adj.TryGetValue(current, out var neighbors))
                foreach (var n in neighbors) queue.Enqueue(n);
        }
        return false;
    }
}
