---
id: MSG-FREETIER-005
from: root
to: freetier
type: task
priority: high
status: READ
ref: MSG-FREETIER-003-DONE
created: 2026-04-20
---

# FREETIER-005 — Application réteg (Nap 5.5–8.0)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` Section 8.3, 8.4, 8.5
> **Blokkoló:** FREETIER-003 ✅ DONE

---

## Nap 5.5 — Interfaces + Application skeleton

**Fájlok:** `Application/Common/Interfaces/`

```csharp
public interface IRateLimitService
{
    // fail-closed: ha Redis le van, dobjon RateLimitException-t (D-18)
    Task<bool> IsAllowedAsync(string scope, string fingerprint, CancellationToken ct = default);
    Task IncrementAsync(string scope, string fingerprint, CancellationToken ct = default);
}

public interface IMagicLinkService
{
    Task<(string rawToken, string tokenHash)> GenerateAsync(CancellationToken ct = default);
    bool Verify(string rawToken, string storedHash);
}

public interface IBrevoEmailService
{
    Task SendMagicLinkAsync(string toEmail, string magicLink, CancellationToken ct = default);
    Task SendUpgradeNotificationAsync(string adminEmail, UpgradeRequest request, CancellationToken ct = default);
}

public interface ITurnstileValidator
{
    Task<bool> ValidateAsync(string turnstileToken, CancellationToken ct = default);
}

public interface ILabelStrategyFactory
{
    ILabelStrategy Create(LabelStrategy strategy);
}
```

**Repository interfészek** (`Application/Common/Repositories/`):
```csharp
public interface IFreeTierUserRepository
{
    Task<FreeTierUser?> GetByEmailHashAsync(string emailHash, CancellationToken ct = default);
    Task<FreeTierUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(FreeTierUser user, CancellationToken ct = default);
}

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Workspace>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Workspace workspace, CancellationToken ct = default);
}

public interface IMagicLinkTokenRepository
{
    Task<MagicLinkToken?> GetActiveByEmailHashAsync(string emailHash, CancellationToken ct = default);
    Task AddAsync(MagicLinkToken token, CancellationToken ct = default);
    Task InvalidateAsync(Guid tokenId, CancellationToken ct = default);
}

public interface IUpgradeRequestRepository
{
    Task AddAsync(UpgradeRequest request, CancellationToken ct = default);
}
```

---

## Nap 6.0 — Command Handlers (6 db)

### 1. `RequestMagicLink` (register + send)

```csharp
// Application/Commands/RequestMagicLink/RequestMagicLinkCommand.cs
public record RequestMagicLinkCommand(string Email, string TurnstileToken)
    : IRequest<Result>;

// Validation: email format, max 254 char, nem üres TurnstileToken
// Handler flow:
// 1. ITurnstileValidator.ValidateAsync → Result.Error("turnstile_failed") ha fail
// 2. IRateLimitService.IsAllowedAsync("magic-link", emailHash) — fail-closed (D-18)
// 3. Ha új user: FreeTierUser.Register(emailHash) + IFreeTierUserRepository.Add
// 4. MagicLinkToken.Generate() → tuple (rawToken, hash)
// 5. MagicLinkToken entity persist
// 6. IBrevoEmailService.SendMagicLinkAsync
// 7. SaveChangesAsync (domain events dispatch-elve: UserRegistered, MagicLinkRequested)
// Return: Result (nem adja vissza a tokent — security!)
```

### 2. `VerifyMagicLink`

```csharp
public record VerifyMagicLinkCommand(string RawToken, string EmailHash)
    : IRequest<Result<FreeTierUserActivatedDto>>;

// Flow:
// 1. IMagicLinkTokenRepository.GetActiveByEmailHashAsync
// 2. MagicLink.Expiry ellenőrzés (IClock.UtcNow)
// 3. Constant-time hash comparison (CryptographicOperations.FixedTimeEquals)
// 4. FreeTierUser.Activate(now) → Result.Error ha már aktív/not-pending
// 5. Token invalidálás (MagicLinkToken.Use(now))
// 6. SaveChangesAsync
// Return: Result<FreeTierUserActivatedDto> {UserId, SessionExpiresAt}
// Security: 2. use → 401 (token már lejárt/used)
```

### 3. `SaveWorkspace`

```csharp
public record SaveWorkspaceCommand(Guid UserId, Guid? WorkspaceId, string Name, NestingInput Input, NestingResultSnapshot Result, LabelStrategy Strategy)
    : IRequest<Result<Guid>>;

// Flow:
// 1. Ha WorkspaceId null: Workspace.Create(userId, name)
// 2. Ha WorkspaceId set: repo.GetByIdAsync(id, userId) — 404 ha nem találja
// 3. Workspace limit check: max 20 aktív/user (D-23)
// 4. workspace.Save(input, result, strategy) → WorkspaceRevision
// 5. SaveChangesAsync
// Return: workspaceId
```

### 4. `SubmitUpgradeRequest`

