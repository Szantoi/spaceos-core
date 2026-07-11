// SpaceOS.Kernel.Application/UserProfiles/Commands/EraseUserCommandHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.UserProfiles;

namespace SpaceOS.Kernel.Application.UserProfiles.Commands;

/// <summary>
/// Handles <see cref="EraseUserCommand"/>: locates the <see cref="UserProfile"/> for the
/// specified user + tenant, calls <see cref="UserProfile.Erase()"/>, and persists the result.
/// Returns <see cref="Result.NotFound()"/> when no profile exists for the given identity.
/// </summary>
internal sealed class EraseUserCommandHandler : IRequestHandler<EraseUserCommand, Result>
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises a new <see cref="EraseUserCommandHandler"/>.</summary>
    /// <param name="repository">The repository for <see cref="UserProfile"/> entities.</param>
    /// <param name="unitOfWork">The unit of work for committing the erasure.</param>
    public EraseUserCommandHandler(IUserProfileRepository repository, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Executes the GDPR erasure and returns the operation result.</summary>
    public async Task<Result> Handle(EraseUserCommand command, CancellationToken ct)
    {
        var profile = await _repository
            .GetByExternalUserIdAsync(command.ExternalUserId, command.TenantId, ct)
            .ConfigureAwait(false);

        if (profile is null)
            return Result.NotFound();

        profile.Erase();

        await _repository.UpdateAsync(profile, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
