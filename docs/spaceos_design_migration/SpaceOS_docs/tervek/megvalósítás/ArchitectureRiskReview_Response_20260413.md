# SpaceOS — Architektúra Kockázatelemzés Válasz

**Keletkezés:** 2026-04-13  
**Válaszol:** Architect (SpaceOS)  
**Forrás:** `ArchitectureRiskReview_20260413.md` (Devil's Advocate analízis)  
**Státusz:** LEZÁRVA — akciók rögzítve  
**Referencia kódbázis:** 1728+ teszt / 0 fail · commit d6b1bad · VPS live

---

## Összesítő

| # | Kockázat | DA értékelés | Architekt értékelés | Indoklás |
|---|---|---|---|---|
| 1 | LLM tool calling CNC kontextusban | 🔴 KRITIKUS | 🟡 KÖZEPES | Félreérti az architektúrát — az LLM nem generál CNC-t |
| 2 | Mixed stack kognitív overhead | 🔴 MAGAS | 🟡 KÖZEPES | Valós, de tudatos trade-off, dokumentált indokkal |
| 3 | Single VPS production | 🔴 KRITIKUS | 🔴 KRITIKUS | Teljes mértékben egyetértés — #1 infra prioritás |
| 4 | SHA-256 audit chain jogi státusz | 🟡 KOCKÁZAT | 🟡 KOCKÁZAT | Egyetértés — OpenTimestamps Escrow GA előtt |
| 5 | Keycloak komplexitás | 🟡 KÖZEPES | 🟢 ALACSONY | Keycloak DEPLOYED, IaC track-en, migration nem opció |
| 6 | B2B Federation YAGNI | 🔴 MAGAS | 🟡 KÖZEPES | Félreérti a scope-ot — nem federation, hanem egyszerű allowlist |

---

## 1. LLM Tool Calling — a DA legnagyobb tévedése

**DA állítás:** Az LLM 85–90%-os tool calling accuracy CNC-precíziós kontextusban kritikus kockázat. 10 rendelésből 1 hibás gyártási lapot eredményez.

**Korrekció:** A SpaceOS "Data → Rules → Geometry" axiómája pontosan ezt a kockázatot kezeli architektúra szinten:

```
User: "Csinálj egy balos beltéri ajtót, 90×210"
  ↓
L3 Orchestrator (LLM): tool_call → { "hinge_side": "left", "width": 900, "height": 2100 }
  ↓
L2 C# Driver (Modules.Abstractions): Graph Engine → determinisztikus CNC deriváció
  ↓
Output: CNC Plan (ManufacturingDerivationService.DeriveCncPlan())
```

Az LLM **kizárólag konfigurációs paramétereket** generál — nem CNC kódot, nem szabáslistát, nem méretet. A `ManufacturingDerivationService` determinisztikus: ugyanaz az input mindig ugyanazt a CNC plan-t adja. A CNC deriváció a C# Graph Engine-ben történik, amely:

- `ProductTemplate` → topologikus rendezés (Kahn's BFS) → `SlotConnection` lánc
- Paraméter-propagáció `CalculationRule`-okon keresztül (determinisztikus)
- `DeriveCncPlan()` és `DeriveProcessPlan()` — tiszta függvények, tesztelve (61 teszt, 0 fail)

**Ha az LLM hibásan mapeli a "balos"-t → `hinge_side: "right"`-ra:** Ez konfiguráció-hiba, nem CNC-hiba. A C# Driver a kapott paraméterrel helyesen számol — csak nem azt kapta, amit a user akart. Ez funkcionálisan ekvivalens azzal, mintha a user egy form-ban kézzel rossz értéket választana.

### DA források értékelése

| Forrás | Relevanciája SpaceOS-re |
|---|---|
| Berkeley Function Calling Leaderboard | Releváns, de a SpaceOS tool schema egyszerű (5–10 mező, erősen típusos) — nem multi-turn reasoning |
| "Hallucination is Inevitable" (Xu et al.) | Igaz általában, de a SpaceOS tool output-ot a C# Driver validálja — FluentValidation + Graph Engine constraint |
| Anthropic Tool Use Docs | Helyes — retry + validation loop implementálva (Phase 2, T-02: `wrapToolResult()`) |
| "Domain Adaptation of LLMs" (Ling et al.) | A legvalósabb pont — magyar faipari terminológia mapping tényleges kockázat |

### A valódi kockázat és mitigációja

A tényleges kockázat nem az LLM → CNC lánc (ez nem létezik), hanem a **magyar nyelv → angol paraméter mapping**. Pl. "tokborítás", "küszöb", "zárkés irány" — ezek domain-specifikus terminusok, amelyekre a few-shot prompt engineering az elsődleges mitigáció.

**Meglévő mitigáció:**
- Phase 2 T-02: `wrapToolResult()` + `sanitizeToolResultForLlm()` minden tool result-re
- Phase 2 T-02: `KernelClientError` enum — hibás tool call nem csendes
- Graph Engine: `CalculationRule` constraint-ek — érvénytelen paraméter-kombináció domain-szinten elutasítva

**Szükséges akció:** E2E 37-tools.chain.test.ts (PLANNED) méri a tényleges accuracy-t. Ha <95%, confidence-based confirmation gate bevezetése (low-confidence → form-based megerősítés). Ez a Doorstar Soft Launch E2E batch része.

### DA nyitott kérdéseire válasz

| Kérdés | Válasz |
|---|---|
| Van-e mért end-to-end accuracy? | Még nincs — 37-tools.chain.test.ts fogja mérni (PLANNED) |
| Human approval gate CNC előtt? | A Graph Engine constraint + `wrapToolResult()` az első védelmi vonal; human review opcionális rétegként tervezett, nem kötelezőként |
| LLM ROI vs. form-alapú konfigurátor? | Az LLM nem helyettesíti a form-ot — kiegészíti. A form mindig elérhető fallback. Az LLM értéke a természetes nyelvű interakció, nem a pontosság |

---

## 2. Mixed Stack — valós, de kontrollált trade-off

**DA állítás:** Három runtime (.NET 8 + Node.js 22 + React 18) = 23–40%-kal több idő build/tooling problémákkal, és senior-only teamet igényel.

**Értékelés:** A DA itt valós kockázatot azonosít. A három runtime ténylegesen overhead. De a döntés tudatos és dokumentált.

### Miért nem ASP.NET Core YARP az Orchestrator?

| Szempont | .NET BFF (YARP) | Node.js BFF (jelenlegi) |
|---|---|---|
| Anthropic/OpenAI SDK | Community-maintained, kevésbé érett | First-class, production-grade |
| SSE streaming + tool calling retry | Kézzel implementálandó | Natív async/await + stream API |
| Agentic loop (abort, retry, timeout) | Lehetséges, de boilerplate-igényes | Express middleware + AbortController natív |
| Kódbázis méret | ~500–800 sor | ~500 sor TypeScript (183 teszt) |
| Antropic SDK frissítések | Hetek késéssel, community PR-ekből | Same-day, official package |

A BFF stateless, ~500 sor TypeScript — nem egy második backend, hanem egy vékony proxy + LLM integráció réteg.

### Mitigiáló tényezők

1. **AI coding agent kompenzáció:** Claude Code a három stack-et egyformán kezeli — a "minden fejlesztőnek mindent tudnia kell" feltételezés a 2024 előtti világra érvényes
2. **Teszt-lefedettség stack-enként:** 1084 Kernel + 183 Orchestrator + 291 Portal + 170 Modules — mindegyik önállóan zöld
3. **Polyrepo izolál:** Kernel bug nem tör el Orchestrator build-et, és fordítva
4. **CI/CD pipeline stack-enként:** GitHub Actions workflow-ok független deploy-t biztosítanak

### DA források értékelése

| Forrás | Kommentár |
|---|---|
| Stack Overflow Survey 2024 (polyglot 23–40%) | Igaz nagyvállalati kontextusban, de a SpaceOS polyrepo + AI agent modellje más paradigma |
| ThoughtWorks Radar Vol 30 (HOLD kis csapatnál) | Korrekt figyelmeztetés — de a "kis csapat" itt 1 fő + AI agent, nem 3–4 fős hagyományos team |
| Martin Fowler "MicroservicePremium" | Érvényes — de a SpaceOS nem microservice, hanem 4 rétegű monolith-like architektúra polyrepo-ban |
| Google SRE Book Ch.8 | Érvényes ops szempontból — a mitigáció: PM2 + systemd + unified logging |

### Szükséges akció

.NET Anthropic SDK érettségi figyelése tech debt-ként rögzítve. Ha eléri a production-grade szintet (official package, streaming, tool calling), az Orchestrator konverzió értékelendő. Addig a jelenlegi stack indokolt.

---

## 3. Single VPS — teljes mértékben egyetértés

**DA állítás:** Single VPS (109.122.222.198) = single point of failure. B2B SaaS-nál blokkoló.

**Értékelés:** 🔴 **KRITIKUS — a legvalósabb kockázat a listán.** Minden DA hivatkozás helyes. A jelenlegi állapot:

| Kérdés | Jelenlegi állapot |
|---|---|
| RTO (Recovery Time Objective) | ❌ Nincs dokumentálva |
| RPO (Recovery Point Objective) | ❌ Nincs dokumentálva |
| Napi backup | ⚠️ VPS snapshot létezik, de restore nem tesztelt |
| PostgreSQL streaming replication | ❌ Nincs |
| Incident response playbook | ❌ Nincs |
| Blue/green deployment | ❌ Nincs |
| Automatikus failover | ❌ Nincs |

### A DA javaslata helyes, elfogadva

**Q3 2026 előtti minimum (2. ügyfél onboarding előtt):**

| # | Akció | Effort | Prioritás |
|---|---|---|---|
| 3.1 | RTO/RPO definiálás és elfogadás | 1 nap | P0 |
| 3.2 | PostgreSQL streaming replication → 1 standby (Hetzner 2. VPS) | 2–3 nap | P0 |
| 3.3 | Automatikus napi `pg_dump` + tesztelt restore script + cron | 1 nap | P0 |
| 3.4 | VPS szintű snapshot automatizálás (Hetzner API) | 0.5 nap | P1 |
| 3.5 | Incident response playbook (`/opt/spaceos/docs/runbooks/`) | 1 nap | P1 |

**Ez a Doorstar Soft Launch után, 2. ügyfél onboarding előtt a #1 infra prioritás.**

---

## 4. SHA-256 jogi státusz — egyetértés, roadmapban van

**DA állítás:** A SHA-256 hash chain technikailag tamper-evident, de jogilag nem eIDAS Qualified időbélyeg.

**Értékelés:** 🟡 Helyes. A Phase 3B Architecture v4 ezt explicit kezeli:

- `ProofHash` WORM sink: jelenleg SHA-256 + append-only PostgreSQL tábla
- Escrow GA gate (backlog): S3 Object Lock / Azure Immutable Blob követelmény rögzítve
- `HashAlgorithm` schema kész (Migration 0023) — SHA3-256 migration utility tervezve

### DA megoldási opciók értékelése

| Megoldás | Architekt értékelés |
|---|---|
| Microsec / NetLock TSA (HU) | Doorstar first invoice után mérlegelendő — ~0.01–0.05 EUR/event elfogadható |
| OpenTimestamps (Bitcoin anchor) | **Escrow GA előtti minimum** — ingyenes, egyszerű integráció, erős bizonyító erő |
| Periodikus notarizált snapshot | Alacsony prioritás — OpenTimestamps jobb megoldás |

### Szükséges akció

OpenTimestamps integráció az Escrow GA sprint részeként. A `ProofHash` tábla `AnchorTimestamp` + `AnchorTxId` column-nal bővül. Qualified TSA értékelés Doorstar first invoice milestone-nál.

---

## 5. Keycloak — a DA túlbecsüli a kockázatot

**DA állítás:** Keycloak enterprise IAM, korai SaaS fázisban overhead. 2GB RAM, ~40% manual migration minor verziónál.

**Értékelés:** 🟢 ALACSONY — a kockázat kezelt. A Keycloak **már production-ben fut** a teljes stack-en:

| Komponens | Státusz | Referencia |
|---|---|---|
| Keycloak 24.0 VPS deploy | ✅ DEPLOYED (2026-04-12) | MSG-INFRA-KC01 |
| OIDC PKCE + JWKS — Kernel | ✅ DEPLOYED | MSG-KC01 · 1068 teszt |
| OIDC PKCE + JWKS — Orchestrator | ✅ DEPLOYED | MSG-KC02 · 177 teszt |
| OIDC PKCE + JWKS — Portal | ✅ DEPLOYED | MSG-KC03 · 291 teszt |
| Script Mapper fix | ✅ DONE | MSG-INFRA-060 |
| Token lifespan 30min → 5min | ✅ DONE | MSG-INFRA-058 |
| Hostname fix + realm roles | ✅ DONE | MSG-INFRA-056 |

**A DA kérdéseire válasz:**

| Kérdés | Válasz |
|---|---|
| Operational cost vs. alternatívák? | A Keycloak-ra fordított effort (~12 nap) egyszeri befektetés. Migration most nem opció — Doorstar onboarding élesben fut rajta. |
| IaC-ban van a konfiguráció? | `realm-export.json` létezik, de van tech debt: VPS nem git repo, git commit manuális. |

### Szükséges akció

Egyetlen akció: `realm-export.json` git commit automatizálása (cron job vagy deploy step). A Keycloak migration (Ory Kratos, Authentik) a backlog legaljára kerül — csak akkor releváns, ha a RAM constraint blokkolóvá válik (2. VPS-sel ez sem probléma).

---

## 6. B2B Federation — a DA félreérti a scope-ot

**DA állítás:** A B2BHandshake implementáció YAGNI-violation — federation két ügyféllel felesleges.

**Korrekció:** A DA "federation"-nek nevezi azt, ami valójában **egyszerű B2B rendelés-delegálás**:

### Ami a B2BHandshake valójában

```sql
-- TenantHandshakeAllowlist (Migration 0026)
-- = Egyetlen JOIN tábla: melyik Tenant rendelhet melyik másik Tenant-tól
CREATE TABLE "TenantHandshakeAllowlist" (
  "GuestTenantId"     uuid NOT NULL,   -- aki rendel (Asztalos)
  "HostTenantId"      uuid NOT NULL,   -- akit megrendelhet (Doorstar)
  "AllowedTradeTypes" varchar(32)[] NOT NULL
);
```

```csharp
// B2BHandshake — Value Object a FlowEpic aggregátumon
// = GuestTenantId + delegation timestamp, semmi más
public class B2BHandshake { ... }
```

### Ami NEM része a jelenlegi implementációnak

| Federation feature (DA által feltételezett) | Implementálva? |
|---|---|
| Cross-tenant identity mapping | ❌ Nincs, nem is tervezett |
| Cross-boundary data sharing / schema versioning | ❌ Nincs — SpatialContractsView AABB-only |
| Conflict resolution | ❌ Nincs — egyirányú delegálás |
| Partial failure recovery | ❌ Nincs — szinkron API hívás |
| Cross-tenant authorization (Need-to-Know RBAC) | ❌ Nincs — RLS tenant-szinten, allowlist tábla szűr |

A teljes B2BHandshake scope ~200 sor kód (Domain entity + allowlist tábla + 1 API endpoint). Ez nem "híd két ember számára" — hanem a domain modell természetes része: az asztalos ajtót rendel Doorstartól, ehhez kell egy allowlist.

### DA források értékelése

| Forrás | Kommentár |
|---|---|
| Pat Helland "Data on the Outside" | Releváns, de a SpaceOS B2BHandshake **nem** cross-boundary data sharing — az allowlist egyetlen DB-ben él, RLS-sel védve |
| Sam Newman "Building Microservices" | Érvényes distributed system-re — de a SpaceOS B2BHandshake nem distributed, hanem single-DB join |
| AWS SaaS Factory "federation 50+ tenant" | Helyes — de ami implementálva van, az nem federation |
| Kent Beck YAGNI | Helyes elv — de a Doorstar use case konkrét: asztalos ajtót rendel. Ez nem spekuláció, hanem a referencia-ügyfél core workflow-ja |
| Gartner Multi-Enterprise Platforms | A SpaceOS Level 1/2 — és a B2BHandshake pontosan ezt a szintet célozza, nem Level 4/5-öt |

### DA nyitott kérdéseire válasz

| Kérdés | Válasz |
|---|---|
| Melyik 2026-os use case igényli? | Doorstar: az asztalos (Guest) ajtót rendel Doorstartól (Host). Ez a Doorstar Soft Launch core feature-je. |
| YAGNI-violation? | Nem — a Doorstar referencia-ügyfél workflow-ja tartalmazza a B2B rendelést. Nélküle a Soft Launch csonka. |

### Szükséges akció

Nincs akció — a jelenlegi scope adekvát. Ha a jövőben (2027, DACH terjeszkedés) valódi federation igény merül fel (cross-schema sync, partial failure, distributed identity), az egy **új architektúra fázis**, nem a jelenlegi B2BHandshake kiterjesztése.

---

## Amit a DA helyesen NEM kérdőjelezett meg

Egyetértés a DA "pozitív lista"-jával:

| Döntés | Miért helyes |
|---|---|
| Domain modell (FlowEpic, SpaceLayer, Tenant hierarchia) | Clean Architecture + DDD megfelelő ennél a komplexitásnál |
| "Data → Rules → Geometry" axióma | A frontend egyetlen koordinátát sem számol — ez a #1 kockázat-csökkentő |
| PostgreSQL + EF Core | Multi-tenant SaaS standard választás, RLS-sel kiegészítve |
| Immutable audit event log | Technikailag helyes; a jogi réteg (pont #4) a kiegészítendő |
| Faipari piac diagnózisa | Viber + Excel koordináció valós fájdalompont |
| Turborepo monorepo | Indokolt multi-brand portálnál (D-04 döntés: modul-rendszer egy app-ban) |

---

## Akció-lista (végleges)

| # | Kockázat | Akció | Felelős | Határidő | Prioritás |
|---|---|---|---|---|---|
| 1 | LLM accuracy | E2E 37-tools.chain.test.ts → mérés → ha <95%, confidence gate | Architect | Következő E2E batch | P1 |
| 2 | Mixed stack | .NET Anthropic SDK figyelése tech debt-ként | Architect | Folyamatos | P3 |
| 3.1 | Single VPS | RTO/RPO definiálás és dokumentálás | Architect | Q3 2026 előtt | **P0** |
| 3.2 | Single VPS | PostgreSQL streaming replication (Hetzner 2. VPS) | Architect | Q3 2026 előtt | **P0** |
| 3.3 | Single VPS | Automatikus pg_dump + tesztelt restore + cron | Architect | Q3 2026 előtt | **P0** |
| 3.4 | Single VPS | Incident response playbook | Architect | Q3 2026 előtt | P1 |
| 4 | SHA-256 jogi | OpenTimestamps integráció | Architect | Escrow GA sprint | P1 |
| 5 | Keycloak | `realm-export.json` git commit automatizálás | Architect | Következő infra task | P2 |
| 6 | B2B scope | Nincs akció szükséges | — | — | — |

---

## Módszertani megjegyzés

A Devil's Advocate elemzés értékes eszköz. A 6 pontból 2 teljes mértékben helyes (#3, #4), 2 részben helyes de túlbecsült (#2, #5), és 2 félreérti az architektúrát (#1, #6). Ez 33% precision-rate a KRITIKUS besorolásra — ami azt jelzi, hogy az elemzés a dokumentáció alapján dolgozott, nem a tényleges kódbázis és architektúra-döntések ismeretében.

A jövőbeli DA elemzéseknél javasolt: a kódbázis státusz (`Codebase_Status_YYYYMMDD.md`) és az ADR-ek (`SpaceOS_*_Architecture_v*.md`) bevonása az input-ba.

---

*Ez a dokumentum a `ArchitectureRiskReview_20260413.md` formális válasza. Mindkét dokumentum a Project Knowledge részét képezi.*
