// Identity.Tests/Application/ResetPasswordCommandTests.cs

using Identity.Application.Common;
using Identity.Application.Users.Commands;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Identity.Tests.Application;

public sealed class ResetPasswordCommandTests
{
    private readonly Mock<ISpaceOSUserRepository> _repoMock = new();
    private readonly Mock<IKcSyncOutboxRepository> _outboxMock = new();
    private readonly Mock<IRateLimitService> _rateLimiterMock = new();
    private readonly Mock<ICurrentUserContext> _ctxMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private ResetPasswordCommandHandler CreateHandler()
    {
        _ctxMock.Setup(c => c.TenantId).Returns(_tenantId);
        return new ResetPasswordCommandHandler(
            _repoMock.Object, _outboxMock.Object, _rateLimiterMock.Object, _ctxMock.Object);
    }

    private SpaceOSUser MakeUser() =>
        SpaceOSUser.Create(_tenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));

    [Fact]
    public async Task Handle_WithinRateLimit_Succeeds()
    {
        var user = MakeUser();
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _rateLimiterMock.Setup(r => r.TryAcquireAsync(It.IsAny<string>(), 5, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _outboxMock.Verify(o => o.InsertAsync(
            It.Is<KcSyncOutboxEntry>(e => e.Operation == KcSyncOperation.ResetPassword),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SixthAttempt_ReturnsRateLimitExceeded()
    {
        var user = MakeUser();
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        // 6th attempt: rate limiter denies
        _rateLimiterMock.Setup(r => r.TryAcquireAsync(It.IsAny<string>(), 5, TimeSpan.FromHours(1), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(user.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
        Assert.Contains("rate_limit_exceeded", result.Errors);
        _outboxMock.Verify(o => o.InsertAsync(It.IsAny<KcSyncOutboxEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CrossTenantUser_ReturnsForbidden()
    {
        var user = SpaceOSUser.Create(Guid.NewGuid(), Email.From("a@b.com"), DisplayName.From("A", "B"));
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new ResetPasswordCommand(user.Id.Value), CancellationToken.None);

        Assert.Equal(Ardalis.Result.ResultStatus.Forbidden, result.Status);
    }
}
