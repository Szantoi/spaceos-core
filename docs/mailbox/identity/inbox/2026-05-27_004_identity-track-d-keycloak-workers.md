---
id: MSG-IDENTITY-004
from: root
to: identity
type: task
priority: high
status: READ
ref: MSG-IDENTITY-001-DONE
created: 2026-05-27
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-IDENTITY-004 — Track D: Keycloak client + Workers + Redis cache

## Kontextus

Track A (Domain) elfogadva. Track D párhuzamosan futhat Track B-vel és C-vel.

Spec: `/opt/spaceos/docs/tasks/active/IDENTITY-V1_modules-identity.md` — §4.3–§4.5
Skills: `.claude/skills/keycloak-outbox/SKILL.md` · `.claude/skills/identity-security/SKILL.md`

## Feladat

Implementáld az **`Identity.Infrastructure/Keycloak`**, **`Workers`** és **`Cache`** rétegeket.

### NuGet csomagok

```bash
cd Identity.Infrastructure
dotnet add package Keycloak.AuthServices.Sdk --version 2.8.0
dotnet add package StackExchange.Redis --version 2.8.16
dotnet add package Polly --version 8.4.1
dotnet add package Polly.Extensions.Http --version 3.0.0
```

### 1. `Keycloak/KeycloakAdminClient.cs`

Implementálja `IIdentityProviderClient`-et. **Csak HTTP adapter — semmi business logic.**

- `CreateUserAsync`: POST `/admin/realms/spaceos/users` → KC user ID visszaolvasás → **SEC-01 tid assert**
- `UpdateUserAsync`: PUT `/admin/realms/spaceos/users/{kcId}`
- `SetUserEnabledAsync`: PUT enabled flag
- `SendPasswordResetEmailAsync`: PUT execute-actions-email `["UPDATE_PASSWORD"]`
- `GetUsersByTenantAsync`: GET users?q=tid:{tenantId} lapozással (max 200/batch)

Minden KC hívás:
- Token: `KeycloakTokenProvider`-tól
- Hiba: `IdentityProviderException`-ba wrappolva (SEC-05 — nem proxyzva a kliensnek)

### 2. `Keycloak/KeycloakTokenProvider.cs`

- Client Credentials grant: `spaceos-identity-service` KC client
- Redis cache: key `kc:admin:token`, TTL = `expires_in − 60s`, **AES-256-GCM titkosítva** (SEC-03)
- Redis fallback: ha Redis elérhetetlen → direkt CC grant, 1× per request

### 3. `Workers/KcSyncWorkerService.cs`

BackgroundService — polling 5 másodpercenként:

```
kc_sync_outbox WHERE processed_at IS NULL AND attempt_count < 3
→ ORDER BY created_at, LIMIT 50
→ foreach: IIdentityProviderClient dispatch (operation alapján)
→ Success: processed_at = now(), spaceos_users.kc_sync_status = Synced, keycloak_user_id = KC ID
→ Failure: attempt_count++, last_attempt_at = now()
           3. kudarc után: kc_sync_status = Failed + UserKcSyncFailedEvent
```

Polly pipeline: 3× retry, exponential backoff (2^n sec) — spec §9, skill részletes mintával.

### 4. `Cache/UserCacheService.cs`

Redis cache-aside, 30s TTL:
- `GetCachedUsersAsync(tenantId)` → cache hit: return; miss: DB query + cache
- `InvalidateAsync(tenantId)` — minden write után hívni

### 5. Tesztek — `Identity.Tests/Infrastructure/`

- `KeycloakAdminClientTests.cs` — mock HttpClient, SEC-01 tid assert (mismatch → exception)
- `UserCacheServiceTests.cs` — cache hit/miss, invalidation
- `KcSyncWorkerServiceTests.cs` — Polly retry, 3× kudarc → Failed státusz + event

## Definition of Done

- [ ] `KcSyncWorkerService` — Polly 3× exponential backoff, `Failed` státusz ha kimerült
- [ ] DB + kc_sync_outbox INSERT atomiáris (egy tranzakcióban) — az Application handler felelős, itt csak az IKcSyncOutboxRepository implementáció kell
- [ ] Redis token cache — TTL = expires_in − 60s; AES-256-GCM titkosítva
- [ ] Redis user lista cache — 30s TTL, write után invalidálás
- [ ] KC Admin API error → `IdentityProviderException` wrap → nem proxyzva (SEC-05)
- [ ] SEC-01: minden `CreateUserAsync` után tid assert
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden teszt zöld (minimum 10 Infrastructure teszt)

## Megjegyzés

- KC admin URL a VPS-en: `http://localhost:8080/auth`
- Realm: `spaceos`
- `spaceos-identity-service` KC client még nem létezik — hozd létre a tesztben mockkal, az INFRA terminál fogja konfigurálni a valódi KC client-et deploy előtt
- AES-256-GCM implementációhoz: `System.Security.Cryptography.AesGcm` — .NET BCL, nincs extra csomag
