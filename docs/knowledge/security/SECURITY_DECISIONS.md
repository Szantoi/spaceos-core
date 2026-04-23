# SpaceOS — Security Decisions

> Minden biztonsági döntés, elfogadott kockázat és indoklás. Forrás: Sprint D Phase 1.5 + Sprint 6 security review.

---

## ADR-SEC-001 — Vite CVE elfogadott kockázat (E2E projekt)

**Finding:** GHSA-4w7w-66w2-5vf9, GHSA-v2wj-q39q-566r, GHSA-p9ff-h696-f583 — Vite 7.x dev server sérülékenységek. [MSG-E2E-028]

**Döntés:** Elfogadott kockázat (`7.3.2` patch-re frissítve).

**Indoklás:**
- Mindhárom CVE a Vite **dev servert** érinti (WebSocket hijacking, `server.fs.deny` bypass, `.map` path traversal)
- `vitest run` módban a dev server nem indul — a CI/CD és E2E futtatás `vitest run`-nal dolgozik
- Production bundleban nem fut Vite dev server
- **Tényleges kockázat: 0** (teszt infrastruktúra, nem production service)
- Mitigáció: `vite@7.3.2`-re frissítve, `npm audit` = 0 vulnerability

---

## ADR-SEC-002 — FORCE RLS bypass megelőzése — tábla owner szeparáció

**Finding (SEC-P15-07 KRITIKUS):** Ha az applikációs user (`spaceos_app`) egyben a tábla ownere is, a PostgreSQL nem alkalmazza a `FORCE ROW LEVEL SECURITY`-t (owner bypass). [MSG-K021]

**Döntés:** Minden tábla ownere `spaceos_schema_owner` NOLOGIN role legyen, nem `spaceos_app`.

**Indoklás:**
- `FORCE ROW LEVEL SECURITY` csak non-owner userekre érvényes
- `spaceos_app` INSERT/SELECT/UPDATE/DELETE jogokat kap explicit GRANT-tal
- `spaceos_schema_owner` NOLOGIN — nem tud bejelentkezni → kompromittálása nehezebb
- Ez az egyetlen módja a DB-szintű tenant izoláció garantálásának

---

## ADR-SEC-003 — DenyWebRequestSentinel minta bevezetése

**Finding:** `ClaimsTenantResolver.TryResolve()` null visszatérési értéke az EF Core query filterben bypass-olt. Web request kontextusban null = "nincs tenant" → átenged mindent. [MSG-KERNEL-081]

**Döntés:** Web request kontextusban hiányzó/érvénytelen `tid` claim → `DenyWebRequestSentinel` (GUID `000...002`).

**Indoklás:**
- Háttérfolyamatok (migration, scheduled job) számára szükséges a null bypass — ezek nem HTTP request kontextusból futnak
- Web requestnél viszont "nincs tenant claim" = auth hiba, nem bypass
- Sentinel GUID biztosítja, hogy az EF filter mindig hamis legyen (nem `== null` ágon megy)
- Megmarad a backward compat: `HttpContext == null` → null → bypass

---

## ADR-SEC-004 — MapInboundClaims = false döntés

**Finding:** ASP.NET Core `MapInboundClaims = true` default a `tid` claim-et `http://schemas.microsoft.com/identity/claims/tenantid` URI-ra rename-li → `ClaimsTenantResolver` nem találja. [MSG-KERNEL-082]

**Döntés:** `MapInboundClaims = false` minden JWT-t validáló .NET szolgáltatásban.

**Indoklás:**
- A SpaceOS saját `spaceos_tenants` és `tid` claim neveket használ (nem Microsoft SOAP konvenció)
- `MapInboundClaims = false` nem gyengíti az auth-ot — JWT signature validation változatlan
- Nélküle: `tid` → MS URI, `ClaimsTenantResolver` null-t kap → `DenyWebRequestSentinel` → minden kérés üres választ kap

**Érintett:** Kernel, Joinery, Abstractions, Cutting, Inventory, Procurement

---

## ADR-SEC-005 — ValidateAudience = true (Abstractions)

**Finding (MEDIUM-01):** `ValidateAudience = false` — más audience-re kiadott érvényes token (pl. portal, orchestrator scope) is elfogadásra kerülhetett az Abstractions modulban. [MSG-ABSTRACTIONS-006]

**Döntés:** `ValidateAudience = true` + `ValidAudience = builder.Configuration["Jwt:Audience"]`.

**Indoklás:**
- Audience ellenőrzés nélkül bármely SpaceOS token elfogadható — ez token confusion támadási felületet nyit
- Alacsony overhead (JWT parse már megtörtént)
- Minden modul saját audience-t kap

---

## ADR-SEC-006 — Procurement /healthz hiányának kockázati szintje

**Finding:** A Procurement modul nem rendelkezik `/healthz` endpointtal (csak `/health`). [MSG-CUTTING-003-DONE, MSG-INFRA-061-DONE]

**Döntés:** Elfogadott kockázat. Alacsony prioritás.

