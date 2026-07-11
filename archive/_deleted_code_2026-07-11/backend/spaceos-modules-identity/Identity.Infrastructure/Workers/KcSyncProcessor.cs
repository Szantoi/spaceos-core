// Identity.Infrastructure/Workers/KcSyncProcessor.cs

using Identity.Application.Common;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Keycloak;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Identity.Infrastructure.Workers;

public sealed class KcSyncProcessor
{
    private const int MaxAttempts = 3;

    private readonly ISpaceOSUserRepository _userRepo;
    private readonly IIdentityProviderClient _idpClient;
    private readonly IKcSyncOutboxProcessor _outboxProcessor;
    private readonly ILogger<KcSyncProcessor> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    public KcSyncProcessor(
        ISpaceOSUserRepository userRepo,
        IIdentityProviderClient idpClient,
        IKcSyncOutboxProcessor outboxProcessor,
        ILogger<KcSyncProcessor> logger,
        ResiliencePipeline? retryPipeline = null)
    {
        _userRepo = userRepo;
        _idpClient = idpClient;
        _outboxProcessor = outboxProcessor;
        _logger = logger;
        _retryPipeline = retryPipeline ?? BuildDefaultPipeline(logger);
    }

    private static ResiliencePipeline BuildDefaultPipeline(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxAttempts - 1,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                OnRetry = args =>
                {
                    logger.LogWarning("KC sync retry {Attempt}: {Ex}",
                        args.AttemptNumber + 1, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

    public async Task ProcessBatchAsync(CancellationToken ct)
    {
        var pending = await _outboxProcessor
            .GetPendingAsync(MaxAttempts, limit: 50, ct).ConfigureAwait(false);

        foreach (var entry in pending)
            await ProcessEntryAsync(entry, ct).ConfigureAwait(false);
    }

    public async Task ProcessEntryAsync(KcSyncOutboxEntry entry, CancellationToken ct)
    {
        var userId = SpaceOSUserId.From(entry.UserId);
        var user = await _userRepo.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning("KcSync: user {UserId} not found, skipping.", entry.UserId);
            await _outboxProcessor.MarkProcessedAsync(entry.Id, ct).ConfigureAwait(false);
            return;
        }

        try
        {
            await _retryPipeline.ExecuteAsync(async token =>
            {
                switch (entry.Operation)
                {
                    case KcSyncOperation.CreateUser:
                        var kcId = await _idpClient.CreateUserAsync(
                            user.TenantId, user.Email, user.DisplayName, token).ConfigureAwait(false);
                        user.MarkKcSynced(kcId);
                        break;

                    case KcSyncOperation.UpdateUser:
                        if (user.KeycloakUserId is not null)
                            await _idpClient.UpdateUserAsync(
                                user.KeycloakUserId, user.DisplayName, token).ConfigureAwait(false);
                        break;

                    case KcSyncOperation.DisableUser:
                        if (user.KeycloakUserId is not null)
                            await _idpClient.DisableUserAsync(user.KeycloakUserId, token).ConfigureAwait(false);
                        break;

                    case KcSyncOperation.EnableUser:
                        if (user.KeycloakUserId is not null)
                            await _idpClient.EnableUserAsync(user.KeycloakUserId, token).ConfigureAwait(false);
                        break;

                    case KcSyncOperation.ResetPassword:
                        if (user.KeycloakUserId is not null)
                            await _idpClient.ResetPasswordAsync(user.KeycloakUserId, token).ConfigureAwait(false);
                        break;
                }
            }, ct).ConfigureAwait(false);

            await _userRepo.UpdateAsync(user, ct).ConfigureAwait(false);
            await _outboxProcessor.MarkProcessedAsync(entry.Id, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KC sync failed: userId={UserId}, op={Op}", entry.UserId, entry.Operation);

            await _outboxProcessor.IncrementAttemptAsync(entry.Id, ct).ConfigureAwait(false);

            if (entry.AttemptCount + 1 >= MaxAttempts)
            {
                user.MarkKcSyncFailed(ex.Message);
                await _userRepo.UpdateAsync(user, ct).ConfigureAwait(false);
            }
        }
    }
}
