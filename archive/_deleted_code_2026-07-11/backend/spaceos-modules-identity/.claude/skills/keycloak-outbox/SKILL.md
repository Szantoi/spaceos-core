---
name: keycloak-outbox
description: >
  Keycloak Admin API + Outbox pattern implementációs útmutató az Identity modulhoz.
  Használd amikor: KC Admin API hívásokat írsz, KcSyncWorkerService-t implementálod,
  Outbox tábla kezelést végzel, token cache-t kezelsz, vagy KC sync hibát diagnosztizálsz.
  Lefedi: KeycloakAdminClient, KeycloakTokenProvider, KcSyncWorkerService, Redis token cache, SEC-01 tid assert.
---

# Keycloak Admin API + Outbox Pattern — Identity Modul

## 1. Architekturális invariant

```
KC Admin API hívás SOHA nem történhet DB tranzakción belül.
DB tranzakció: INSERT spaceos_users + INSERT kc_sync_outbox  (atomiáris)
KC hívás: KcSyncWorkerService (BackgroundService, aszinkron, Polly retry)
```

Miért: KC hívás elbukása nem rollbackelheti a DB-t — az Outbox garantálja az eventual consistency-t.

---

## 2. Outbox tábla lifecycle

```
kc_sync_outbox sor STATE:
  processed_at IS NULL + attempt_count = 0  → feldolgozandó (új)
  processed_at IS NULL + attempt_count 1-2  → retry folyamatban
  processed_at IS NULL + attempt_count = 3  → Failed → UserKcSyncFailedEvent
  processed_at IS NOT NULL                  → kész, törölhető
```

**Partial index** — csak a feldolgozatlan sorokat indexeli:
```sql
CREATE INDEX idx_kc_sync_outbox_unprocessed
    ON identity.kc_sync_outbox (created_at)
    WHERE processed_at IS NULL;
```

---

## 3. KcSyncWorkerService mintakód

```csharp
// Identity.Infrastructure/Workers/KcSyncWorkerService.cs
public class KcSyncWorkerService(
    IServiceScopeFactory scopeFactory,
    ILogger<KcSyncWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await ProcessPendingOutboxAsync(ct).ConfigureAwait(false);
            await Task.Delay(PollingInterval, ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessPendingOutboxAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var kcClient = scope.ServiceProvider.GetRequiredService<IIdentityProviderClient>();

        var pending = await db.KcSyncOutbox
            .Where(o => o.ProcessedAt == null && o.AttemptCount < 3)
            .OrderBy(o => o.CreatedAt)
            .Take(50)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var entry in pending)
        {
            try
            {
                await DispatchAsync(entry, kcClient, ct).ConfigureAwait(false);
                entry.ProcessedAt = DateTime.UtcNow;

                var user = await db.SpaceOSUsers.FindAsync([entry.UserId], ct).ConfigureAwait(false);
                if (user is not null) user.KcSyncStatus = KcSyncStatus.Synced;
            }
            catch (Exception ex)
            {
                entry.AttemptCount++;
                entry.LastAttemptAt = DateTime.UtcNow;
                logger.LogWarning(ex, "KC sync failed for outbox {Id}, attempt {N}", entry.Id, entry.AttemptCount);

                if (entry.AttemptCount >= 3)
                {
                    var user = await db.SpaceOSUsers.FindAsync([entry.UserId], ct).ConfigureAwait(false);
                    if (user is not null)
                    {
                        user.KcSyncStatus = KcSyncStatus.Failed;
                        user.AddDomainEvent(new UserKcSyncFailedEvent(user.Id));
                    }
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
```

---

## 4. KeycloakTokenProvider — Redis token cache

```csharp
// Redis key: kc:admin:token
// TTL: expires_in − 60 másodperc (biztonsági margin)
// Titkosítás: AES-256-GCM (SEC-03)

public async Task<string> GetAdminTokenAsync(CancellationToken ct)
{
    var cached = await _redis.StringGetAsync("kc:admin:token").ConfigureAwait(false);
    if (cached.HasValue) return Decrypt(cached!);

    var token = await FetchNewTokenAsync(ct).ConfigureAwait(false);
    var ttl = TimeSpan.FromSeconds(token.ExpiresIn - 60);
    await _redis.StringSetAsync("kc:admin:token", Encrypt(token.AccessToken), ttl).ConfigureAwait(false);
    return token.AccessToken;
}
```

**Redis fallback** (ha Redis elérhetetlen):
```csharp
catch (RedisException ex)
{
    _logger.LogWarning(ex, "Redis unavailable, fetching KC token directly");
    var token = await FetchNewTokenAsync(ct).ConfigureAwait(false);
    return token.AccessToken;
}
```

---

## 5. SEC-01 — tid assert minden KC hívás előtt

```csharp
// KeycloakAdminClient.cs — CreateUserAsync után kötelező:
var created = await GetUserByIdAsync(kcUserId, ct).ConfigureAwait(false);
var kcTid = created.Attributes?.GetValueOrDefault("tid")?.FirstOrDefault();
if (kcTid != tenantId.ToString())
{
    _logger.LogError("KC tid mismatch: expected {Expected}, got {Actual}", tenantId, kcTid);
    throw new IdentityProviderException("tid_assertion_failed");
}
```

---

## 6. Polly pipeline — 3× exponential backoff

```csharp
// Program.cs / DI registration:
services.AddHttpClient<KeycloakAdminClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

---

## 7. IIdentityProviderClient — csak HTTP adapter, nincs business logic

```csharp
// Identity.Domain/Interfaces/IIdentityProviderClient.cs
public interface IIdentityProviderClient
{
    Task<string> CreateUserAsync(KcCreateUserRequest request, CancellationToken ct);
    Task UpdateUserAsync(string kcUserId, KcUpdateUserRequest request, CancellationToken ct);
    Task SetUserEnabledAsync(string kcUserId, bool enabled, CancellationToken ct);
    Task SendPasswordResetEmailAsync(string kcUserId, CancellationToken ct);
    Task<IReadOnlyList<KcUserRepresentation>> GetUsersByTenantAsync(Guid tenantId, int first, int max, CancellationToken ct);
}
```

**TILOS az Infrastructure rétegben:**
- Business döntések (pl. "ha user disabled, ne küldd el")
- Mapping Application DTO → KC request (ez Application réteg feladata)
- Tenant validáció (kivéve a tid assert, ami security gate)

---

## 8. Hibakezelés — IdentityProviderException wrap (SEC-05)

```csharp
// Minden KC hívást wrappolj:
try
{
    await _kcClient.CreateUserAsync(request, ct).ConfigureAwait(false);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "KC Admin API error: {Status}", ex.StatusCode);
    throw new IdentityProviderException("kc_create_failed");  // sanitized, nem proxyzza a KC hibát
}
```

---

## 9. Checklist — KC Outbox implementáció review

- [ ] DB tranzakción belül: CSAK spaceos_users + kc_sync_outbox INSERT
- [ ] KC Admin API hívás: CSAK KcSyncWorkerService-ben
- [ ] tid assert: minden CreateUserAsync után
- [ ] Redis token cache: AES-256 encrypt, TTL = expires_in − 60s
- [ ] Redis fallback: direkt CC grant ha Redis elérheten
- [ ] Polly: 3× retry, exponential backoff
- [ ] SEC-05: KC hibák nem kerülnek a klienshez (IdentityProviderException wrap)
- [ ] audit_log INSERT: minden write művelet után
