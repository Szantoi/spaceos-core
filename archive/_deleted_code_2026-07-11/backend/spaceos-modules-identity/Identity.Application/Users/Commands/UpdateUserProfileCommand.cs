// Identity.Application/Users/Commands/UpdateUserProfileCommand.cs

using Ardalis.Result;
using FluentValidation;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Commands;

public sealed record UpdateUserProfileCommand(Guid UserId, string FirstName, string LastName)
    : IRequest<Result<UserDto>>;

public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public UpdateUserProfileCommandHandler(ISpaceOSUserRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        var userId = SpaceOSUserId.From(request.UserId);
        var user = await _repository.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
            return Result<UserDto>.NotFound();

        if (user.TenantId != _currentUser.TenantId)
            return Result<UserDto>.Forbidden();

        DisplayName displayName;
        try
        {
            displayName = DisplayName.From(request.FirstName, request.LastName);
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Invalid(new ValidationError(ex.Message));
        }

        user.UpdateProfile(displayName);
        await _repository.UpdateAsync(user, ct).ConfigureAwait(false);
        user.ClearDomainEvents();

        return Result<UserDto>.Success(UserMapper.ToDto(user));
    }
}
