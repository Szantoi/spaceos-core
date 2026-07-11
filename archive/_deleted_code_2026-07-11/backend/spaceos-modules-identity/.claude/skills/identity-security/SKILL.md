---
name: identity-security
description: >
  Identity modul biztonsági mintái és guard-ok. Használd amikor: tenant izolációt implementálsz,
  BOLA guard-ot írsz, rate limiting-et kezelsz, RLS-t konfigurálsz, vagy security review-t végzel.
  Lefedi: SEC-01–SEC-09 findingek, defense-in-depth tenant guard, rate limiting Redis sliding window,
  PII masking Serilog-ban, audit log pattern.
---

# Identity Modul — Biztonsági Minták

## 1. Defense-in-depth — 3 rétegű tenant izolálás

```
Réteg 1 — RLS (DB szint):
  tenant_id = current_setting('app.current_tenant_id')::UUID
  → minden SELECT/UPDATE automatikusan szűrt

Réteg 2 — Handler guard (Application szint, SEC-02):
  if (user.TenantId != _currentUser.TenantId)
      return Result.Forbidden();

Réteg 3 — KC client assert (Infrastructure szint, SEC-01):
  assert(kc_user.tid == currentUser.TenantId)
  → mismatch → IdentityProviderException
```

**Mind a 3 réteg kötelező** — egyetlen réteg megkerülése security incident.

---

## 2. Explicit tenant guard minden GET handler-ben (SEC-02 — BOLA)

```csharp
// GetUserByIdQueryHandler.cs
public async Task<Result<UserDto>> Handle(GetUserByIdQuery query, CancellationToken ct)
{
    var user = await _repo.GetByIdAsync(query.UserId, ct).ConfigureAwait(false);
    if (user is null) return Result.NotFound();

    // SEC-02: explicit BOLA check — RLS is véd, de explicit guard is kötelező
    if (user.TenantId != _currentUser.TenantId)
        return Result.Forbidden();

    return Result.Success(UserDto.From(user));
}
```

---

## 3. Rate limiting — reset-password (SEC-04)

```csharp
// Redis key: rl:reset:{userId}
// Window: 1 hour sliding
// Limit: 5 attempts
// Response: 429 RFC 7807

// ResetPasswordCommandHandler:
var key = $"rl:reset:{command.UserId}";
var count = await _redis.StringIncrementAsync(key).ConfigureAwait(false);
if (count == 1) await _redis.KeyExpireAsync(key, TimeSpan.FromHours(1)).ConfigureAwait(false);
if (count > 5) return Result.Error("rate_limit_exceeded");  // → 429 a controller-ben
```

---

## 4. RLS SET LOCAL — minden mutating repository metódusban (DB-05)

```csharp
// SpaceOSUserRepository.cs — minden write metódus elején:
await _db.Database.ExecuteSqlRawAsync(
    "SET LOCAL app.current_tenant_id = {0}",
    tenantId.ToString()).ConfigureAwait(false);
```

`SET LOCAL` (nem `SET`): csak az aktuális tranzakcióra érvényes — connection pooling biztonságos.

---

## 5. PII masking — Serilog (SEC-08)

```csharp
// Program.cs Serilog konfig:
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<SpaceOSUser>(u => new
    {
        u.Id,
        u.TenantId,
        email_masked = MaskEmail(u.Email.Value),  // "j***@example.com"
        u.Status,
        u.KcSyncStatus
    })
    .WriteTo.Console()
    .CreateLogger();

private static string MaskEmail(string email)
{
    var at = email.IndexOf('@');
    return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
}
```

---

## 6. Audit log — minden write handler-ben (SEC-07)

```csharp
// Minden CreateUser/UpdateProfile/Disable/Enable/ResetPassword handler végén:
await _db.AuditLogs.AddAsync(new IdentityAuditLog
{
    TenantId = _currentUser.TenantId,
    ActorUserId = _currentUser.UserId,
    Action = "UserCreated",  // vagy "UserDisabled", "PasswordResetRequested", stb.
    TargetUserId = newUser.Id.Value,
    OccurredAt = DateTime.UtcNow,
    Metadata = JsonSerializer.SerializeToElement(new { email_masked = maskedEmail })
}, ct).ConfigureAwait(false);
```

---

## 7. SyncFromKeycloak — tid assert + skip (SEC-06)

```csharp
// SyncTenantUsersFromKeycloakCommandHandler.cs:
foreach (var kcUser in kcUsers)
{
    var kcTid = kcUser.Attributes?.GetValueOrDefault("tid")?.FirstOrDefault();
    if (kcTid != command.TenantId.ToString())
    {
        _logger.LogWarning("Skipping KC user {KcId}: tid mismatch (expected {Expected}, got {Actual})",
            kcUser.Id, command.TenantId, kcTid);
        continue;  // skip + warn — nem throw, nem import
    }
    // ... upsert
}
```

---

## 8. `tid` claim — kizárólag JWT-ből (SEC-09)

```csharp
// CurrentUserContext.cs:
public Guid TenantId => Guid.Parse(
    _httpContextAccessor.HttpContext!.User.FindFirstValue("tid")
    ?? throw new UnauthorizedAccessException("tid claim missing"));
```

**TILOS:**
- `tid` header-ből olvasni
- `tid` request body-ból elfogadni
- `tid` query string-ből olvasni

---

## 9. Security review checklist — minden PR előtt

- [ ] Minden handler: explicit `user.TenantId != currentUser.TenantId` guard?
- [ ] Minden write: `SET LOCAL app.current_tenant_id` hívás?
- [ ] `tid` csak JWT `_currentUser.TenantId`-ből jön?
- [ ] KC hibák wrappolva `IdentityProviderException`-ba (nem proxyzva)?
- [ ] Email nem plaintext a logban?
- [ ] `audit_log` INSERT minden write handler végén?
- [ ] `reset-password`: Redis sliding window rate limit?
- [ ] `sync-from-keycloak`: `[Authorize(Policy = "SuperAdmin")]` + tid assert?
- [ ] P0-1 blocker: HS256 → RS256 migráció ELŐTT nem kerül production GA-ra!