```csharp
public record SubmitUpgradeRequestCommand(Guid UserId, Guid WorkspaceId, string CompanyName, string? VatNumber, string ContactEmail, int? UserCount, int? NestVolume)
    : IRequest<Result<Guid>>;

// Flow:
// 1. UpgradeRequest.Submit(...)
// 2. Repo.Add
// 3. SaveChangesAsync (UpgradeRequested event → IBrevoEmailService notify admin)
// Return: upgradeRequestId
```

### 5. `RevokeShareToken` (BE-16)

```csharp
public record RevokeShareTokenCommand(Guid UserId, Guid WorkspaceId, Guid ShareTokenId)
    : IRequest<Result>;

// Flow:
// 1. Workspace.GetByIdAsync(workspaceId, userId)
// 2. workspace.RevokeShare(shareTokenId)
// 3. SaveChangesAsync
```

### 6. `GenerateShareToken`

```csharp
public record GenerateShareTokenCommand(Guid UserId, Guid WorkspaceId, DateTime? ExpiresAt)
    : IRequest<Result<GeneratedShareTokenDto>>;

// Return: {ShareTokenId, RawToken (SINGLE USE — soha nem tárolni!), ExpiresAt}
// Security: RawToken a dto-ban csak a hívóhoz kerül — DB-ben csak TokenHash+TokenPrefix
```

---

## Nap 7.0 — Query Handlers (3 db)

### 1. `GetWorkspace`

```csharp
public record GetWorkspaceQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<Result<WorkspaceDto>>;
// IWorkspaceRepository.GetByIdAsync(id, userId) — RLS védi, de explicit userId filter is
```

### 2. `GetSharedWorkspace` (ShareDbContext — public, no auth)

```csharp
public record GetSharedWorkspaceQuery(string TokenPrefix, string RawToken)
    : IRequest<Result<SharedWorkspaceDto>>;

// Flow:
// 1. ShareDbContext: ShareToken by TokenPrefix lookup
// 2. Constant-time hash compare RawToken vs TokenHash
// 3. Expiry ellenőrzés
// 4. AccessCount++ (fire-and-forget, BE-18 tech debt documented)
// 5. Return WorkspaceRevision adatok
```

### 3. `GetWorkspaceRevisions` (BE-17)

```csharp
public record GetWorkspaceRevisionsQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<Result<IReadOnlyList<WorkspaceRevisionDto>>>;
```

---

## Nap 7.5 — FluentValidation + ConfigureAwait

**Minden command-on kötelező FluentValidation:**

```csharp
// Példa:
public class RequestMagicLinkCommandValidator : AbstractValidator<RequestMagicLinkCommand>
{
    public RequestMagicLinkCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.TurnstileToken).NotEmpty();
    }
}
```

**`ConfigureAwait(false)` minden `await` után** (Golden Rule — spec 8.4):
```csharp
var user = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
```

**ValidationBehavior pipeline:**
```csharp
// Infrastructure/Behaviors/ValidationBehavior.cs (MediatR IPipelineBehavior<TReq, TRes>)
// Ha validation fail → Result.ValidationError(errors) visszaad, nem throw
```

---

## Nap 8.0 — Tests

**Min. 35 Application handler teszt + 5 carry-over persistence:**

**Handler tesztek (Moq + xUnit):**
- `RequestMagicLink`: happy path, Turnstile fail, rate limit hit, duplicate email
- `VerifyMagicLink`: happy path, expired token, second use → 401, wrong hash
- `SaveWorkspace`: create new, update existing, 21. workspace → fail (D-23)
- `SubmitUpgradeRequest`: happy path, duplicate (same user, same workspace)
- `RevokeShareToken`: happy path, wrong user → 404
- `GetSharedWorkspace`: valid token, expired token, wrong hash

**Carry-over persistence tesztek (+5):**
- `FreeTierDbContext` default `QueryTrackingBehavior.NoTracking` explicit teszt
- `SaveChangesAsync` domain event dispatch: UserRegistered event el lett-e küldve?

**Összesített DoD target:** ≥ 103 teszt (68 előző + 35 új)

---

## Definition of Done

- [ ] 5 interface implementált (`IRateLimitService`, `IMagicLinkService`, `IBrevoEmailService`, `ITurnstileValidator`, `ILabelStrategyFactory`)
- [ ] 4 repository interface implementált
- [ ] 6 command handler (`RequestMagicLink`, `VerifyMagicLink`, `SaveWorkspace`, `SubmitUpgradeRequest`, `RevokeShareToken`, `GenerateShareToken`)
- [ ] 3 query handler (`GetWorkspace`, `GetSharedWorkspace`, `GetWorkspaceRevisions`)
- [ ] FluentValidation minden command-on
- [ ] `ValidationBehavior` MediatR pipeline
- [ ] `ConfigureAwait(false)` minden await-en
- [ ] Constant-time hash compare (`CryptographicOperations.FixedTimeEquals`) magic link + share token verify-ban
- [ ] `QueryTrackingBehavior.NoTracking` explicit integrációs teszt
- [ ] `SaveChangesAsync` domain event dispatch teszt
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 103 pass (68 előző + min 35 új)
- [ ] Outbox DONE üzenet küldve
