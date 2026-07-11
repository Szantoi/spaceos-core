using FluentAssertions;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;

namespace SpaceOS.Modules.Joinery.Tests.Domain;

/// <summary>
/// Unit tests for <see cref="CuttingListSnapshot"/> factory invariants (SEC-06).
/// </summary>
public class CuttingListSnapshotTests
{
    private static readonly DateTimeOffset FixedTime = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static CuttingListLine MakeLine(int sortOrder = 1) =>
        new("Stile", "Panel", 900m, 2100m, 910m, 2110m, "MDF", 18m, 1, sortOrder);

    private static IReadOnlyList<CuttingListLine> OneValidLine() =>
        new[] { MakeLine() };

    private static CuttingListSnapshot CreateValid(
        Guid? tenantId = null,
        IReadOnlyList<CuttingListLine>? lines = null) =>
        CuttingListSnapshot.Create(
            tenantId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "standard-v1",
            1,
            900m,
            2100m,
            null,
            FixedTime,
            lines ?? OneValidLine());

    // --- Validation: empty lines ---

    [Fact]
    public void Create_WhenLinesIsEmpty_ThrowsArgumentException()
    {
        var emptyLines = Array.Empty<CuttingListLine>();

        var act = () => CuttingListSnapshot.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "standard-v1", 1, 900m, 2100m, null, FixedTime, emptyLines);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one line*");
    }

    [Fact]
    public void Create_WhenLinesIsNull_ThrowsArgumentException()
    {
        var act = () => CuttingListSnapshot.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "standard-v1", 1, 900m, 2100m, null, FixedTime, null!);

        act.Should().Throw<ArgumentException>();
    }

    // --- Validation: lines > 200 ---

    [Fact]
    public void Create_WhenLinesExceed200_ThrowsArgumentException()
    {
        var tooManyLines = Enumerable.Range(1, 201)
            .Select(i => MakeLine(i))
            .ToList();

        var act = () => CuttingListSnapshot.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "standard-v1", 1, 900m, 2100m, null, FixedTime, tooManyLines);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*200 lines*");
    }

    [Fact]
    public void Create_WhenLinesExactly200_Succeeds()
    {
        var lines = Enumerable.Range(1, 200)
            .Select(i => MakeLine(i))
            .ToList();

        var act = () => CuttingListSnapshot.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "standard-v1", 1, 900m, 2100m, null, FixedTime, lines);

        act.Should().NotThrow();
    }

    // --- Happy path ---

    [Fact]
    public void Create_WithValidData_ReturnsSnapshotWithCorrectProperties()
    {
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var snapshot = CuttingListSnapshot.Create(
            tenantId, orderId, itemId,
            "standard-v1", 2,
            850m, 2050m, null, FixedTime, OneValidLine());

        snapshot.TenantId.Should().Be(tenantId);
        snapshot.DoorOrderId.Should().Be(orderId);
        snapshot.DoorItemId.Should().Be(itemId);
        snapshot.TemplateName.Should().Be("standard-v1");
        snapshot.TemplateVersion.Should().Be(2);
        snapshot.InputWidth.Should().Be(850m);
        snapshot.InputHeight.Should().Be(2050m);
        snapshot.CalculatedAt.Should().Be(FixedTime);
        snapshot.Lines.Should().HaveCount(1);
    }

    // --- IsLatest default ---

    [Fact]
    public void Create_IsLatestDefaultsToTrue()
    {
        var snapshot = CreateValid();

        snapshot.IsLatest.Should().BeTrue();
    }

    // --- MarkNotLatest ---

    [Fact]
    public void MarkNotLatest_SetsIsLatestToFalse()
    {
        var snapshot = CreateValid();
        snapshot.IsLatest.Should().BeTrue();

        snapshot.MarkNotLatest();

        snapshot.IsLatest.Should().BeFalse();
    }

    // --- ContentHash binds to TenantId (SEC-06) ---

    [Fact]
    public void ContentHash_DifferentTenantIds_ProduceDifferentHashes()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var line = MakeLine();

        var snapshotA = CuttingListSnapshot.Create(
            tenantA, orderId, itemId, "standard-v1", 1, 900m, 2100m, null, FixedTime, new[] { line });

        var snapshotB = CuttingListSnapshot.Create(
            tenantB, orderId, itemId, "standard-v1", 1, 900m, 2100m, null, FixedTime, new[] { line });

        snapshotA.ContentHash.Should().NotBe(snapshotB.ContentHash,
            because: "TenantId must be bound into the hash to prevent cross-tenant replay (SEC-06)");
    }

    [Fact]
    public void ContentHash_SameTenantAndInputs_ProduceSameHash()
    {
        var tenantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var line = MakeLine();

        var snapshotA = CuttingListSnapshot.Create(
            tenantId, orderId, itemId, "standard-v1", 1, 900m, 2100m, null, FixedTime, new[] { line });

        var snapshotB = CuttingListSnapshot.Create(
            tenantId, orderId, itemId, "standard-v1", 1, 900m, 2100m, null, FixedTime, new[] { line });

        snapshotA.ContentHash.Should().Be(snapshotB.ContentHash,
            because: "identical inputs with the same tenant must always produce the same hash (determinism)");
    }

    // --- ContentHash is non-empty ---

    [Fact]
    public void Create_ContentHashIsNonEmpty()
    {
        var snapshot = CreateValid();

        snapshot.ContentHash.Should().NotBeNullOrWhiteSpace();
    }
}
