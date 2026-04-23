# SpaceOS — Test Coverage Risk Review · 2026-04-15

**Módszer:** Devils-Advocate audit — 6 párhuzamos ügynök, kódbázisokra specializálva  
**Scope:** Kernel · Orchestrator · Portal · Joinery · Abstractions · E2E  
**Kiváltó esemény:** Sprint 4 lezárása (Migration 0029 live, Ecosystem Actor v4, 1931 teszt pass)  
**Elvégzője:** Root terminál (koordinátor)  
**Összesített tesztszám az audit előtt:** 1931+ pass (166/166 E2E)

---

## Kontextus

Az audit célja nem a kódbázis minőségének megkérdőjelezése — a rendszer szállítható, a Doorstar Q2
Soft Launch teljesíthető. A cél: azonosítani azokat a pontokat, ahol a tesztek **hamis
biztonságérzetet adnak**, és ahol az első éles ügyfél terhelés alatt produkciós incidensek
keletkezhetnek.

---

## R-13 — E2E: Cross-tenant izoláció teljesen teszteletlen

**Kategória:** Biztonsági · KRITIKUS  
**Forrás:** E2E Devils-Advocate audit 2026-04-15  
**Valószínűség:** Magas (architectural precedens: Phase 2 RLS sentinel fallback bug megtörtént)  
**Hatás:** KATASZTROFÁLIS — egy tenant látja a másik adatait → jogi, reputációs, üzleti megsemmisítés

### Leírás

A 37 E2E tesztfájlban egyetlen cross-tenant negatív teszt sem létezik. Nincs teszt arra, hogy:
- Tenant A user Tenant B node-ját kéri le → elvárás: **404** (nem 403: a 403 létezést fed fel)
- Tenant A user Tenant B erőforrását módosítja → elvárás: 404
- Tenant A user feliratkozik SSE-re → Tenant B eseményei nem érkeznek meg

A Phase 2 memóriában rögzítve: az RLS sentinel fallback **production bug volt** — regressziós
teszt nélkül ez bármikor visszatérhet.

### Elfogadhatatlan állapot

```
GET /api/nodes/{tenant_b_node_id}
Authorization: Bearer <tenant_a_jwt>
→ Ha 200, a rendszer adatszivárgással rendelkezik production-ban
→ Ha 404, helyes — de ezt MA nem ellenőrzi egyetlen teszt sem
```

### Ajánlott akció

`E2E-023`: Cross-tenant isolation test suite:
- Előkészítés: 2 tenant, 2 user, kölcsönös erőforrások létrehozása
- Negatív tesztek: GET/PATCH/DELETE cross-tenant → 404
- SSE stream: Tenant B esemény nem jelenik meg Tenant A stream-jén
- Forged header: kézzel módosított `tid` claim → 401/403

**Határidő:** Sprint 5 — E2E terminál, magas prioritás

---

## R-14 — KERNEL: DB triggerek SQLite/InMemory teszteken nem futnak

**Kategória:** Tesztelési hamis pozitív · KRITIKUS  
**Forrás:** Kernel Devils-Advocate audit 2026-04-15  
**Valószínűség:** Közepes (Phase 1.5 SQLite-Postgres keveredés precedens)  
**Hatás:** Magas — SEC-01/SEC-02 biztonsági triggerek tesztelés nélkül, runtime-on derül ki a hiba

### Leírás

A Migration 0029 két PostgreSQL triggert vezet be:
- `prevent_tenant_type_change` (SEC-01): `TenantType` nem módosítható UPDATE-tel
- `validate_enabled_modules_for_type` (SEC-02): `EnabledModules` értékkészlete `TenantType`-hoz kötött

Ezek a triggerek **csak valódi PostgreSQL-en léteznek**. Ha bármely integrációs teszt EF Core
InMemory provider-t vagy SQLite-ot használ, a triggerek csendesen hiányoznak. A teszt zöld, de
a Golden Rule #3 (Immutability & Trust) iluzórikus védelmi vonallal rendelkezik.

