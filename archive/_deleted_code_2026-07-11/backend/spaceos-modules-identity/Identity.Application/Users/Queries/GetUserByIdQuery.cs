// Identity.Application/Users/Queries/GetUserByIdQuery.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public GetUserByIdQueryHandler(ISpaceOSUserRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var userId = SpaceOSUserId.From(request.UserId);
        var user = await _repository.GetByIdAsync(userId, ct).ConfigureAwait(false);

        if (user is null)
            return Result<UserDto>.NotFound();

        // SEC-02: explicit BOLA guard
        if (user.TenantId != _currentUser.TenantId)
            return Result<UserDto>.Forbidden();

        return Result<UserDto>.Success(UserMapper.ToDto(user));
    }
}