**Indoklás:**
- Procurement belső service (loopback-only)
- `/health` rendelkezésre áll monitoring célokra (200-as HTTP válasz)
- A hiány nem jelent biztonsági kockázatot — csak monitoring szintű kellemetlen
- Q3 scope: uniform `/healthz` endpoint minden service-n

---

## ADR-SEC-007 — Keycloak Script Mapper JAR deploy (nem beépített)

**Finding:** Keycloak 24-ben a Script Mapper nem beépített — `KC_FEATURES=scripts` + JAR deploy szükséges. [MSG-INFRA-KC01-DONE]

**Döntés:** JAR deploy elfogadott production megoldásnak.

**Indoklás:**
- A `spaceos-tenants` claim (double-serialized JSON, snake_case) az egyedi adatstruktúra miatt nem megvalósítható beépített mapperrel
- JAR: `/opt/keycloak-app/providers/spaceos-tenants-mapper.jar`
- `kc.sh build` + restart szükséges minden script módosítás után
- Backup megléte kötelező (`*.jar.bak-YYYYMMDD-HHMMSS` minta)

---

## ADR-SEC-008 — WORM hash sink role izolációja

**Finding (SEC-07):** Az `AUDIT_SINK_CONNECTION_STRING` ne `appsettings.json`-ból jöjjön — csak env var. [MSG-K031 Track C]

**Döntés:** `spaceos_audit_worm` NOLOGIN role csak INSERT jogosultsággal, env varból.

**Indoklás:**
- WORM = Write Once Read Many — ha a worm role is tud SELECT-elni/törölni, az audit hash manipulálható
- `AUDIT_SINK_CONNECTION_STRING` env var = systemd EnvironmentFile-ból jön, nem git-ben
- `spaceos_audit_worm` NOLOGIN: közvetlen DB hozzáférés nem lehetséges, csak az alkalmazáson keresztül

---

## ADR-SEC-009 — E2E test-runner client (Direct Access Grant)

**Finding:** A production PKCE OIDC flow headless E2E-ben nem tesztelhető közvetlenül. [MSG-E2E-028]

**Döntés:** `test-runner` confidential Keycloak client Direct Access Grant-tal, csak E2E célra.

**Indoklás:**
- PKCE flow böngésző redirect-et igényel — headless API tesztben nem kivitelezhető
- `test-runner` client: confidential, DAG ON, Standard Flow OFF
- `test-runner` client secret nincs git-ben (E2E `.env` `.gitignore`-ban van)
- A 28-keycloak-auth teszt a production PKCE flow konformitását ellenőrzi (nem a tényleges flow-t hajtja végre)
- Doorstar első live user-ek előtt PKCE UI tesztje manuálisan elvégzendő

---

## ADR-SEC-010 — kernel.env jogosultság (644 vs 640)

**Finding:** `/etc/spaceos/kernel.env` world-readable (644) — `Crypto__SigningKey` és DB jelszó benne van. [MSG-INFRA-060-DONE]

**Döntés:** Elfogadott kockázat ideiglenesen. Javítandó Q3 előtt.

**Indoklás:**
- VPS-en csak a `spaceos-deploy` user fér hozzá SSH-n keresztül
- `Crypto__SigningKey` jelenleg dev-only dummy érték (`spaceos-vps-dev-signing-key-2026`)
- Éles crypto funkciók bevezetése előtt 640-re kell szigorítani + titoktárból injektálni
- `abstractions.env` és `joinery.env` már 640 jogosultságúak (jobb minta)
- `systemctl show spaceos-kernel --property=EnvironmentFiles` — env fájl olvasása root vagy spaceos-deploy usernek engedélyezett

---

## Sprint 6 Security Review Összesítő

| ID | Severity | Komponens | Status |
|----|----------|-----------|--------|
| K1/KERNEL-081 | HIGH | DenyWebRequestSentinel bevezetése | ✅ FIXED |
| K2/KERNEL-082 | CRITICAL | MapInboundClaims=false | ✅ FIXED |
| M01 | MEDIUM | Abstractions ValidateAudience=true | ✅ FIXED |
| M02 | MEDIUM | Abstractions read-path RLS (DbCommandInterceptor) | ✅ FIXED |
| M03 | MEDIUM | Abstractions repo TenantId query filter | ✅ FIXED |
| SEC-P15-07 | CRITICAL | AuditEvents tábla owner | ✅ FIXED (Phase 1.5) |
| SEC-P15-08 | HIGH | RLS missing_ok fix | ✅ FIXED (Phase 1.5) |
| Vite CVE | HIGH | E2E vite@7.3.2 | ✅ FIXED |
| Axios CVE | HIGH | Orchestrator axios@1.15.0 | ✅ FIXED |
| kernel.env 644 | LOW | Permissions szigorítás | ⚠️ ACCEPTED — Q3 |
| Procurement /healthz | LOW | Health endpoint hiány | ⚠️ ACCEPTED — Q3 |
