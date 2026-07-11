using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Domain tests for Gyartasilap aggregate — creation, status transitions, validation.
/// </summary>
public class GyartasilapTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid JoineryOrderId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_SucceedsAndReturnsDraft()
    {
        var result = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.JoineryOrderId.Should().Be(JoineryOrderId);
        result.Value.LabelVariant.Should().Be("L1");
        result.Value.Status.Should().Be(GyartasilapStatus.Draft);
        result.Value.Version.Should().Be("v1.0");
    }

    [Fact]
    public void Create_WithEmptyOrderId_ReturnsFail()
    {
        var result = Gyartasilap.Create(TenantId, Guid.Empty, null, "L1");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "JoineryOrderId");
    }

    [Theory]
    [InlineData("L1")]
    [InlineData("L2")]
    [InlineData("L3")]
    [InlineData("L4")]
    public void Create_WithValidVariant_Succeeds(string variant)
    {
        var result = Gyartasilap.Create(TenantId, JoineryOrderId, null, variant);

        result.IsSuccess.Should().BeTrue();
        result.Value.LabelVariant.Should().Be(variant);
    }

    [Theory]
    [InlineData("L0")]
    [InlineData("L5")]
    [InlineData("")]
    [InlineData("INVALID")]
    public void Create_WithInvalidVariant_ReturnsFail(string variant)
    {
        var result = Gyartasilap.Create(TenantId, JoineryOrderId, null, variant);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "LabelVariant");
    }

    [Fact]
    public void UpdateStorage_FromDraft_Succeeds()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var storageUrl = "gyartasilap/tenant/plan/gyartasilap_L1.pdf";

        var result = gyartasilap.UpdateStorage(pdfBytes, storageUrl);

        result.IsSuccess.Should().BeTrue();
        gyartasilap.PdfContent.Should().Equal(pdfBytes);
        gyartasilap.StorageUrl.Should().Be(storageUrl);
    }

    [Fact]
    public void Finalize_FromDraft_TransitionsToFinalized()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;

        var result = gyartasilap.Finalize();

        result.IsSuccess.Should().BeTrue();
        gyartasilap.Status.Should().Be(GyartasilapStatus.Finalized);
    }

    [Fact]
    public void Finalize_FromFinalized_ReturnsFail()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;
        gyartasilap.Finalize();

        var result = gyartasilap.Finalize();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void Archive_FromDraft_TransitionsToArchived()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;

        var result = gyartasilap.Archive();

        result.IsSuccess.Should().BeTrue();
        gyartasilap.Status.Should().Be(GyartasilapStatus.Archived);
    }

    [Fact]
    public void Archive_FromFinalized_TransitionsToArchived()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;
        gyartasilap.Finalize();

        var result = gyartasilap.Archive();

        result.IsSuccess.Should().BeTrue();
        gyartasilap.Status.Should().Be(GyartasilapStatus.Archived);
    }

    [Fact]
    public void Archive_FromArchived_ReturnsFail()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;
        gyartasilap.Archive();

        var result = gyartasilap.Archive();

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }

    [Fact]
    public void UpdateStorage_FromArchived_ReturnsFail()
    {
        var gyartasilap = Gyartasilap.Create(TenantId, JoineryOrderId, null, "L1").Value;
        gyartasilap.Archive();

        var result = gyartasilap.UpdateStorage(new byte[] { 1, 2, 3 }, "url");

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "Status");
    }
}
