---
id: MSG-IDENTITY-004-DONE
from: identity
to: root
type: done
priority: high
status: READ
ref: MSG-IDENTITY-004
created: 2026-05-27
---

# MSG-IDENTITY-004-DONE — Track D: Keycloak client + Workers + Redis cache kész

## Összefoglaló

Track D teljes egészében implementálva. Commit: `689d610`

**Létrehozott fájlok:**

| Fájl | Leírás |
|---|---|
| `Infrastructure/Keycloak/KeycloakAdminClient.cs` | `IIdentityProviderClient` impl — plain HttpClient, SEC-01 tid assert |
| `Infrastructure/Keycloak/IKeycloakTokenProvider.cs` | Token provider interfész |
| `Infrastructure/Keycloak/KeycloakTokenProvider.cs` | CC grant, AES-256-GCM Redis cache (SEC-03), Redis fallback |
| `Infrastructure/Keycloak/KeycloakOptions.cs` | Konfiguráció (BaseUrl, Realm, ClientId, ClientSecret) |
| `Infrastructure/Keycloak/IdentityProviderException.cs` | Wrap — SEC-05: nem proxyzva kliensnek |
| `Infrastructure/Keycloak/Models/KcUserRepresentation.cs` | KC request/response DTO |
| `Infrastructure/Keycloak/Models/KcTokenResponse.cs` | |
| `Infrastructure/Workers/KcSyncProcessor.cs` | Polly 3× exponential backoff, Failed státusz 3. kudarcnál |
| `Infrastructure/Workers/KcSyncWorkerService.cs` | BackgroundService polling wrapper (5s) |
| `Infrastructure/Cache/UserCacheService.cs` | Redis cache-aside, 30s TTL, invalidation |
| `Application/Common/IKcSyncOutboxProcessor.cs` | GetPending / MarkProcessed / IncrementAttempt |

## Tesztek

```
Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54, Duration: 492 ms
```

13 új Infrastructure teszt:
- `KeycloakAdminClientTests` (4) — SEC-01 tid mismatch → exception, HTTP 5xx → exception, missing Location header → exception, StatusCode tárolás
- `UserCacheServiceTests` (5) — cache hit/miss, StringSet TTL=30s, KeyDelete, Redis unavailable → null (graceful)
- `KcSyncWorkerServiceTests` (4) — siker → Synced + MarkProcessed, 3. kudarc → Failed, 1. kudarc → AttemptCount++, user not found → skip

## Definition of Done checklist

- [x] `KcSyncWorkerService` — Polly 3× exponential backoff, `Failed` státusz ha kimerült
- [x] `IKcSyncOutboxRepository` implementáció interfész kész (DB impl: Track C)
- [x] Redis token cache — TTL = expires_in − 60s; AES-256-GCM titkosítva (SEC-03)
- [x] Redis user lista cache — 30s TTL, write után invalidálás
- [x] KC Admin API error → `IdentityProviderException` wrap (SEC-05)
- [x] SEC-01: `CreateUserAsync` után tid assert — mismatch → exception
- [x] `dotnet build` → 0 error, 0 warning
- [x] `dotnet test` → 54/54 zöld (13 infra + 20 application + 21 domain)

## Security review

- SEC-01: `CreateUserAsync` után GET user + `tid` attribute ellenőrzés — mismatch → `IdentityProviderException`
- SEC-03: Redis token cache AES-256-GCM titkosítva, nonce randomizálva, tag 16 byte
- SEC-05: Minden KC hiba `IdentityProviderException`-ba wrappolva — belső státuszok nem szivárognak ki
- Redis unavailability: graceful fallback minden esetben (log warning, continue)

## ⚠️ Megjegyzés — package inkompatibilitás

**`Keycloak.AuthServices.Sdk 2.8.0`** a CLAUDE.md approved csomaglistán szerepel, de **net10.0-t igényel** (net8.0-val inkompatibilis). A KC admin klienst plain `HttpClient`-tel implementáltam, ami ekvivalens funkcionalitást nyújt.

**Javasolt döntés root-nak:** Az approved list frissítendő. Opciók:
1. `Keycloak.AuthServices.Sdk 2.5.x` (net8.0 kompatibilis, régebbi API)
2. Maradjon a plain HttpClient (jelenlegi megvalósítás)

## Kockázatok / kérdések

Nincsenek blokkolók. Track C (Persistence / EF Core) párhuzamosan futhat.
