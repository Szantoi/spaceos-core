# SpaceOS — Q2 Pre-launch Security Review

**Dátum:** 2026-04-15  
**Scope:** Kernel · Orchestrator · Portal · Joinery · Abstractions · E2E  
**Módszer:** Statikus kódelemzés + logika review (terminálonként önellenőrzés)  
**Eredmény: 0 kritikus sérülékenység. Launch nem blokkolt.**

---

## Összesített találatok

| # | Terminál | Szint | Azonosító | Leírás | Státusz |
|---|---|---|---|---|---|
| 1 | ORCH | ⚠️ Közepes | ORCH-K1 | `error.middleware.ts` — `err.message` production-ban kiszivárog | Nyitott |
| 2 | ORCH | ⚠️ Közepes | ORCH-K2 | `stageDispatch.route.ts` — SSRF: `stage.moduleEndpoint` URL allowlist nélkül | Nyitott |
| 3 | ORCH | ⚠️ Közepes | ORCH-K3 | `interpreter.service.ts` — user message content szanitizálatlan (prompt inject) | Nyitott |
| 4 | PORTAL | ⚠️ Közepes | PORTAL-M1 | Axios ≤1.14.0 CVE (SSRF bypass, browser ctx közepes kockázat) | VPS Operator |
| 5 | JOINERY | ⚠️ Közepes | JOINERY-M2 | `ValidateAudience = false` — bármely valid token elfogadott | Nyitott |
| 6 | JOINERY | ⚠️ Közepes | JOINERY-M3 | Nincs startup fail-fast ha JWT Authority hiányzik | Nyitott |
| 7 | JOINERY | ✅ Közepes | JOINERY-M1 | `pageSize` unbounded TAKE | **JAVÍTVA** (Math.Clamp — commit pending) |
| 8 | ABSTRACTIONS | ⚠️ Közepes | ABS-M01 | `ValidateAudience = false` | Nyitott |
| 9 | ABSTRACTIONS | ⚠️ Közepes | ABS-M02 | `TenantSessionInterceptor` csak write-path-en fut (read: nincs tenant session) | Nyitott |
| 10 | ABSTRACTIONS | ⚠️ Közepes | ABS-M03 | `GetTemplateAsync` nem szűr TenantId-ra — IDOR handler-szintre bízva | Nyitott |
| 11 | KERNEL | ⚠️ Közepes | KERNEL-M1 | 6 core tábla DB-szintű RLS hiány (EF filter véd app-szinten) | Q3 sprint |
| 12 | E2E | ✅ — | — | Vite CVE-k (3×HIGH) | **JAVÍTVA** (npm audit fix, vite@7.3.2) |

---

## Alacsony prioritású találatok (nem blokkoló)

| Terminál | Leírás |
|---|---|
| KERNEL L1 | `/healthz` → 401 monitoring probe-nál (`.AllowAnonymous()` hiányzik) |
| KERNEL L3 | HSTS + security response headerek hiánya (API, korlátozott hatás) |
| ORCH A1 | `X-SpaceOS-Brand` kliens által felülírható |
| ORCH A2 | `federation.proxy.ts` 500→502 remapping hiányzik |
| ORCH A3 | `CORS_ORIGINS` default localhost ha env nincs beállítva |
| PORTAL L1 | `/callback` route nincs ProtectedRoute mögött (PKCE state nélkül nem exploitable) |
| JOINERY L1 | `Database.MigrateAsync()` production indításkor fut |
| ABSTRACTIONS L1 | `RequireHttpsMetadata = false` (loopback service, elfogadható) |
| E2E L1 | `E2E_TEST_PASSWORD ?? 'test'` fallback (test env, nem production) |

---

## Q2 Launch előtt javítandó (Közepes szint)

### 1. csoport — ValidateAudience (JOINERY + ABSTRACTIONS, azonos pattern)

Mindkét modulban `ValidateAudience = false` → bármely valid Keycloak token elfogadott,
nem csak a `kernel-api` audience-re kiadott.

**Fix:**
```csharp
ValidateAudience = true,
ValidAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
```

### 2. csoport — ORCH biztonsági javítások (K1 + K2 + K3)

- **K1:** `error.middleware.ts` → NODE_ENV check: production-ban `err.message` ne kerüljön a response-ba
- **K2:** `stageDispatch.route.ts` → `stage.moduleEndpoint` URL allowlist: `^http://127\.0\.0\.1:\d+/`
- **K3:** `interpreter.service.ts` → user message content sanitizáció kiterjesztése (legalább `\x00` + control char strip)

### 3. csoport — ABSTRACTIONS read-path RLS (M02 + M03)

- **M02:** `DbCommandInterceptor` vagy explicit `set_config` read query-k előtt is
- **M03:** `GetTemplateAsync(Guid id, Guid tenantId)` — TenantId szűrő a query-ban

### 4. csoport — JOINERY commit + deploy

- JOINERY-M1 (`pageSize` clamp) kód kész, commit + INFRA deploy szükséges

### 5. csoport — Axios CVE (PORTAL)

VPS Operator: `chown` + `pnpm update axios@1.15.0`

---

## Q3 halasztható

- KERNEL M1: 6 core tábla DB-szintű RLS migration (EF filter megvéd app-szinten)
- KERNEL L3: HSTS + security headers
- JOINERY L1: MigrateAsync production-ban
- ORCH A2: federation.proxy.ts 500→502

---

## Pozitívumok (kiemelkedő biztonsági megoldások)

| Terület | Megjegyzés |
|---|---|
| AuditEvent immutability | Csak `AddAsync` + read — UPDATE/DELETE nem létezik interfészen |
| Cross-tenant 404 (nem 403) | Minden modulban konzisztensen — tenant enumeration ellen |
| SHA-256 chain + Sequence BIGINT | Migration 0030 tiebreaker — determinisztikus audit trail |
| SEC-01/SEC-02 triggerek | DB-szintű TenantType immutability + module whitelist |
| Token memory-only (Portal) | Nincs localStorage / cookie token storage |
| PKCE S256 | `crypto.subtle.digest` + base64url — helyes implementáció |
| E2E SSRF rejection teszt | `15-nodes-sync` test szándékosan teszteli a 169.254.x rejection-t |
| QuestPDF (nem HTML template) | PDF HTML injection strukturálisan lehetetlen |
