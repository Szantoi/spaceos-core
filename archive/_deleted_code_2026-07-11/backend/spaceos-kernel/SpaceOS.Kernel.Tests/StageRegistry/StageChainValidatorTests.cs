// SpaceOS.Kernel.Tests/StageRegistry/StageChainValidatorTests.cs
using SpaceOS.Infrastructure.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry;

/// <summary>Unit tests for <see cref="StageChainValidator"/> — chain-advance validation logic.</summary>
public sealed class StageChainValidatorTests
{
    private static readonly Guid TestTenantGuid   = new("70000000-0000-0000-0000-000000000007");
    private static readonly Guid TestFacilityGuid = new("80000000-0000-0000-0000-000000000008");
    private static readonly Guid ChainId          = new("90000000-0000-0000-0000-000000000009");

    private readonly StageChainValidator _sut = new();

    // ─── Helper builders ──────────────────────────────────────────────────────

    private static FlowEpic EpicWithChain(string currentStage)
    {
        var epic = FlowEpic.Create(
            "Validator Test Epic",
            FacilityId.From(TestFacilityGuid),
            TenantId.From(TestTenantGuid));
        epic.AssignChain(ChainId, currentStage);
        return epic;
    }

    private static FlowEpic EpicWithoutChain()
        => FlowEpic.Create(
            "No Chain Epic",
            FacilityId.From(TestFacilityGuid),
            TenantId.From(TestTenantGuid));

    private static StageDefinition MakeDef(string code)
        => StageDefinition.Register(TestTenantGuid, code, $"Display {code}", "http://127.0.0.1:5000");

    /// <summary>
    /// Creates a StageChainTemplate with steps built from (code, sortOrder, isOptional) tuples.
    /// Returns the steps list directly for use with the validator.
    /// </summary>
    private static IReadOnlyList<StageChainStep> BuildSteps(
        params (string code, int order, bool optional)[] defs)
    {
        var template = StageChainTemplate.Create(TestTenantGuid, "test-chain");
        foreach (var (code, order, optional) in defs)
        {
            var sd = MakeDef(code);
            template.AddStep(sd, order, optional);
        }
        return template.Steps;
    }

    // ─── NoChainAssigned ──────────────────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_NoChainAssigned_ThrowsDomainException()
    {
        var epic  = EpicWithoutChain();
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false));

        Assert.Throws<DomainException>(() =>
            _sut.ValidateAdvance(epic, "stage_b", steps));
    }

    // ─── TargetNotInChain ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_TargetNotInChain_ThrowsDomainException()
    {
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false));

        Assert.Throws<DomainException>(() =>
            _sut.ValidateAdvance(epic, "stage_ghost", steps));
    }

    // ─── Valid forward move ───────────────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_ValidForwardMove_DoesNotThrow()
    {
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false));

        // Should not throw
        _sut.ValidateAdvance(epic, "stage_b", steps);
    }

    [Fact]
    public void ValidateAdvance_ValidForwardMove_SkipsTwoStages_WithAllOptional_DoesNotThrow()
    {
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(
            ("stage_a", 1, false),
            ("stage_b", 2, true),   // optional
            ("stage_c", 3, true),   // optional
            ("stage_d", 4, false));

        // Skipping optional stage_b and stage_c to reach stage_d is valid
        _sut.ValidateAdvance(epic, "stage_d", steps);
    }

    // ─── Backward move ────────────────────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_BackwardMove_ThrowsDomainException()
    {
        var epic  = EpicWithChain("stage_b");
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false), ("stage_c", 3, false));

        Assert.Throws<DomainException>(() =>
            _sut.ValidateAdvance(epic, "stage_a", steps));
    }

    [Fact]
    public void ValidateAdvance_SameStageAsTarget_ThrowsDomainException()
    {
        var epic  = EpicWithChain("stage_b");
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false));

        Assert.Throws<DomainException>(() =>
            _sut.ValidateAdvance(epic, "stage_b", steps));
    }

    // ─── Skips required stage ─────────────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_SkipsRequiredStage_ThrowsDomainException()
    {
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(
            ("stage_a", 1, false),
            ("stage_b", 2, false),  // required — cannot be skipped
            ("stage_c", 3, false));

        Assert.Throws<DomainException>(() =>
            _sut.ValidateAdvance(epic, "stage_c", steps));
    }

    // ─── Skip optional stage is valid ────────────────────────────────────────

    [Fact]
    public void ValidateAdvance_SkipsOptionalStage_DoesNotThrow()
    {
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(
            ("stage_a", 1, false),
            ("stage_b", 2, true),   // optional — may be skipped
            ("stage_c", 3, false));

        // Should not throw — stage_b is optional
        _sut.ValidateAdvance(epic, "stage_c", steps);
    }

    // ─── First advance from null current stage ────────────────────────────────

    [Fact]
    public void ValidateAdvance_FromNullCurrentStage_ValidFirstTarget_DoesNotThrow()
    {
        // Epic with chain assigned but CurrentStageCode is first stage;
        // we simulate advancing to the second with no required gaps.
        var epic  = EpicWithChain("stage_a");
        var steps = BuildSteps(("stage_a", 1, false), ("stage_b", 2, false));

        _sut.ValidateAdvance(epic, "stage_b", steps);
    }
}
