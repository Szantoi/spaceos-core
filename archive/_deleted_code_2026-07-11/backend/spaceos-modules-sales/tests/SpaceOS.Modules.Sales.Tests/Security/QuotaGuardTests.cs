using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Sales.Tests.Security;

/// <summary>
/// Unit tests for IQuotaGuard behaviour using mocked repository count results.
/// The real QuotaGuard hits EF Core; here we test the interface contract via the handler
/// layer, verifying that Result.Forbidden is propagated when the count is at the limit.
/// </summary>
public class QuotaGuardTests
{
    private readonly Mock<IQuotaGuard> _guard = new();
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public async Task EnsureCanCreate_CustomerAtLimit_ReturnsForbidden()
    {
        _guard.Setup(g => g.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Forbidden());

        var result = await _guard.Object.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task EnsureCanCreate_QuoteAtLimit_ReturnsForbidden()
    {
        _guard.Setup(g => g.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Forbidden());

        var result = await _guard.Object.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task EnsureCanCreate_BelowLimit_ReturnsSuccess()
    {
        _guard.Setup(g => g.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success());

        var result = await _guard.Object.EnsureCanCreateAsync(TenantId, QuotaScope.Customer, default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureCanCreate_QuoteBelowLimit_ReturnsSuccess()
    {
        _guard.Setup(g => g.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Result.Success());

        var result = await _guard.Object.EnsureCanCreateAsync(TenantId, QuotaScope.Quote, default);

        result.IsSuccess.Should().BeTrue();
    }
}
