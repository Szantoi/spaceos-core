using FluentAssertions;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Security;

public class FilePathTraversalTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _slotId = Guid.NewGuid();

    [Fact]
    public void ValidPath_GeoSlash_DoorFrame_Step_ReturnsSuccess()
    {
        var result = GeometryAttachment.Create(
            _tenantId, _slotId, GeometryLevel.L3_Surface,
            fileReference: "geo/door_frame.step",
            fileFormat: "STEP");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AbsolutePath_StartsWithSlash_ReturnsInvalid()
    {
        var result = GeometryAttachment.Create(
            _tenantId, _slotId, GeometryLevel.L3_Surface,
            fileReference: "/etc/passwd",
            fileFormat: "STEP");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParentTraversal_DotDot_ReturnsInvalid()
    {
        var result = GeometryAttachment.Create(
            _tenantId, _slotId, GeometryLevel.L3_Surface,
            fileReference: "../../etc/passwd.step",
            fileFormat: "STEP");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e =>
            e.ErrorMessage.Contains("traversal") || e.ErrorMessage.Contains("FileReference"));
    }

    [Fact]
    public void NullPath_Allowed_ForOptionalField()
    {
        var result = GeometryAttachment.Create(
            _tenantId, _slotId, GeometryLevel.L0_Parameter,
            fileReference: null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void InvalidExtension_Exe_ReturnsInvalid()
    {
        var result = GeometryAttachment.Create(
            _tenantId, _slotId, GeometryLevel.L3_Surface,
            fileReference: "malicious.exe",
            fileFormat: "STEP");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }
}
