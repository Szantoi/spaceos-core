using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Validation;

public class ComponentSlotTests
{
    private static readonly Guid TemplateId = Guid.NewGuid();
    private static readonly Guid TenantId   = Guid.NewGuid();

    [Fact]
    public void Glass_ComponentType_IsAccepted()
    {
        var result = ComponentSlot.Create(
            TemplateId, TenantId, "Üveg", "Glass",
            defaultMaterial: null, defaultThickness: 6m,
            quantity: 1, isVirtual: false, semanticRole: null, sortOrder: 0);

        result.IsSuccess.Should().BeTrue("Glass is a valid ComponentType");
        result.Value.ComponentType.Should().Be("Glass");
        result.Value.IsVirtual.Should().BeFalse("Glass is a physical component");
    }

    [Fact]
    public void Unknown_ComponentType_IsRejected()
    {
        var result = ComponentSlot.Create(
            TemplateId, TenantId, "Test", "InvalidType",
            defaultMaterial: null, defaultThickness: null,
            quantity: 1, isVirtual: false, semanticRole: null, sortOrder: 0);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage.Contains("InvalidType"));
    }
}
