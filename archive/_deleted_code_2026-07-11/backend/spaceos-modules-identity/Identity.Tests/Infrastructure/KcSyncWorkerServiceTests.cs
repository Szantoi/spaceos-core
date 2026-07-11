// Identity.Tests/Infrastructure/KcSyncWorkerServiceTests.cs

using Identity.Application.Common;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Keycloak;
using Identity.Infrastructure.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly;
using Xunit;

namespace Identity.Tests.Infrastructure;

public sealed class KcSyncWorkerServiceTests
{
    private readonly Mock<ISpaceOSUserRepository> _userRepoMock = new();
    private readonly Mock<IIdentityProviderClient> _idpMock = new();
    private readonly Mock<IKcSyncOutboxProcessor> _processorMock = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // No-delay pipeline for deterministic tests
    private static readonly ResiliencePipeline NoWaitPipeline =
        new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant
            })
            .Build();

    private SpaceOSUser MakeUser() =>
        SpaceOSUser.Create(_tenantId, Email.From("a@b.com"), DisplayName.From("A", "B"));

    private KcSyncProcessor CreateProcessor() => new(
        _userRepoMock.Object,
        _idpMock.Object,
        _processorMock.Object,
        NullLogger<KcSyncProcessor>.Instance,
        NoWaitPipeline);

    [Fact]
    public async Task ProcessEntry_SuccessfulSync_MarksEntryProcessedAndSyncsUser()
    {
        var user = MakeUser();
        var kcId = KeycloakUserId.From("kc-123");
        var entry = new KcSyncOutboxEntry
        {
            UserId = user.Id.Value,
            TenantId = _tenantId,
            Operation = KcSyncOperation.CreateUser,
            AttemptCount = 0
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);
        _idpMock.Setup(c => c.CreateUserAsync(
                _tenantId, It.IsAny<Email>(), It.IsAny<DisplayName>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(kcId);

        var processor = CreateProcessor();
        await processor.ProcessEntryAsync(entry, CancellationToken.None);

        Assert.Equal(KcSyncStatus.Synced, user.KcSyncStatus);
        Assert.Equal(kcId, user.KeycloakUserId);
        _processorMock.Verify(p => p.MarkProcessedAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEntry_ThirdFailure_MarksUserFailedAndUpdates()
    {
        var user = MakeUser();
        var entry = new KcSyncOutboxEntry
        {
            UserId = user.Id.Value,
            TenantId = _tenantId,
            Operation = KcSyncOperation.CreateUser,
            AttemptCount = 2 // 3rd attempt (0-indexed: 0,1,2)
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);
        _idpMock.Setup(c => c.CreateUserAsync(
                _tenantId, It.IsAny<Email>(), It.IsAny<DisplayName>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IdentityProviderException("KC unreachable", 503));

        var processor = CreateProcessor();
        await processor.ProcessEntryAsync(entry, CancellationToken.None);

        Assert.Equal(KcSyncStatus.Failed, user.KcSyncStatus);
        _processorMock.Verify(p => p.IncrementAttemptAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessEntry_FirstFailure_IncrementsAttemptDoesNotMarkFailed()
    {
        var user = MakeUser();
        var entry = new KcSyncOutboxEntry
        {
            UserId = user.Id.Value,
            TenantId = _tenantId,
            Operation = KcSyncOperation.CreateUser,
            AttemptCount = 0 // first attempt
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(user);
        _idpMock.Setup(c => c.CreateUserAsync(
                _tenantId, It.IsAny<Email>(), It.IsAny<DisplayName>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IdentityProviderException("KC unreachable", 503));

        var processor = CreateProcessor();
        await processor.ProcessEntryAsync(entry, CancellationToken.None);

        // AttemptCount was 0 → 0+1=1 < 3, should NOT mark failed
        Assert.NotEqual(KcSyncStatus.Failed, user.KcSyncStatus);
        _processorMock.Verify(p => p.IncrementAttemptAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
        _processorMock.Verify(p => p.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessEntry_UserNotFound_MarksEntryProcessedAndSkips()
    {
        var entry = new KcSyncOutboxEntry
        {
            UserId = Guid.NewGuid(),
            TenantId = _tenantId,
            Operation = KcSyncOperation.CreateUser,
            AttemptCount = 0
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<SpaceOSUserId>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((SpaceOSUser?)null);

        var processor = CreateProcessor();
        await processor.ProcessEntryAsync(entry, CancellationToken.None);

        _processorMock.Verify(p => p.MarkProcessedAsync(entry.Id, It.IsAny<CancellationToken>()), Times.Once);
        _idpMock.Verify(c => c.CreateUserAsync(
            It.IsAny<Guid>(), It.IsAny<Email>(), It.IsAny<DisplayName>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
