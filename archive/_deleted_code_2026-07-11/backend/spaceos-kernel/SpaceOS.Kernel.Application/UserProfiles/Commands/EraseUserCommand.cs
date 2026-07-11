// SpaceOS.Kernel.Application/UserProfiles/Commands/EraseUserCommand.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.UserProfiles.Commands;

/// <summary>
/// Command that erases the PII stored for a user profile, satisfying a GDPR right-to-erasure request.
/// The pseudonym GUID is preserved so audit log entries remain valid; only the
/// external user identifier is replaced with the sentinel value <c>"[ERASED]"</c>.
/// </summary>
/// <param name="ExternalUserId">The JWT <c>sub</c> claim of the user to erase.</param>
/// <param name="TenantId">The tenant the user belongs to.</param>
public sealed record EraseUserCommand(string ExternalUserId, Guid TenantId) : IRequest<Result>;
