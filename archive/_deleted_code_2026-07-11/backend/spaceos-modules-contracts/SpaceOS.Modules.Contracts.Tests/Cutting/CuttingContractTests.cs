using System.Text.Json;
using Ardalis.Result;
using Xunit;
using FluentAssertions;
using SpaceOS.Modules.Contracts.Cutting;
using SpaceOS.Modules.Contracts.Cutting.DTOs;
using SpaceOS.Modules.Contracts.Cutting.Enums;
using SpaceOS.Modules.Contracts.Cutting.Requests;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Tests.Cutting;

public class CuttingContractTests
{
    [Fact]
    public void CuttingSheetDto_RecordEquality_WithSameValues()
    {
        var lines = new List<CuttingLineDto>
        {
            new("Door", "Panel", 800m, 2000m, 803m, 2003m, "MDF18", 18m, 1, 1, false),
        };
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var calculatedAt = DateTimeOffset.UtcNow;

        var dto1 = new CuttingSheetDto(
            id, tenantId, sourceId, "Joinery", null, "DoorTemplate", 1,
            800m, 2000m, null, "abc123", calculatedAt, CuttingSheetStatus.Received,
            lines, [], []);

        var dto2 = new CuttingSheetDto(
            id, tenantId, sourceId, "Joinery", null, "DoorTemplate", 1,
            800m, 2000m, null, "abc123", calculatedAt, CuttingSheetStatus.Received,
            lines, [], []);

        dto1.Should().Be(dto2);
    }

    [Fact]
    public void CuttingLineDto_CanRotate_DefaultFalse()
    {
        var line = new CuttingLineDto("Shelf", "Panel", 600m, 400m, 603m, 403m, "MDF18", 18m, 2, 1, false);

        line.CanRotate.Should().BeFalse();
    }

    [Fact]
    public void PlacedPieceDto_CuttingLineIndex_IsPreserved()
    {
        var piece = new PlacedPieceDto(
            CuttingLineIndex: 3,
            ComponentName: "Shelf",
            X: 10m,
            Y: 20m,
            Width: 600m,
            Height: 400m,
            IsRotated: false);

        piece.CuttingLineIndex.Should().Be(3);
    }

    [Fact]
    public void CuttingExecutionStatus_Serialize_Roundtrip()
    {
        var original = CuttingExecutionStatus.InProgress;

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<CuttingExecutionStatus>(json);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void CuttingSheetStatus_AllValues_AreValid()
    {
        var values = Enum.GetValues<CuttingSheetStatus>();

        values.Should().Contain(CuttingSheetStatus.Received);
        values.Should().Contain(CuttingSheetStatus.Queued);
        values.Should().Contain(CuttingSheetStatus.InNesting);
        values.Should().Contain(CuttingSheetStatus.Nested);
        values.Should().Contain(CuttingSheetStatus.InExecution);
        values.Should().Contain(CuttingSheetStatus.Completed);
        values.Should().HaveCount(6);
    }

    [Fact]
    public void AnonymousSheetRequest_RecordEquality_WithSameValues()
    {
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);
        var partnerId = Guid.NewGuid();

        var req1 = new AnonymousSheetRequest(sheet, SourceChannel.Partner, partnerId, null, "hash123");
        var req2 = new AnonymousSheetRequest(sheet, SourceChannel.Partner, partnerId, null, "hash123");

        req1.Should().Be(req2);
    }

    [Fact]
    public void AnonymousSheetRequest_RecordInequality_WhenSourceDiffers()
    {
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);

        var req1 = new AnonymousSheetRequest(sheet, SourceChannel.FreeTier, null, null, null);
        var req2 = new AnonymousSheetRequest(sheet, SourceChannel.Partner, Guid.NewGuid(), null, null);

        req1.Should().NotBe(req2);
    }

    [Fact]
    public async Task ICuttingProvider_SubmitAnonymousSheetAsync_DimThrowsNotSupportedException()
    {
        ICuttingProvider provider = new StubCuttingProvider();
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);
        var request = new AnonymousSheetRequest(sheet, SourceChannel.FreeTier, null, null, null);

        var act = async () => await provider.SubmitAnonymousSheetAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*CuttingAnonymous*");
    }

    [Theory]
    [InlineData(SourceChannel.Direct)]
    [InlineData(SourceChannel.FreeTier)]
    [InlineData(SourceChannel.Api)]
    public void AnonymousSheetRequest_NonPartnerSource_PartnerIdIsNull(SourceChannel source)
    {
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);

        var request = new AnonymousSheetRequest(sheet, source, null, null, null);

        request.PartnerId.Should().BeNull();
    }

    [Fact]
    public void AnonymousSheetRequest_PartnerSource_HasPartnerId()
    {
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);
        var partnerId = Guid.NewGuid();

        var request = new AnonymousSheetRequest(sheet, SourceChannel.Partner, partnerId, null, null);

        request.PartnerId.Should().Be(partnerId);
        request.Source.Should().Be(SourceChannel.Partner);
    }

    [Fact]
    public void AnonymousSheetRequest_SessionFingerprint_IsPreserved()
    {
        var sheet = new SubmitCuttingSheetRequest(
            Guid.NewGuid(), "Joinery", null, "DoorTpl", 1,
            800m, 2000m, null, [], [], []);

        var request = new AnonymousSheetRequest(sheet, SourceChannel.FreeTier, null, null, "abc123hash");

        request.SessionFingerprint.Should().Be("abc123hash");
    }

    /// <summary>Minimal stub that does not override the DIM.</summary>
    private sealed class StubCuttingProvider : ICuttingProvider
    {
        public string ProviderName => "Stub";
        public ProviderCapability Capabilities => ProviderCapability.CuttingSubmit;
        public Task<bool> HealthCheckAsync(CancellationToken ct) => Task.FromResult(true);
        public Task<Result<Guid>> SubmitCuttingSheetAsync(SubmitCuttingSheetRequest r, CancellationToken ct) => Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));
        public Task<Result<CuttingSheetDto>> GetCuttingSheetAsync(Guid id, CancellationToken ct) => Task.FromResult(Result<CuttingSheetDto>.NotFound());
        public Task<Result<IReadOnlyList<CuttingSheetDto>>> GetCuttingSheetsBySourceAsync(Guid id, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<CuttingSheetDto>>.Success([]));
        public Task<Result<NestingResultDto>> GetNestingResultAsync(Guid id, CancellationToken ct) => Task.FromResult(Result<NestingResultDto>.NotFound());
        public Task<Result<ExecutionStatusDto>> GetExecutionStatusAsync(Guid id, CancellationToken ct) => Task.FromResult(Result<ExecutionStatusDto>.NotFound());
        public Task<Result<WasteReportDto>> GetWasteReportAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct) => Task.FromResult(Result<WasteReportDto>.NotFound());
    }
}
