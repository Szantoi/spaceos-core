// Identity.Application/Users/Commands/ClearOperatorPinCommand.cs

using Ardalis.Result;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record ClearOperatorPinCommand(Guid UserId) : IRequest<Result>;

internal sealed class ClearOperatorPinCommandHandler : IRequestHandler<ClearOperatorPinCommand, Result>
{
    private readonly ISpaceOSUserRepository _repository;

    public ClearOperatorPinCommandHandler(ISpaceOSUserRepository repository) => _repository = repository;

    public async Task<Result> Handle(ClearOperatorPinCommand request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(SpaceOSUserId.From(request.UserId), ct).ConfigureAwait(false);

        if (user == null)
            return Result.NotFound($"User {request.UserId} not found.");

        var (success, error) = user.ClearOperatorPin();

        if (!success)
            return Result.Error(new ErrorList([error ?? "Failed to clear operator PIN."], null));

        await _repository.UpdateAsync(user, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
