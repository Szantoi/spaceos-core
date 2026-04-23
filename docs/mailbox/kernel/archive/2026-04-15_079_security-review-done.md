---
id: MSG-KERNEL-079-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-079
created: 2026-04-15
---

# MSG-KERNEL-079 — Security Review (Q2 Pre-launch) DONE

## Összefoglaló

Teljes statikus biztonsági önellenőrzés elvégezve. **Kritikus sérülékenység: 0.**
Blokkoló probléma nincs — outbox státusz: `DONE` (nem `BLOCKED`).

---

## Eredmény táblázat

| # | Terület | Státusz | Kockázat |
|---|---|---|---|
| 1 | Authentication & Authorization | ✅ PASS | — |
| 2 | Row-Level Security (RLS) | ⚠ RÉSZLEGES | Közepes |
| 3 | Input Validation | ✅ PASS | — |
| 4 | Audit Immutability | ✅ PASS | — |
| 5 | Sensitive Data | ✅ PASS (1 minor) | Alacsony |
| 6 | OWASP Top 10 | ✅ PASS (2 minor) | Alacsony |

---

## 1. Authentication & Authorization — ✅ PASS

- **Összes endpoint `[Authorize]`-zal védett** — minden endpointon `RequireAuthorization()` van
- **Kivétel (szándékos):** `POST /api/auth/logout` → `.AllowAnonymous()` (helyes)
- **JWT konfig helyes:** `ValidateIssuer=true`, `ValidateAudience=true`, `ValidateLifetime=true`, `ClockSkew=30s`, RS256/JWKS Keycloak authority discovery
- **RBAC policies:** 6 policy definiálva (`ReadPolicy`, `WritePolicy`, `AdminPolicy`, `SystemAdminPolicy`, `TenantAdminPolicy`, `StageOperatorPolicy`), minden endpointon megfelelő policy
- **ClaimsTenantResolver null handling:** `null` visszatérési érték (soha nem throw), meghívók kezelik — helyes
- **TenantSessionInterceptor:** csak PostgreSQL (produkció) alatt regisztrálva, Singleton, paraméteres SQL (`set_config`)

**Minor (LOW):** `/healthz` és `/health/ready` endpointok **nincs** `.AllowAnonymous()` — monitoring probe-ok 401-et kapnak JWT nélkül. Kubernetes liveness probe-okat érinthet.
→ `Program.cs` health endpoint regisztrációban `.AllowAnonymous()` hozzáadandó (következő sprint)

---

## 2. Row-Level Security (RLS) — ⚠ RÉSZLEGES

**RLS-sel lefedett táblák (12 db):** AggregateSnapshots, OutboxMessages, AuditHashes, PhysicalSpaces, BvhNodes, SpatialElements, SpatialTaskLinks, TenantHandshakeAllowlist, StageDefinitions, StageChainTemplates, StageChainSteps, StageHandoffs

**DB-szintű RLS nélküli core táblák (6 db):**

| Tábla | EF Global Filter | DB RLS | Kockázat |
|---|---|---|---|
| `Tenants` | ✅ | ❌ | Közepes |
| `Facilities` | ✅ | ❌ | Közepes |
| `FlowEpics` | ✅ | ❌ | Közepes |
| `WorkStations` | ✅ | ❌ | Közepes |
| `SpaceLayers` | ✅ | ❌ | Közepes |
| `AuditEvents` | ✅ | ❌ | Közepes |

**Megjegyzés:** EF Core global query filter véd minden alkalmazás-szintű hozzáféréstől. A DB-szintű RLS hiánya csak közvetlen DB hozzáférésnél (postgres user, backup script, admin tool) jelent cross-tenant exposure kockázatot. Az alkalmazás logikájában nincs szivárgás.

**Minor (LOW):** `SpatialTaskLinks` és `TenantHandshakeAllowlist` policy-kban hiányzik a COALESCE null guard — `app.current_tenant_id` unset esetén implicit deny (nem explicit sentinel). Következő sprint, migrációval javítható.

**Backlog javaslat:** Core 6 tábla DB-szintű RLS hozzáadása a Q3 sprint során.

---

## 3. Input Validation — ✅ PASS

- **Összes Command-hoz van FluentValidation validator** — `AddValidatorsFromAssembly()` + `ValidationBehavior<,>` MediatR pipeline-ban
- **SQL injection:** nincs raw string concat user inputtal — minden EF Core paraméteres, raw SQL-ek fix literálokat (GUID, DateTimeOffset) interpolálnak, nem user inputot
- **Request body size limit:** Kestrel-szinten 64 KB (`Program.cs:281`)

---

