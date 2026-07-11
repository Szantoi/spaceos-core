// SpaceOS.Kernel.Application/UserProfiles/Pseudonymizer.cs

using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.UserProfiles;

namespace SpaceOS.Kernel.Application.UserProfiles;

/// <summary>
/// Application-layer implementation of <see cref="IPseudonymizer"/>.
/// Creates a new <see cref="UserProfile"/> on first encounter and returns the cached pseudonym
/// on subsequent calls within the same tenant scope.
/// </summary>
internal sealed class Pseudonymizer : IPseudonymizer
{
    private readonly IUserProfileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises a new <see cref="Pseudonymizer"/>.</summary>
    /// <param name="repository">The repository for <see cref="UserProfile"/> entities.</param>
    /// <param name="unitOfWork">The unit of work for persisting new profiles.</param>
    public Pseudonymizer(IUserProfileRepository repository, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _repository  = repository;
        _unitOfWork  = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<Guid> GetOrCreatePseudonymAsync(
        string externalUserId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            throw new ArgumentException("External user ID cannot be empty.", nameof(externalUserId));

        var existing = await _repository
            .GetByExternalUserIdAsync(externalUserId, tenantId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing.Id;

        var profile = UserProfile.Create(externalUserId, tenantId);
        await _repository.AddAsync(profile, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return profile.Id;
    }
}
