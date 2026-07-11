// Identity.Application/Users/Commands/CreateUserCommand.cs

using Ardalis.Result;
using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record CreateUserCommand(string Email, string FirstName, string LastName)
    : IRequest<Result<UserDto>>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly IKcSyncOutboxRepository _outbox;
    private readonly ICurrentUserContext _currentUser;

    public CreateUserCommandHandler(
        ISpaceOSUserRepository repository,
        IKcSyncOutboxRepository outbox,
        ICurrentUserContext currentUser)
    {
        _repository = repository;
        _outbox = outbox;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        Email email;
        DisplayName displayName;

        try
        {
            email = Email.From(request.Email);
            displayName = DisplayName.From(request.FirstName, request.LastName);
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Invalid(new ValidationError(ex.Message));
        }

        // Duplicate email check
        var existing = await _repository.GetByEmailAsync(email, _currentUser.TenantId, ct).ConfigureAwait(false);
        if (existing is not null)
            return Result<UserDto>.Conflict("A user with this email already exists in the tenant.");

        var user = SpaceOSUser.Create(_currentUser.TenantId, email, displayName);

        // Atomic: INSERT user + INSERT outbox (Unit of Work enforced at Infrastructure level)
        await _repository.AddAsync(user, ct).ConfigureAwait(false);

        await _outbox.InsertAsync(new KcSyncOutboxEntry
        {
            UserId = user.Id.Value,
            TenantId = user.TenantId,
            Operation = KcSyncOperation.CreateUser
        }, ct).ConfigureAwait(false);

        user.ClearDomainEvents();

        return Result<UserDto>.Created(UserMapper.ToDto(user), $"/identity/users/{user.Id.Value}");
    }
}