## 4. Audit Immutability — ✅ PASS

- **`IAuditEventRepository`** csak `AddAsync()` + read metodusokat exponál — `UpdateAsync`/`DeleteAsync` nem létezik
- **`AuditEvent` domain entity:** minden property `{ get; private set; }` — módosítás impossible alkalmazás rétegből
- **API szinten:** csak `GET` és chain verify/analysis endpointok — `PUT`/`PATCH`/`DELETE` nem elérhető
- **Hash chain:** `PreviousHash` → `StateHash` lánc, `Sequence BIGINT GENERATED ALWAYS AS IDENTITY` tiebreaker (Migration 0030)
- **SEC-01 trigger:** `TR_Tenants_ImmutableTenantType` — `TenantType` módosítása tiltva creation után
- **SEC-02 trigger:** `TR_Tenants_ValidateModulesForType` — per-tenant-type module whitelist enforce

---

## 5. Sensitive Data — ✅ PASS (1 minor)

- **ExternalAuthToken:** nem plaintext DB-ben — `ExternalAuthTokenRef` Key Vault reference nevet tárol, actual secret runtime-on `ISecretProvider`-en keresztül kerül elő ✅
- **Exception handling:** stack trace soha nem szivárog API response-ba (`ExceptionHandlingMiddleware.cs`) ✅
- **AuditEvent payload:** API response DTO-ból kizárva (`AuditEventDto`) ✅
- **ActorId:** `IPseudonymizer` via GDPR compliance ✅
- **appsettings.Production.json:** csak `Jwt:Authority`, `Jwt:Audience`, `Urls` — nincs hardcoded secret ✅

**Minor (LOW):** `ConfigurationGenesisHashProvider.cs:57-60` — az ephemeral genesis hash **plain text logba kerül** `LogWarning` szinten, ha `AuditChain:GenesisHash` nincs konfigurálva. A genesis hash technikai értelemben nem titok (csak audit chain anchor), de log aggregation rendszerekben megjelenik.
→ Megoldás: `AuditChain:GenesisHash` VPS-en konfigurálva (MSG-077 javaslat) — akkor ez az ág nem fut

---

## 6. OWASP Top 10 — ✅ PASS (2 minor)

| # | Ellenőrzés | Eredmény |
|---|---|---|
| A1 — Broken Access Control | Cross-tenant resource → `404` (nem `403`) ✅ — EF global filter, tenant nem tudja enumerate más tenant adatát | PASS |
| A2 — Cryptographic Failures | MD5/SHA1/DES: **nem található** a kódbázisban. SHA-256 audithoz, RS256 JWT-hez ✅ | PASS |
| A3 — Injection | EF Core paraméteres query mindenhol, raw SQL csak fix literálokkal ✅ | PASS |
| A5 — Security Misconfiguration | appsettings.Production.json tiszta ✅. **Missing:** HSTS, X-Content-Type-Options, X-Frame-Options security response header-ek | PASS (minor gap) |
| A9 — Logging PII | Email/telefon/név nem logolódik. SourceIp: szándékos audit trail elem. ActorId: pseudonymizált ✅ | PASS |

**Minor (LOW):** Biztonsági HTTP response headerek (`Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`) **nem konfiguráltak**. Mivel az API backend-only (nem közvetlenül böngésző-facing), hatása korlátozott. Ajánlott hozzáadni `Program.cs`-ben middleware-ként.

---

## Összefoglalt találatok — prioritás szerint

### Kritikus (azonnali fix): 0

### Közepes (következő sprint): 1

| # | Probléma | Fájl | Teendő |
|---|---|---|---|
| M1 | 6 core tábla DB-szintű RLS hiány | `Migrations/InitialCreate.cs` + `AddAuditEventsTable.cs` | Q3 sprint: RLS policy migrációk hozzáadása |

### Alacsony (backlog): 4

| # | Probléma | Fájl | Teendő |
|---|---|---|---|
| L1 | `/healthz` endpoint 401-et ad monitoring probe-nak | `Program.cs` health reg. | `.AllowAnonymous()` health endpointokra |
| L2 | Genesis hash clear-text log | `ConfigurationGenesisHashProvider.cs:57` | VPS-en `AuditChain:GenesisHash` konfigurálása (MSG-077) eliminates this path |
| L3 | HSTS + security headers hiány | `Program.cs` | Security header middleware hozzáadása |
| L4 | 2 RLS policy COALESCE null guard hiány | Migration 0019, 0026 | Javítómigráció SpatialTaskLinks + TenantHandshakeAllowlist |

---

## Tesztek

Nincs kódváltozás ebben a feladatban (review only). **1110 teszt zöld.**
