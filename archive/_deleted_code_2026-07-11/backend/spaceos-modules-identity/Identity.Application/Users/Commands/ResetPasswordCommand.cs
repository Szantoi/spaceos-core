// Identity.Application/Users/Commands/ResetPasswordCommand.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record ResetPasswordCommand(Guid UserId) : IRequest<Result>;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly IKcSyncOutboxRepository _outbox;
    private readonly IRateLimitService _rateLimiter;
    private readonly ICurrentUserContext _currentUser;

    private const int MaxResetPerHour = 5;

    public ResetPasswordCommandHandler(
        ISpaceOSUserRepository repository,
        IKcSyncOutboxRepository outbox,
        IRateLimitService rateLimiter,
        ICurrentUserContext currentUser)
    {
        _repository = repository;
        _outbox = outbox;
        _rateLimiter = rateLimiter;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var userId = SpaceOSUserId.From(request.UserId);
        var user = await _repository.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
            return Result.NotFound();

        if (user.TenantId != _currentUser.TenantId)
            return Result.Forbidden();

        // SEC-04: Redis sliding window, 5/user/hour
        var rateLimitKey = $"reset_password:{user.Id.Value}";
        var allowed = await _rateLimiter.TryAcquireAsync(rateLimitKey, MaxResetPerHour, TimeSpan.FromHours(1), ct).ConfigureAwait(false);
        if (!allowed)
            return Result.Error(new ErrorList(["rate_limit_exceeded"], null));

        user.RequestPasswordReset();

        await _outbox.InsertAsync(new KcSyncOutboxEntry
        {
            UserId = user.Id.Value,
            TenantId = user.TenantId,
            Operation = KcSyncOperation.ResetPassword
        }, ct).ConfigureAwait(false);

        user.ClearDomainEvents();

        return Result.Success();
    }
}
