// Identity.Application/Users/Queries/GetUsersByRoleQuery.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Application.Common.DTOs;
using Identity.Domain.Interfaces;
using MediatR;

namespace Identity.Application.Users.Queries;

public sealed record GetUsersByRoleQuery(string Role) : IRequest<Result<IReadOnlyList<UserWithRoleDto>>>;

public sealed class GetUsersByRoleQueryHandler
    : IRequestHandler<GetUsersByRoleQuery, Result<IReadOnlyList<UserWithRoleDto>>>
{
    private readonly IIdentityProviderClient _identityProvider;
    private readonly ICurrentUserContext _currentUser;

    // Role whitelist - only valid roles can be queried
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "machine_operator",
        "production_manager",
        "admin"
    };

    public GetUsersByRoleQueryHandler(
        IIdentityProviderClient identityProvider,
        ICurrentUserContext currentUser)
    {
        _identityProvider = identityProvider;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserWithRoleDto>>> Handle(
        GetUsersByRoleQuery request,
        CancellationToken ct)
    {
        // Validate role against whitelist
        if (!ValidRoles.Contains(request.Role))
        {
            return Result<IReadOnlyList<UserWithRoleDto>>.Invalid(
                new ValidationError
                {
                    Identifier = nameof(request.Role),
                    ErrorMessage = $"Invalid role. Allowed roles: {string.Join(", ", ValidRoles)}"
                });
        }

        // Query Keycloak for users with the specified role, filtered by tenant
        var kcUsers = await _identityProvider
            .GetUsersByRoleAsync(_currentUser.TenantId, request.Role, ct)
            .ConfigureAwait(false);

        // Map to DTO
        var result = kcUsers
            .Where(u => Guid.TryParse(u.KcId.Value, out _)) // Filter out invalid GUIDs
            .Select(u => new UserWithRoleDto(
                Id: Guid.Parse(u.KcId.Value),
                Name: $"{u.DisplayName.FirstName} {u.DisplayName.LastName}",
                Email: u.Email.Value,
                Role: u.Role))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<UserWithRoleDto>>.Success(result);
    }
}
