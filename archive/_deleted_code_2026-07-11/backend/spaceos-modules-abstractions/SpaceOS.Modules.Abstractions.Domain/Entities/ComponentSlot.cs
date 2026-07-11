using Ardalis.Result;
using SpaceOS.Modules.Abstractions.Domain.Common;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Domain.Entities;

public sealed class ComponentSlot : TenantScopedEntity
{
    private static readonly HashSet<string> AllowedComponentTypes = new(StringComparer.Ordinal)
    {
        "Root", "Frame", "Insert", "Clad", "FrameCore", "Blende", "Coating",
        "Panel", "Shelf", "Back", "Door", "Drawer", "Hardware", "Edge", "Virtual", "Glass"
    };

    public Guid TemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ComponentType { get; private set; } = string.Empty;
    public SemanticRole? SemanticRole { get; private set; }
    public string? DefaultMaterial { get; private set; }
    public decimal? DefaultThickness { get; private set; }
    public int Quantity { get; private set; }
    public bool IsVirtual { get; private set; }
    public int SortOrder { get; private set; }

    private ComponentSlot() { }

    public static Result<ComponentSlot> Create(
        Guid templateId, Guid tenantId, string name, string componentType,
        string? defaultMaterial, decimal? defaultThickness,
        int quantity, bool isVirtual, SemanticRole? semanticRole, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ComponentSlot>.Invalid(new ValidationError("Name is required"));
        if (!AllowedComponentTypes.Contains(componentType))
            return Result<ComponentSlot>.Invalid(new ValidationError($"Invalid ComponentType: {componentType}"));
        if (quantity < 1 || quantity > 100)
            return Result<ComponentSlot>.Invalid(new ValidationError("Quantity must be between 1 and 100"));

        return Result<ComponentSlot>.Success(new ComponentSlot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = templateId,
            Name = name,
            ComponentType = componentType,
            DefaultMaterial = defaultMaterial,
            DefaultThickness = defaultThickness,
            Quantity = quantity,
            IsVirtual = isVirtual,
            SemanticRole = semanticRole,
            SortOrder = sortOrder
        });
    }

    public static IReadOnlySet<string> GetAllowedComponentTypes() => AllowedComponentTypes;
}
