// SpaceOS.Kernel.Tests/Application/ChainVerificationResultDtoTests.cs

using SpaceOS.Kernel.Application.AuditLog.Queries;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>
/// Tests for the <see cref="ChainVerificationResultDto"/> record,
/// verifying that the new Phase 3B fields default correctly.
/// </summary>
public sealed class ChainVerificationResultDtoTests
{
    [Fact]
    public void ChainVerificationResultDto_DefaultWormStorageAvailable_IsTrue()
    {
        var dto = new ChainVerificationResultDto(true, 0, null, true);
        Assert.True(dto.WormStorageAvailable);
    }

    [Fact]
    public void ChainVerificationResultDto_DefaultDiagnosticMessage_IsNull()
    {
        var dto = new ChainVerificationResultDto(true, 0, null, true);
        Assert.Null(dto.DiagnosticMessage);
    }

    [Fact]
    public void ChainVerificationResultDto_ExplicitWormUnavailable_IsPreserved()
    {
        var dto = new ChainVerificationResultDto(true, 0, null, true, WormStorageAvailable: false,
            DiagnosticMessage: "WORM unavailable.");
        Assert.False(dto.WormStorageAvailable);
        Assert.Equal("WORM unavailable.", dto.DiagnosticMessage);
    }

    [Fact]
    public void ChainVerificationResultDto_IsRecord_SupportsEquality()
    {
        var d1 = new ChainVerificationResultDto(true, 5, null, true);
        var d2 = new ChainVerificationResultDto(true, 5, null, true);
        Assert.Equal(d1, d2);
    }
}
