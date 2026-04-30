---
id: MSG-FREETIER-009-DONE
from: freetier
to: root
type: done
priority: high
status: READ
ref: MSG-FREETIER-009
created: 2026-04-23
---

# FREETIER-009 DONE — Portal backend delta (FP-14..17)

## Összefoglaló

Mind a 4 FP implementálva — a FreeTier Frontend portál backend dependenciái teljesek.

### FP-14 — Session check endpoint ✅

`GET /api/freetier/auth/session`
- Ha van érvényes `ft_sess` cookie → Redis session lookup → 200 + `{ userId, authenticatedAt }`
- Ha nincs cookie → 401 Unauthorized

### FP-15 — CORS middleware ✅

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://eszkozok.joinerytech.hu")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
app.UseCors(); // BEFORE UserSessionMiddleware
```

### FP-16 — Cookie domain ✅

`Set-Cookie: ft_sess={nonce}; Domain=.joinerytech.hu; ...`

Konfiguráció: `FreeTier:CookieDomain` appsettings.json-ben
- Development/test: üres (null) → domain nincs beállítva → localhost kompatibilis
- Production (`appsettings.Production.json`): `.joinerytech.hu`

### FP-17 — Logout endpoint ✅

`DELETE /api/freetier/auth/session` → 204 No Content
- Redis session törlés (ha volt cookie)
- Set-Cookie: `ft_sess=; Max-Age=0` (cookie törlés)
- Anonymous path-on van (nem igényel aktív session-t)

## Tesztek

```
Passed!  - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53 - SpaceOS.FreeTier.Application.Tests.dll
Passed!  - Failed: 0, Passed: 64, Skipped: 0, Total: 64 - SpaceOS.FreeTier.Integration.Tests.dll
```

**Összesen: 168 teszt, mind zöld.** (Cél: ≥168 ✅)

Új tesztek (6):
- `GET /auth/session` no cookie → 401
- `GET /auth/session` empty cookie → 401
- `DELETE /auth/session` → 204
- `DELETE /auth/session` clears Set-Cookie header
- `DELETE /auth/session` no cookie → 204 (graceful)
- CORS preflight `eszkozok.joinerytech.hu` origin → 2xx

## Security review

- **FP-15:** `AllowCredentials()` + explicit `WithOrigins()` — NEM `AllowAnyOrigin()` ✅
- **FP-16:** Cookie domain konfiguráció — production `.joinerytech.hu`, dev/test null ✅
- **FP-17:** Logout session törlés Redis-ből + cookie invalidálás ✅
- **Session paths:** `/api/freetier/auth/*` anonymous (UserSessionMiddleware bypass) ✅

## Kockázatok / kérdések

Nincsenek.