Precedens: a Phase 1.5 bugban SQLite típusok szivárogtak Postgres-re — ez a keveredési minta
dokumentált a projektben.

### Ellenőrzendő kérdés

> A 101 integrációs tesztből mennyi futtat valódi PostgreSQL-t (Testcontainers / docker-compose)?
> Mennyi SQLite/InMemory-t?

### Ajánlott akció

1. `KERNEL-072`: Integrációs teszt provider audit — minden SQLite/InMemory referencia azonosítása
2. Ha van keveredés: migrálás `Testcontainers` alapú PostgreSQL-re
3. SEC-01 dedikált teszt: `UPDATE "Tenants" SET "TenantType" = 'Installer' WHERE ...` → DB kivétel
4. SEC-02 dedikált teszt: érvénytelen modul kombinació INSERT → DB kivétel

**Határidő:** Sprint 5 — Kernel terminál

---

## R-15 — KERNEL: Audit chain integritás nem CI-garantált

**Kategória:** Golden Rule #3 megsértés · KRITIKUS  
**Forrás:** Kernel Devils-Advocate audit 2026-04-15 + KERNEL-070 preexisting gap  
**Valószínűség:** Már létező gap (KERNEL-070 audit chain hash mismatch azonosítva)  
**Hatás:** Magas — az Escrow/WORM feature előfeltétele; jogi audit esetén az audit trail
megbízhatatlan

### Leírás

A KERNEL-070 (`new/` mappában) egy preexisting audit chain hash mismatch-et dokumentál.
Az SHA-256 hash lánc integritása nincs CI-ban folyamatosan verifikálva — ha egy commit
megbontja a láncot, csak manuális ellenőrzésnél derül ki.

Golden Rule #3: *"Immutability & Trust — nincs UPDATE CAD adatokon, minden SHA-256 hashed
audit eventtel"* — ez a szabály konvenció által tartott, nem teszttel.

### Ajánlott akció

KERNEL-070 feloldása: dedikált `AuditChainIntegrityTest` amely:
- Létrehoz N audit eventet
- Visszaolvassa és hash-lánc konzisztenciát ellenőriz
- CI gate: ha lánc törött, build fail

**Határidő:** Sprint 5 (escrow/WORM előfeltétel) — Kernel terminál

---

## R-16 — ORCHESTRATOR: Upstream hibakezelés és auth negatív ágak

**Kategória:** Resilience + biztonsági · KRITIKUS  
**Forrás:** Orchestrator Devils-Advocate audit 2026-04-15  
**Valószínűség:** Magas (egyetlen upstream failure teszt sem azonosítható)  
**Hatás:** Magas — Kernel leállás esetén portal befagy; missing `tid` = cross-tenant szivárgás

### Leírás

**1. BFF catch-all proxy hibakezelés hiánya:**
A `/bff/api/*` proxy-nak nincs egyetlen tesztje arra, mi történik ha:
- Kernel ECONNREFUSED → portal 30s-ig fagy
- Kernel 500 → raw error leak a kliensnek
- Kernel timeout → hanging request

**2. `requireAuth` negatív ágak:**
A missing `tid` claim eset a legveszélyesebb: ha a middleware csendesen default-ol
tenant-ra vagy átugorja a tenant-ellenőrzést, cross-tenant adatszivárgás jön létre.
Tesztelendő mátrix: `[no-token, expired, wrong-iss, wrong-aud, missing-tid, tid-mismatch]`.

### Ajánlott akció

`ORCH-065`: 
1. `proxy.errors.test.ts`: nock/msw mock — ECONNREFUSED, 500, 504, 30s hang → BFF 502/504, bounded timeout
2. `requireAuth.matrix.test.ts`: 6 negatív eset, mindegyik 401/403 + specifikus hibakód

**Határidő:** Sprint 5 — Orchestrator terminál

---

## R-17 — PORTAL: Auth boundary teszteletlen

**Kategória:** Biztonsági · KRITIKUS  
**Forrás:** Portal Devils-Advocate audit 2026-04-15  
**Valószínűség:** Közepes  
**Hatás:** Magas — CSRF vektor, session kill, Doorstar első felhasználói élmény

