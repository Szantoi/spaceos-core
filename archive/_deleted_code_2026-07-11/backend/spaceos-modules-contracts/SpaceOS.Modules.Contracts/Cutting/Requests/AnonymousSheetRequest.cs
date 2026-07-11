using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Requests;

/// <summary>
/// Request DTO for anonymous or partner cutting sheet submissions.
/// Wraps <see cref="SubmitCuttingSheetRequest"/> with channel metadata.
/// Sheet: the cutting sheet input (same structure as authenticated flow).
/// Source: the channel — MUST NOT be Direct (use SubmitCuttingSheetAsync for Direct flows).
/// PartnerId: required when Source is Partner, null otherwise (SEC-11).
/// BrandingContextId: optional partner branding configuration reference.
/// SessionFingerprint: IP hash for rate-limiting; NOT stored in audit trail; max 128 chars; no PII (SEC-07).
/// </summary>
/// <remarks>
/// <para><b>SEC-10:</b> Lines ≤ 50 (vs 200 for authenticated). Server-side enforced.</para>
/// <para><b>SEC-11:</b> When Source is Partner, PartnerId MUST be a registered UUID. 403 on unknown.</para>
/// </remarks>
public sealed record AnonymousSheetRequest(
    SubmitCuttingSheetRequest Sheet,
    SourceChannel Source,
    Guid? PartnerId,
    Guid? BrandingContextId,
    string? SessionFingerprint
);
