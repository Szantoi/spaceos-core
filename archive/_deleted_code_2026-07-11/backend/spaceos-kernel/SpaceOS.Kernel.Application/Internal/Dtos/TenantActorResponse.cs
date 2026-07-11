namespace SpaceOS.Kernel.Application.Internal.Dtos;

/// <summary>
/// Minimal tenant actor info for cross-module directory lookups (ADR-039 read path).
/// SEC-S-09 INVARIANT: This DTO MUST NOT contain contact, billing, address,
/// tax number, email, phone, or any other PII field.
/// Review gate on every PR that touches this type.
/// </summary>
public sealed record TenantActorResponse(
    Guid TenantId,
    string TenantType,
    string DisplayName,
    bool HasVerifiedHandshakeWithRequester);
