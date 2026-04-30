---
id: MSG-FREETIER-009
from: root
to: freetier
type: task
priority: high
status: READ
ref: SpaceOS_FreeTier_Portal_Architecture_v1.md
created: 2026-04-23
---

# FREETIER-009 — Portal backend delta (FP-14..17)

> **Tervdok:** `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` (Architect APPROVED)
> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Blokkoló:** A FreeTier Frontend portál ezen endpointok nélkül NEM tud működni
> **Effort:** ~0.85 nap
> **Használhatsz sub-agent-eket** ha szükséges

---

## FP-14 — Session check endpoint (KRITIKUS)

```csharp
// GET /api/freetier/auth/session → 200 + user JSON, vagy 401
// A frontend page reload után ezzel ellenőrzi a session érvényességét
```

**Endpoint:** `GET /api/freetier/auth/session`
- Ha van érvényes `ft_sess` cookie → Redis session lookup → 200 + `{ userId, email, authenticatedAt }`
- Ha nincs cookie vagy lejárt → 401

**Implementáció:** Új query handler `GetCurrentSession` — a UserSessionMiddleware már parse-olja a cookie-t, csak egy endpoint kell ami visszaadja a session adatait.

---

## FP-15 — CORS middleware config (KRITIKUS)

Az `eszkozok.joinerytech.hu` frontend hívja a `freetier.joinerytech.hu` API-t — ez cross-origin.

**Program.cs kiegészítés:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://eszkozok.joinerytech.hu")
              .AllowCredentials()  // cookie küldés engedélyezése
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// app.UseCors() BEFORE app.UseRouting()
```

**FONTOS:** `AllowCredentials()` + explicit `WithOrigins()` — `AllowAnyOrigin()` NEM kompatibilis a `credentials: include`-dal!

---

## FP-16 — Cookie domain beállítás (KRITIKUS)

A `ft_sess` cookie jelenleg `freetier.joinerytech.hu` domain-re van állítva. A frontend (`eszkozok.joinerytech.hu`) nem kapja meg.

**Fix:** A verify endpoint `Set-Cookie`-jában:
```
Set-Cookie: ft_sess={nonce}; Domain=.joinerytech.hu; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=1800
```

A `Domain=.joinerytech.hu` lehetővé teszi hogy minden `*.joinerytech.hu` subdomain megkapja a cookie-t.

**Érintett fájl:** Valószínűleg az `AuthEndpoints.cs` verify endpoint, ahol a `Set-Cookie` header-t állítod.

---

## FP-17 — Logout endpoint

```csharp
// DELETE /api/freetier/auth/session → 204
// 1. ft_sess cookie olvasás
// 2. Redis session törlés: sess:{SHA256(nonce)} DELETE
// 3. Set-Cookie: ft_sess=; Max-Age=0 (cookie törlés)
// 4. 204 No Content
```

**Implementáció:** Új endpoint az AuthEndpoints-ban. A UserSessionMiddleware-t bypass-olja (anonymous path), mert a cookie törlés nem igényel aktív session-t.

---

## Tesztek

**Min. +6 teszt:**
1. `GET /auth/session` valid cookie → 200 + user JSON
2. `GET /auth/session` no cookie → 401
3. `GET /auth/session` expired session → 401
4. `DELETE /auth/session` → 204 + cookie cleared
5. CORS: `OPTIONS` preflight `eszkozok.joinerytech.hu` origin → 200 + correct headers
6. Cookie domain: verify response `Set-Cookie` tartalmazza `Domain=.joinerytech.hu`

---

## Definition of Done

- [ ] `GET /api/freetier/auth/session` endpoint (FP-14)
- [ ] CORS middleware `eszkozok.joinerytech.hu` origin (FP-15)
- [ ] Cookie `Domain=.joinerytech.hu` a verify endpoint-ban (FP-16)
- [ ] `DELETE /api/freetier/auth/session` logout endpoint (FP-17)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 168 pass (162 előző + min 6 új)
- [ ] Outbox DONE üzenet küldve