### Leírás

**1. PKCE callback hibaágak:**
A `CallbackPage.tsx` nincs tesztelve egyetlen OAuth hibaesetére sem:
- `?error=access_denied`
- `?error=invalid_state` (= state parameter mismatch → CSRF indikátor)
- `code_verifier` hiányzik a sessionStorage-ból
- Keycloak token endpoint 400/500

**2. Refresh token race condition:**
Két komponens egyszerre triggerel `refreshToken()` → két kérés → Keycloak refresh token
reuse detection → **session kill** → felhasználó kidobva. Egyetlen dedupe teszt sincs.

### Ajánlott akció

`PORTAL-003`:
1. `CallbackPage.test.tsx`: 4 OAuth hibaesetre assertion + state mismatch = security log
2. `authStore.concurrent.test.ts`: `vi.useFakeTimers()` + `Promise.all([refresh(), refresh()])` → 1 hálózati kérés

**Határidő:** Sprint 5 — Portal terminál

---

## R-18 — ABSTRACTIONS: Graph Engine ciklus detekció write-time hiánya

**Kategória:** Algoritmikus invariáns · KRITIKUS  
**Forrás:** Abstractions Devils-Advocate audit 2026-04-15  
**Valószínűség:** Ismeretlen (nem derül ki a tesztekből, mert a gap épp ott van)  
**Hatás:** MAGAS — érvénytelen gráf persistálva → downstream Joinery/Cutting végtelen loop;
NuGet Contracts 1.0.0 boundary miatt külső integrátorokhoz is eljut

### Leírás

Az ügynök központi kérdése: **`AddEdge()` futtat-e DFS ciklus-ellenőrzést az INSERT előtt,
vagy csak `DeriveManufacturing()` traversal-kor kerül elő a probléma?**

Ha csak traversal-time:
- Érvénytelen gráf bekerül a DB-be (audit immutability miatt ki sem javítható UPDATE-tel)
- Diamond dependency: A→B, A→C, B→D, C→D → D kétszer deriválódik → helytelen gyártási mennyiség
- Concurrent edge insert: két writer egymástól független érvényes edge-et ad hozzá, együtt ciklust alkotnak

Mivel ez a modul **foundational** (Joinery, Cutting, jövőbeli modulok mind fogyasztják), egy
algoritmikus hiba minden downstream modult érint.

### Ajánlott akció

`ABSTRACTIONS-003`:
1. Megerősíteni / dokumentálni: hol fut a ciklus-ellenőrzés (write-time vs. traversal)
2. Ha csak traversal-time: `AddEdge` pre-condition DFS check hozzáadása
3. Tesztek: self-referential cycle, diamond dependency, concurrent cycle creation
4. `AddEdge_WhenCreatesCycle_RejectsAtWriteTime` legyen explicit teszteset

**Határidő:** Sprint 5 — Abstractions terminál

---

## R-19 — JOINERY: PDF gyártásilap golden-file teszt és contract integráció

**Kategória:** Funkcionális helyesség · MAGAS  
**Forrás:** Joinery Devils-Advocate audit 2026-04-15  
**Valószínűség:** Közepes  
**Hatás:** Magas — hibás gyártásilap jogi/operációs dokumentum; silent field loss nem derül ki

### Leírás

**1. PDF gyártásilap golden-file teszt hiánya:**
A gyártásilap Doorstar számára operatív dokumentum — hibás mező esetén a gyártás megáll,
vagy rosszabb: rossz méretű ajtó készül el. Jelenleg nincs:
- Golden-file / snapshot teszt a generált PDF ellen
- Magyar karakterkódolás teszt (ő, ű, á, é — PDF encoding edge case)
- Kötelező mezők jelenlétének assertion-je

**2. ICuttingProvider / IInventoryProvider / IProcurementProvider mock-only:**
A mock-ok az implementáló feltételezéseit tesztelik, nem a valódi contract surface-t.
Ha Modules.Contracts 1.0.0 szemantikát változtat (null vs empty, exception vs Result),
minden mock zöld marad, production elszakad.

