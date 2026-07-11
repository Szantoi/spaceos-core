// SpaceOS.Kernel.Application/Auth/Commands/RevokeTokenCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Auth;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.Auth.Commands;

/// <summary>
/// Handles <see cref="RevokeTokenCommand"/>: revokes the specified refresh token.
/// Idempotent — if the token does not exist or is already revoked, returns success (BE-P15-11).
/// </summary>
internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _rtRepo;
    private readonly IUnitOfWork _uow;

    /// <summary>Initialises the handler.</summary>
    public RevokeTokenCommandHandler(IRefreshTokenRepository rtRepo, IUnitOfWork uow)
    {
        ArgumentNullException.ThrowIfNull(rtRepo);
        ArgumentNullException.ThrowIfNull(uow);
        _rtRepo = rtRepo;
        _uow    = uow;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var hash   = RefreshTokenHasher.HashToken(cmd.RefreshToken);
        var stored = await _rtRepo.GetByHashAsync(hash, ct).ConfigureAwait(false);

        // Idempotent: non-existent or already-revoked → 200 OK, not 404 (BE-P15-11).
        if (stored is not null && !stored.IsRevoked)
            stored.Revoke();

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
