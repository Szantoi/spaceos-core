using FluentAssertions;
using SpaceOS.Modules.Joinery.Application.Products.DTOs;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Infrastructure.Services;

namespace SpaceOS.Modules.Joinery.Tests.Products;

/// <summary>
/// Unit tests for <see cref="ProductConfiguratorService"/>.
/// Tests validation, BOM calculation and pricing logic.
/// </summary>
public class ProductConfiguratorServiceTests
{
    private readonly ProductConfiguratorService _service = new();

    private static ProductTemplate CreateStandardDoorTemplate() => new()
    {
        Id = "standard_door",
        Name = "Standard beltéri ajtó",
        Category = "doors",
        DimensionRules = "{\"minWidth\": 700, \"maxWidth\": 1100, \"minHeight\": 1900, \"maxHeight\": 2200, \"allowedThickness\": [40, 45]}",
        AllowedMaterials = "[{\"id\": \"chipboard_18mm\", \"name\": \"Forgácslap 18mm\", \"type\": \"core\", \"unitPrice\": 8500},{\"id\": \"oak_veneer\", \"name\": \"Tölgy furnér\", \"type\": \"veneer\", \"unitPrice\": 5200},{\"id\": \"pvc_edge_2mm\", \"name\": \"PVC élzáró 2mm\", \"type\": \"edge\", \"unitPrice\": 450}]",
        AllowedFittings = "[{\"id\": \"hidden_3d\", \"name\": \"Rejtett 3D zsanér\", \"category\": \"hinge\", \"unitPrice\": 1200},{\"id\": \"modern_steel\", \"name\": \"Modern acél kilincs\", \"category\": \"handle\", \"unitPrice\": 4500},{\"id\": \"standard_cylinder\", \"name\": \"Standard henger zár\", \"category\": \"lock\", \"unitPrice\": 3200}]",
        PricingRules = "{\"laborRate\": 5000, \"marginPercent\": 15, \"setupCost\": 2000}",
        LeadTimeDays = 7
    };

    // ── Validation Tests ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateConfiguration_WithValidParams_ReturnsSuccess()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfiguration_WithWidthBelowMin_ReturnsError()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(600, 2100, 40); // 600 < 700 min
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Dimensions.Width");
    }

    [Fact]
    public void ValidateConfiguration_WithWidthAboveMax_ReturnsError()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(1200, 2100, 40); // 1200 > 1100 max
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Dimensions.Width");
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidThickness_ReturnsError()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 50); // 50 not in [40, 45]
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Dimensions.Thickness");
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidCoreMaterial_ReturnsError()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("unknown_material", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Materials.Core");
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidHinge_ReturnsError()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("unknown_hinge", "modern_steel", "standard_cylinder");

        // Act
        var result = _service.ValidateConfiguration(template, dimensions, materials, fittings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Fittings.Hinge");
    }

    // ── BOM Calculation Tests ────────────────────────────────────────────────

    [Fact]
    public void CalculateBom_ReturnsCorrectItemCount()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        bom.Should().HaveCount(6); // core, veneer, edge, hinge, handle, lock
    }

    [Fact]
    public void CalculateBom_CoreMaterialHasCorrectQuantity()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        var coreMaterial = bom.FirstOrDefault(i => i.ItemType == "material");
        coreMaterial.Should().NotBeNull();
        coreMaterial!.Quantity.Should().Be(1);
        coreMaterial.Unit.Should().Be("db");
    }

    [Fact]
    public void CalculateBom_VeneerCalculatesAreaInSquareMeters()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        var veneer = bom.FirstOrDefault(i => i.ItemType == "veneer");
        veneer.Should().NotBeNull();
        veneer!.Unit.Should().Be("m²");
        // Expected: (0.9m * 2.1m) * 2 = 3.78 m²
        veneer.Quantity.Should().BeApproximately(3.78m, 0.01m);
    }

    [Fact]
    public void CalculateBom_EdgeCalculatesPerimeterInLinearMeters()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        var edge = bom.FirstOrDefault(i => i.ItemType == "edge");
        edge.Should().NotBeNull();
        edge!.Unit.Should().Be("fm");
        // Expected: (900*2 + 2100*2) / 1000 = 6.0 fm
        edge.Quantity.Should().Be(6.0m);
    }

    [Fact]
    public void CalculateBom_HingeCountIs3ForStandardHeight()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2100, 40);
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        var hinge = bom.FirstOrDefault(i => i.Name.Contains("zsanér"));
        hinge.Should().NotBeNull();
        hinge!.Quantity.Should().Be(3);
    }

    [Fact]
    public void CalculateBom_HingeCountIs4ForTallDoor()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var dimensions = new DimensionsDto(900, 2200, 40); // >2100 mm
        var materials = new MaterialsDto("chipboard_18mm", "oak_veneer", "pvc_edge_2mm");
        var fittings = new FittingsDto("hidden_3d", "modern_steel", "standard_cylinder");

        // Act
        var bom = _service.CalculateBom(template, dimensions, materials, fittings);

        // Assert
        var hinge = bom.FirstOrDefault(i => i.Name.Contains("zsanér"));
        hinge.Should().NotBeNull();
        hinge!.Quantity.Should().Be(4);
    }

    // ── Price Calculation Tests ──────────────────────────────────────────────

    [Fact]
    public void CalculatePrice_IncludesLaborAndMargin()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var bom = new List<BomPreviewItem>
        {
            new("material", "Test Material", 1, "db", 10000, 10000)
        };

        // Act
        var price = _service.CalculatePrice(template, bom);

        // Assert
        // Expected: (10000 materials + 5000 labor + 2000 setup) * 1.15 margin = 19550
        price.Should().Be(19550);
    }

    [Fact]
    public void CalculatePrice_CalculatesCorrectTotalForMultipleItems()
    {
        // Arrange
        var template = CreateStandardDoorTemplate();
        var bom = new List<BomPreviewItem>
        {
            new("material", "Core", 1, "db", 8500, 8500),
            new("veneer", "Veneer", 3.78m, "m²", 5200, 19656),
            new("edge", "Edge", 6, "fm", 450, 2700),
            new("fitting", "Hinge", 3, "db", 1200, 3600),
            new("fitting", "Handle", 1, "db", 4500, 4500),
            new("fitting", "Lock", 1, "db", 3200, 3200)
        };

        // Act
        var price = _service.CalculatePrice(template, bom);

        // Assert
        // Materials: 8500+19656+2700+3600+4500+3200 = 42156
        // Subtotal: 42156 + 5000 (labor) + 2000 (setup) = 49156
        // With 15% margin: 49156 * 1.15 = 56529.4 → rounded to 56529
        price.Should().Be(56529);
    }
}
