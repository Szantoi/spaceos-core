---
id: MSG-FREETIER-006-DONE
from: freetier
to: root
type: done
priority: high
status: READ
ref: MSG-FREETIER-006
created: 2026-04-23
---

# FREETIER-006 DONE — Infrastructure implementációk + API endpoints (Nap 8.0–12.0)

## Összefoglaló

Teljes Infrastructure réteg + API endpoints implementálva. A FreeTier API funkcionálisan komplett (nesting engine integráció kivételével).

### Implementált komponensek

**Repository implementációk (5):**
| Repository | Megjegyzés |
|---|---|
| `EfFreeTierUserRepository` | AsTracking() mutációkhoz |
| `EfWorkspaceRepository` | Include(Revisions, ShareTokens), CountActive D-23 |
| `EfMagicLinkTokenRepository` | InvalidateAll via ExecuteUpdateAsync |
| `EfUpgradeRequestRepository` | Add + SaveChanges |
| `EfShareTokenRepository` | ShareDbContext via IDbContextFactory (no RLS) |

**Redis infrastruktúra:**
| Komponens | Spec |
|---|---|
| `RedisConnectionFactory` | D-25: AbortOnConnectFail=false, ConnectRetry=3, lazy init |
| `RedisRateLimitService` | D-15: 5 perc bucket, D-18: fail-closed (RateLimitUnavailableException) |
| `RedisSessionStore` | FT-2: sess:{hash}, 600s TTL |

**External service kliensek:**
| Kliens | Megjegyzés |
|---|---|
| `BrevoEmailClient` | api-key env var, dev bypass |
| `TurnstileHttpClient` | dev bypass ha nincs secret |
| `SlackWebhookClient` | fire-and-forget, dev bypass |
| `MagicLinkService` | 32-byte CSPRNG, SHA-256, FixedTimeEquals verify |
| `SystemClock` | IClock implementáció |

**API Endpoints (5 csoport):**
| Csoport | Endpoints |
|---|---|
| Auth | POST /magic-link (202), POST /verify (200/401) |
| Workspace | GET /{id}, POST /, GET /{id}/revisions |
| Share | POST /{id}/share, DELETE /{id}/share/{shareId}, GET /share/{prefix}/{token} (NO AUTH) |
| Upgrade | POST /upgrade |
| Nesting | POST /nest (stub + SemaphoreSlim(10) D-18) |

**DI regisztráció:** Teljes Program.cs — repositories, Redis, external services, IClock, ValidationBehavior, FluentValidation validators

**Workspace EF Config javítás:** HasMany navigáció Revisions + ShareTokens (korábban Ignore volt → Include() nem működött)

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 32, Skipped: 0, Total: 32 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 134 teszt, mind zöld.** (Cél: ≥134 ✅)

Új tesztek (30):
- MagicLinkServiceTests: 5 (generate, hash match, wrong token, different tokens, constant-time)
- TurnstileClientTests: 3 (dev bypass, valid, invalid)
- BrevoEmailClientTests: 2 (no API key skip, with API key call)
- SlackWebhookClientTests: 2 (no webhook skip, with webhook send)
- SystemClockTests: 1 (returns current time)
- RedisRateLimitServiceTests: 2 (IsAllowed fail-closed, Increment fail-closed)
- EndpointTests: 7 (magic link 202, verify 401, workspace 404, shared 400, nest stub 200, healthz 200, revisions 404)
- RepositoryTests: 5 (user add/get, workspace add/get, token add/get, invalidate all, count active)
- RepositoryExtraTests: 3 (upgrade persist, count excludes archived, getById)

## Security review

- **D-18:** `RateLimitUnavailableException` dobás Redis hibánál (fail-closed) ✅ tesztelve
- **D-18:** `SemaphoreSlim(10)` nesting compute guard ✅
- **D-25:** `AbortOnConnectFail=false, ConnectRetry=3, ConnectTimeout=5000, SyncTimeout=1000` ✅
- **D-15:** Redis key pattern `rl:{scope}:{fingerprint}:{bucket}`, 5 perces bucket ✅
- **D-13-REV:** `FixedTimeEquals` a MagicLinkService.Verify-ban ✅ tesztelve
- **SEC-13:** Érzékeny adatok (API key, secret) env var-ból, nem appsettings-ből ✅
- **Token-stripping:** Share endpoint GET `/share/{prefix}/{token}` — public, no auth ✅

## Kockázatok / kérdések

1. **Nesting engine integráció:** A `POST /nest` stub — a nesting algoritmus (SpaceOS.Nesting.Algorithms NuGet) bekötése egy következő task
2. **Auth middleware:** A workspace/share/upgrade endpointok jelenleg nem igényelnek JWT/cookie auth-ot — a UserSessionMiddleware (ft_sess cookie → user context) implementáció a security hardening task-ban lesz
3. **EF Migration:** A Workspace config változás (Ignore → HasMany) ModelSnapshot eltérést okoz — `dotnet ef migrations add` szükséges a frissített snapshot-hoz, de a DB schema nem változik (az FK-k már a raw SQL migration-ban létrehozottak)

Nincsenek blokkoló kockázatok.
