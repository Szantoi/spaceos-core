// Identity.Application/Users/Commands/EnableUserCommand.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record EnableUserCommand(Guid UserId) : IRequest<Result>;

public sealed class EnableUserCommandHandler : IRequestHandler<EnableUserCommand, Result>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public EnableUserCommandHandler(ISpaceOSUserRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(EnableUserCommand request, CancellationToken ct)
    {
        var userId = SpaceOSUserId.From(request.UserId);
        var user = await _repository.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
            return Result.NotFound();

        if (user.TenantId != _currentUser.TenantId)
            return Result.Forbidden();

        var (success, error) = user.Enable();
        if (!success)
            return Result.Error(new ErrorList([error!], null));

        await _repository.UpdateAsync(user, ct).ConfigureAwait(false);
        user.ClearDomainEvents();

        return Result.Success();
    }
}
