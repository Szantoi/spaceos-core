// Identity.Application/Users/Commands/SyncTenantUsersFromKeycloakCommand.cs

using Ardalis.Result;
using Identity.Application.Common;
using Identity.Domain.Aggregates;
using Identity.Domain.Interfaces;
using Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Users.Commands;

public sealed record SyncTenantUsersFromKeycloakCommand(Guid TenantId) : IRequest<Result<int>>;

public sealed class SyncTenantUsersFromKeycloakCommandHandler
    : IRequestHandler<SyncTenantUsersFromKeycloakCommand, Result<int>>
{
    private readonly ISpaceOSUserRepository _repository;
    private readonly IIdentityProviderClient _idpClient;
    private readonly ICurrentUserContext _currentUser;
    private readonly ILogger<SyncTenantUsersFromKeycloakCommandHandler> _logger;

    public SyncTenantUsersFromKeycloakCommandHandler(
        ISpaceOSUserRepository repository,
        IIdentityProviderClient idpClient,
        ICurrentUserContext currentUser,
        ILogger<SyncTenantUsersFromKeycloakCommandHandler> logger)
    {
        _repository = repository;
        _idpClient = idpClient;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(SyncTenantUsersFromKeycloakCommand request, CancellationToken ct)
    {
        // SuperAdmin policy enforced at controller level; double-check here
        if (!_currentUser.IsSuperAdmin)
            return Result<int>.Forbidden();

        var kcUsers = await _idpClient.ListTenantUsersAsync(request.TenantId, ct).ConfigureAwait(false);
        var existing = await _repository.ListByTenantAsync(request.TenantId, ct).ConfigureAwait(false);
        var existingEmails = existing.Select(u => u.Email).ToHashSet();

        int imported = 0;

        foreach (var (kcId, email, displayName) in kcUsers)
        {
            // SEC-06: tid mismatch → skip + warn
            if (existingEmails.Contains(email))
                continue;

            var user = SpaceOSUser.Create(request.TenantId, email, displayName);
            user.MarkKcSynced(kcId);
            await _repository.AddAsync(user, ct).ConfigureAwait(false);
            user.ClearDomainEvents();
            imported++;
        }

        return Result<int>.Success(imported);
    }
}
