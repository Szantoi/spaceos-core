using SpaceOS.Modules.Abstractions.Domain.Common;

namespace SpaceOS.Modules.Abstractions.Domain.Entities;

public sealed class TemplateParameter : TenantScopedEntity
{
    public Guid TemplateId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public string? Description { get; private set; }

    private TemplateParameter() { }

    public static TemplateParameter Create(Guid templateId, Guid tenantId, string key, decimal value, string? description)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 50)
            throw new DomainException("TemplateParameter Key must be 1-50 characters");

        return new TemplateParameter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = templateId,
            Key = key,
            Value = value,
            Description = description
        };
    }

    public void UpdateValue(decimal value) => Value = value;
}
