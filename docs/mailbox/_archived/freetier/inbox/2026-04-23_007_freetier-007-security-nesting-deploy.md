---
id: MSG-FREETIER-007
from: root
to: freetier
type: task
priority: critical
status: READ
ref: MSG-FREETIER-006-DONE
created: 2026-04-23
---

# FREETIER-007 — Security hardening + Nesting integráció + Deploy prep (Nap 12.0–15.0)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` Section 6, 8.5
> **Blokkoló:** FREETIER-006 ✅ DONE
> **Használhatsz sub-agent-eket** ha szükséges (csharp-expert, devils-advocate)

---

## Nap 12.0 — UserSessionMiddleware (D-14-REV)

A FREETIER-006 DONE-ban jelzett fő hiány: az endpoint-ok jelenleg nem igényelnek auth-ot.

**Fájl:** `Api/Middleware/UserSessionMiddleware.cs`

```csharp
// 1. Kérés → ft_sess cookie olvasás
// 2. SHA-256(cookie_value) → Redis lookup (sess:{hash})
// 3. Ha valid session → HttpContext.Items["UserId"] = session.UserId
// 4. Ha nincs cookie / lejárt → 401 Unauthorized
// 5. Session renew: sliding expiry reset (600s TTL)
```

**Endpoint auth enforcement:**

| Endpoint csoport | Auth |
|---|---|
| `POST /nest` | Anonymous OK (rate limit véd) |
| `POST /auth/magic-link` | Anonymous OK (Turnstile véd) |
| `POST /auth/verify` | Anonymous OK (token véd) |
| `GET /share/{prefix}/{token}` | Anonymous OK (public share) |
| `GET /healthz` | Anonymous OK |
| Minden más (workspace, upgrade) | **UserSessionMiddleware KÖTELEZŐ** |

**Session cookie beállítás a verify endpoint-ban:**
```csharp
// POST /auth/verify sikeres → Set-Cookie: ft_sess={nonce}; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=1800
// Redis: sess:{SHA256(nonce)} → { userId, createdAt, authenticatedAt }
```

**Tesztek (+8):**
- Valid session → 200
- Missing cookie → 401
- Expired session → 401
- Session renew: TTL reset after request
- Anonymous endpoints (/nest, /auth/*, /share, /healthz) → nem 401

---

## Nap 13.0 — Nesting engine integráció

**`POST /nest` stub → valós implementáció**

A `SpaceOS.Nesting.Algorithms` NuGet csomag (v1.1.0) tartalmazza az FFDH + Guillotine algoritmusokat.

```csharp
// Infrastructure/Nesting/NestingEngineService.cs
public sealed class NestingEngineService
{
    // 1. NestingInput validáció (SEC-08: max 500 parts, 1-10000mm)
    // 2. SemaphoreSlim(10) acquire (D-18)
    // 3. FFDH/Guillotine futtatás
    // 4. NestingResultSnapshot összeállítás
    // 5. SemaphoreSlim release (finally!)
}
```

**NuGet hivatkozás:** Ellenőrizd, hogy a `SpaceOS.Nesting.Algorithms` 1.1.0 elérhető-e. Ha nem, használj stub implementációt és jelezd BLOCKED-ban.

**Tesztek (+3):**
- Nesting happy path (2 sheet, 5 part)
- SEC-08: 501 part → 400
- SemaphoreSlim: 11. concurrent → 429 vagy queue

---

## Nap 14.0 — EF Migration snapshot fix + Security tesztek

### Migration fix

A FREETIER-006 DONE jelzi: Workspace config `Ignore() → HasMany()` ModelSnapshot eltérést okoz.

```bash
dotnet ef migrations add F_0002_WorkspaceNavigationFix \
  --project src/SpaceOS.FreeTier.Infrastructure \
  --startup-project src/SpaceOS.FreeTier.Api
```

Ha az Up() üres (mert a raw SQL migration-ban már léteznek az FK-k), az rendben van — a ModelSnapshot frissítése a lényeg.

### Security tesztek (spec 8.5 — ~25 security teszt target)

| Teszt | Spec ref |
|---|---|
| Magic link 2. használat → 401 | SEC-01 |
| Magic link 15 perc után → 401 (FakeClock) | D-11-REV |
| Rate limit: 4. anonymous nest → 429 | D-15/D-18 |
| Redis down → POST /nest 503 (fail-closed) | D-18 |
| Share token expired → 404 | D-13-REV |
| Share token revoked → 404 | D-13-REV |
| GDPR delete: email null, Redis purge | SEC-07 |
| ExtendSession 181 nap → error (FakeClock) | SEC-15 |
| Workspace limit: 21. → 409 | D-23 |
| Constant-time hash: timing nem függ input-tól | SEC-01 |

**Tesztek (+10):** Security-specifikus integrációs tesztek (Testcontainers + FakeClock)

---

## Nap 15.0 — Deploy előkészítés

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "FreeTierDb": "FROM_ENV"
  },
  "Redis": {
    "ConnectionString": "FROM_ENV"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://127.0.0.1:5010" }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### dotnet publish

```bash
dotnet publish src/SpaceOS.FreeTier.Api -c Release -o /tmp/freetier-publish/
```

### Smoke teszt (healthcheck)

```bash
# Publish output-ból futtatva:
dotnet /tmp/freetier-publish/SpaceOS.FreeTier.Api.dll &
curl -s http://127.0.0.1:5010/healthz
# Expected: {"status":"healthy"}
```

**Tesztek (+3):**
- Production config loads without error
- Publish output contains all DLL-ek
- /healthz 200 a published binary-ból

---

## Definition of Done

- [ ] UserSessionMiddleware (D-14-REV): ft_sess cookie, Redis session, sliding expiry
- [ ] Auth enforcement: workspace/upgrade endpoints → 401 without session
- [ ] Verify endpoint: Set-Cookie ft_sess (HttpOnly, Secure, SameSite=Lax)
- [ ] Nesting engine integráció (SpaceOS.Nesting.Algorithms v1.1.0) VAGY stub+BLOCKED
- [ ] SemaphoreSlim(10) valós compute guard
- [ ] EF Migration snapshot fix (F_0002 ha szükséges)
- [ ] Security tesztek: magic link expiry, rate limit, fail-closed, GDPR, session limit
- [ ] appsettings.Production.json + dotnet publish
- [ ] /healthz 200 a published binary-ból
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 158 pass (134 előző + min 24 új)
- [ ] Outbox DONE üzenet küldve
