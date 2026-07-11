// Identity.Application/Users/Commands/SetOperatorPinCommand.cs

using Ardalis.Result;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record SetOperatorPinCommand(Guid UserId, string? Pin) : IRequest<Result>;

internal sealed class SetOperatorPinCommandHandler : IRequestHandler<SetOperatorPinCommand, Result>
{
    private readonly ISpaceOSUserRepository _repository;

    public SetOperatorPinCommandHandler(ISpaceOSUserRepository repository) => _repository = repository;

    public async Task<Result> Handle(SetOperatorPinCommand request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(SpaceOSUserId.From(request.UserId), ct).ConfigureAwait(false);

        if (user == null)
            return Result.NotFound($"User {request.UserId} not found.");

        try
        {
            var pin = OperatorPin.FromString(request.Pin);
            var (success, error) = user.SetOperatorPin(pin);

            if (!success)
                return Result.Error(new ErrorList([error ?? "Failed to set operator PIN."], null));

            await _repository.UpdateAsync(user, ct).ConfigureAwait(false);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Invalid(new ValidationError
            {
                ErrorMessage = ex.Message,
                Identifier = nameof(request.Pin)
            });
        }
    }
}
