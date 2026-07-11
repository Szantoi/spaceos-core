// SpaceOS.Kernel.Application/Common/ISecretProvider.cs
namespace SpaceOS.Kernel.Application.Common;

/// <summary>
/// Resolves a secret value by its Key Vault reference name.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Returns the plaintext secret value for the given <paramref name="secretRef"/>.
    /// Returns <c>null</c> if the reference is absent or cannot be resolved.
    /// </summary>
    /// <param name="secretRef">The Key Vault reference name to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default);
}