**3. VPS pending migráció:**
A `0002+J0002` migration még nincs alkalmazva VPS-en és nincs production-like adattal
tesztelve. (Precedens: Migration 0029 story mutatta, milyen veszélyes a DB drift.)

### Ajánlott akció

`JOINERY-003`:
1. PDF golden-file test: 3 canonical order, PdfPig extraction, field + encoding assertion
2. `Joinery.Contracts.IntegrationTests` projekt: Testcontainers + real HTTP, nem mock
3. VPS migration alkalmazása staging-en production seed adattal, CI job

**Határidő:** Sprint 5 — Joinery terminál

---

## R-20 — E2E: Ecosystem Actor v4 és hibaágak lefedése

**Kategória:** Regresszió · MAGAS  
**Forrás:** E2E Devils-Advocate audit 2026-04-15  
**Valószínűség:** Biztos gap (file inventory megerősítette: nincs `*-ecosystem*` vagy `*-tenant-type*` fájl)  
**Hatás:** Közepes — a Sprint 4 legnagyobb feature-je nincs E2E-ben lefedve

### Leírás

Az Ecosystem Actor Architecture v4 (TenantType, ModuleRegistry, TenantHandshakeAllowlist)
a Sprint 4 fő deliverable-je — de a 37 E2E fájlban egyik endpoint sem szerepel.

Továbbá: a suite kizárólag happy-path-t fed le. Hiányzó negatív ágak:
- 401 (no token), 403 (wrong role), 404 (not found), 422 (validation error), 500 (server error)
- Rate limiting viselkedés

### Ajánlott akció

`E2E-024` (Sprint 5):
1. `38-ecosystem-actor.chain.test.ts`: TenantType CRUD + ModuleRegistry + TenantHandshakeAllowlist
2. `39-error-paths.test.ts`: minden endpoint negatív ág (401/403/404/422)

**Határidő:** Sprint 5 — E2E terminál

---

## Összesítő táblázat

| ID | Kockázat | Terminál | Súlyosság | Státusz |
|---|---|---|---|---|
| R-13 | Cross-tenant E2E izoláció hiánya | E2E | 🔴 KRITIKUS | Nyitott |
| R-14 | DB triggerek SQLite-on nem futnak | KERNEL | 🔴 KRITIKUS | Nyitott |
| R-15 | Audit chain CI-garantálatlan | KERNEL | 🔴 KRITIKUS | Nyitott |
| R-16 | BFF proxy + `requireAuth` negatív ágak | ORCH | 🔴 KRITIKUS | Nyitott |
| R-17 | PKCE callback + refresh race condition | PORTAL | 🔴 KRITIKUS | Nyitott |
| R-18 | Graph Engine ciklus detekció write-time | ABSTRACTIONS | 🔴 KRITIKUS | Nyitott |
| R-19 | PDF golden-file + contract integráció | JOINERY | 🟡 MAGAS | Nyitott |
| R-20 | Ecosystem Actor v4 E2E lefedés | E2E | 🟡 MAGAS | Nyitott |

### Moderate (következő sprint)

| Kockázat | Terminál |
|---|---|
| LLM provider failure (Gemini 429/503) → portal crash | ORCH |
| SSE socket leak kliens disconnect esetén | ORCH |
| Upstream 4xx/5xx propagation mangling | ORCH |
| Error boundary hiánya → white screen | PORTAL |
| NodeGraph round-trip konzisztencia | PORTAL |
| RLS negatív teszt Joinery szinten | JOINERY |
| Business rule edge cases (dimension/material) | JOINERY |
| RLS cross-tenant gráf access | ABSTRACTIONS |
| Diamond dependency double-count | ABSTRACTIONS |
| Token refresh mid-request retry | E2E |

---

## Korábbi kockázatok státusza

Az R-01–R-12 kockázatok változatlanok (ld. README.md aktív tábla).
Ez a dokumentum az R-13–R-20 kockázatokat rögzíti.

---

*Generálta: Root terminál · Devils-Advocate audit · 2026-04-15*
