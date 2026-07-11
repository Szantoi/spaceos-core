using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Abstractions.Application;
using SpaceOS.Modules.Abstractions.Application.Calculation.Commands;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Abstractions.Tests.Calculation;

public class CalculateByNameTests
{
    private static readonly Guid TenantA = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly Mock<IAbstractionsRepository> _repo = new();
    private readonly Mock<IProductCalculationEngine> _engine = new();
    private readonly CalculateByNameHandler _sut;

    public CalculateByNameTests()
    {
        _sut = new CalculateByNameHandler(_repo.Object, _engine.Object);
    }

    [Fact]
    public async Task Handle_TemplateNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetTemplateByNameWithAllAsync("MISSING", TenantA, It.IsAny<CancellationToken>()))
             .ReturnsAsync((ProductTemplate?)null);

        var cmd = new CalculateByNameCommand("MISSING", TenantA, 900, 2100, 40);
        var result = await _sut.Handle(cmd, default);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsForbidden()
    {
        // Template is owned by TenantA, but the command comes in with TenantB.
        // The mock returns the TenantA-owned template regardless (simulating a
        // repo that bypasses RLS, so the handler must enforce the tenant check).
        var ownedByA = ProductTemplate.Create(TenantA, "door", "FAF_T").Value;

        _repo.Setup(r => r.GetTemplateByNameWithAllAsync("FAF_T", TenantB, It.IsAny<CancellationToken>()))
             .ReturnsAsync(ownedByA);

        var cmd = new CalculateByNameCommand("FAF_T", TenantB, 900, 2100, 40);
        var result = await _sut.Handle(cmd, default);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccess()
    {
        var template = ProductTemplate.Create(TenantA, "door", "FAF_T").Value;
        var expectedResult = new CalculationResult(
            template,
            new Dictionary<Guid, ResolvedDimensions>(),
            new List<CuttingListItem>(),
            new Dictionary<string, decimal>());

        _repo.Setup(r => r.GetTemplateByNameWithAllAsync("FAF_T", TenantA, It.IsAny<CancellationToken>()))
             .ReturnsAsync(template);
        _engine.Setup(e => e.Calculate(template, It.IsAny<DimensionInput>(), null))
               .Returns(expectedResult);

        var cmd = new CalculateByNameCommand("FAF_T", TenantA, 900, 2100, 40);
        var result = await _sut.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsEngineWithCorrectDimensions()
    {
        var template = ProductTemplate.Create(TenantA, "door", "FAF_T").Value;
        var expectedResult = new CalculationResult(
            template,
            new Dictionary<Guid, ResolvedDimensions>(),
            new List<CuttingListItem>(),
            new Dictionary<string, decimal>());

        DimensionInput? capturedInput = null;
        _repo.Setup(r => r.GetTemplateByNameWithAllAsync("FAF_T", TenantA, It.IsAny<CancellationToken>()))
             .ReturnsAsync(template);
        _engine.Setup(e => e.Calculate(template, It.IsAny<DimensionInput>(), null))
               .Callback<ProductTemplate, DimensionInput, IReadOnlyDictionary<string, decimal>?>(
                   (_, input, _) => capturedInput = input)
               .Returns(expectedResult);

        var cmd = new CalculateByNameCommand("FAF_T", TenantA, 900m, 2100m, 40m);
        await _sut.Handle(cmd, default);

        capturedInput.Should().NotBeNull();
        capturedInput!.Width.Should().Be(900m);
        capturedInput.Height.Should().Be(2100m);
        capturedInput.Depth.Should().Be(40m);
    }

    [Fact]
    public async Task Handle_TenantCheck_RepositoryCalledWithCorrectTenantId()
    {
        _repo.Setup(r => r.GetTemplateByNameWithAllAsync("FAF_T", TenantA, It.IsAny<CancellationToken>()))
             .ReturnsAsync((ProductTemplate?)null);

        var cmd = new CalculateByNameCommand("FAF_T", TenantA, 900, 2100, 40);
        await _sut.Handle(cmd, default);

        _repo.Verify(r => r.GetTemplateByNameWithAllAsync("FAF_T", TenantA, It.IsAny<CancellationToken>()), Times.Once);
    }
}
