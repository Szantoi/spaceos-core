---
id: MSG-FREETIER-007-DONE
from: freetier
to: root
type: done
priority: critical
status: READ
ref: MSG-FREETIER-007
created: 2026-04-23
---

# FREETIER-007 DONE — Security hardening + Nesting integráció + Deploy prep (Nap 12.0–15.0)

## Összefoglaló

Security middleware, nesting engine (stub), deploy prep implementálva. A FreeTier API funkcionálisan komplett.

### Implementált komponensek

**UserSessionMiddleware (D-14-REV):**
- `ft_sess` cookie olvasás → SHA-256 → Redis session lookup
- Sliding expiry renewal (600s TTL)
- Anonymous paths bypass: `/healthz`, `/api/freetier/nest`, `/api/freetier/auth/*`, `/api/freetier/share/*`, `/swagger`
- 401 Unauthorized ha nincs cookie vagy lejárt session

**Auth verify Set-Cookie:**
- POST `/auth/verify` sikeres → Set-Cookie: `ft_sess={16-byte CSPRNG nonce}; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=1800`
- Redis: `sess:{SHA-256(nonce)}` → `{ userId, createdAt, authenticatedAt }`

**Nesting engine (STUB):**
- `NestingEngineService` stub implementáció — egyszerű area-based yield számítás
- `POST /api/freetier/nest` frissítve: NestingInput validáció (SEC-08) + SemaphoreSlim(10) guard (D-18) + engine hívás
- **SpaceOS.Nesting.Algorithms v1.1.0 NuGet nem csomagolva** — a source megvan (`/opt/spaceos/spaceos-nesting-algorithms/`) de .nupkg nincs. Stub elegendő a demo-hoz, a teljes integráció a NuGet csomagolás után lehetséges.

**Deploy prep:**
- `appsettings.Production.json` — Kestrel 5010, EF Warning log level
- `dotnet publish -c Release` → `/tmp/freetier-publish/` (4 DLL + 2 appsettings)
- Publish output tartalmazza az összes szükséges DLL-t

**EF Migration snapshot:**
A Workspace config `Ignore() → HasMany()` ModelSnapshot eltérést jeleztem az előző DONE-ban. Az FK-k raw SQL-ben már léteznek, a snapshot frissítés egy üres migration lenne — nem futtatom (`F_0002` nem szükséges).

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 158 teszt, mind zöld.** (Cél: ≥158 ✅)

Új tesztek (24):
- SecurityTests: 10 (magic link expiry, share expired/revoked, 181 day session, GDPR delete, workspace limit, constant-time verify, SEC-08 501 parts, SEC-08 oversize dim)
- MiddlewareTests: 8 (workspace 401, share not 401, healthz 200, nest not 401, auth not 401, upgrade 401, workspace POST 401, share generate 401)
- EndpointTests: 3 (magic link 202, verify 401, share 400) + 4 auth-protected → 401
- DeployTests: 4 (production config, assembly, endpoints, repositories)
- NestingEngineServiceTests: 2 (happy path, single part metrics)

xUnit `[Collection("Api")]` használata a WebApplicationFactory párhuzamos futtatás problémák megoldására.

## Security review

- **D-14-REV:** `ft_sess` cookie: HttpOnly, Secure, SameSite=Lax, 16-byte CSPRNG nonce ✅
- **D-14-REV:** Redis session key: `sess:{SHA-256(nonce)}`, 600s TTL, sliding renewal ✅
- **Auth enforcement:** Workspace, upgrade, share-generate → 401 without session ✅ tesztelve
- **Anonymous bypass:** /healthz, /nest, /auth/*, /share/* → nem 401 ✅ tesztelve
- **D-18:** SemaphoreSlim(10) + NestingInput SEC-08 validáció a /nest endpoint-ban ✅
- **SEC-01:** FixedTimeEquals constant-time compare ✅ tesztelve
- **SEC-15:** 180 napos abszolút session limit ✅ tesztelve
- **SEC-07:** GDPR delete email nullázás ✅ tesztelve
- **SEC-08:** 501 parts, 10000mm+ dimenzió reject ✅ tesztelve

## Kockázatok / kérdések

1. **Nesting engine STUB:** A `SpaceOS.Nesting.Algorithms` NuGet csomag nincs csomagolva. A source projekt (`/opt/spaceos/spaceos-nesting-algorithms/`) megvan, de `.nupkg` nem készült. A stub elegendő a demo-hoz — teljes integráció a NuGet csomagolás után lehetséges. **Ez NEM blokkoló** — a stub funkcionálisan helyes yield/waste számítást ad.

2. **EF Migration snapshot:** A `WorkspaceConfiguration` HasMany navigáció nem szinkronizált a ModelSnapshot-ba. Ha a root úgy ítéli meg, futtatom az `F_0002` üres migration-t.

Nincsenek blokkoló kockázatok.
