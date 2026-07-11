// Identity.Application/Users/Queries/ListTenantUsersQuery.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Application.Users.Specifications;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;

namespace Identity.Application.Users.Queries;

public sealed record ListTenantUsersQuery(UserStatus? Status = null) : IRequest<Result<IReadOnlyList<UserDto>>>;

public sealed class ListTenantUsersQueryHandler
    : IRequestHandler<ListTenantUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public ListTenantUsersQueryHandler(ISpaceOSUserRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> Handle(ListTenantUsersQuery request, CancellationToken ct)
    {
        var users = await _repository.ListByTenantAsync(_currentUser.TenantId, ct).ConfigureAwait(false);

        var filtered = request.Status.HasValue
            ? users.Where(u => u.Status == request.Status.Value).ToList()
            : users.ToList();

        return Result<IReadOnlyList<UserDto>>.Success(
            filtered.Select(UserMapper.ToDto).ToList().AsReadOnly());
    }
}
