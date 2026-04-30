---
id: MSG-ARCH-017-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-014
created: 2026-04-28
---

# Manufacturing Phase 1 Architecture — Review eredmény

> Tervdok: `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md` (v4 FINAL)
> README: `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md`
> 43 finding (2🔴/16🟠/25🟡), 0 nyitott CRITICAL/HIGH

---

## 1. Scope — mi Phase 1, mi Phase 2?

**Határ tiszta és jól dokumentált.**

| Feature | Státusz |
|---------|---------|
| `ManufacturingOrder` saga koordinátor + counter columns | ✅ Phase 1 |
| `EdgeBandingTask` aggregate (FSM + line items) | ✅ Phase 1 |
| `CncTask` aggregate (FSM + PlanIntegrityHash SEC-07) | ✅ Phase 1 |
| Cross-module Inbox subscription (`CuttingPanelCompleted`) | ✅ Phase 1 |
| Workers.Identity service (Enroll, Revoke, KEK rotation) | ✅ Phase 1 (dedikált Track D) |
| mTLS cross-module endpoint (`/internal/inbox/cutting`) | ✅ Phase 1 |
| `Surface` + `Assembly` aggregatok | ❌ NEM Phase 1 — Phase 2 |
| DAG-based operation ordering | ❌ NEM Phase 1 — Phase 2 |
| Machine-HMAC enforce (SEC-16 Phase 1 csak audit marker) | ❌ NEM Phase 1 — Phase 2 |
| Logistics / Installation modulok | ❌ Phase 1+ downstream |

**Verdict:** A scope határ clean. Semmi nem szivárog be Phase 1-be, ami nem kell.

---

## 2. Két repo — setup szükséges?

**Igen, mindkét repo teljesen új — 0 sor kód sem létezik még.**

| Repo | Létezik? | Setup szükséges |
|------|----------|-----------------|
| `spaceos-modules-manufacturing` | ❌ NEM | Teljes scaffold Day 1 Track A/B |
| `spaceos-workers-identity` | ❌ NEM | Teljes scaffold Day 1 Track D |

**Scaffold terv:**

```
spaceos-modules-manufacturing/
├── SpaceOS.Modules.Manufacturing.Domain/
├── SpaceOS.Modules.Manufacturing.Application/
├── SpaceOS.Modules.Manufacturing.Infrastructure/
├── SpaceOS.Modules.Manufacturing.Api/             (port: 5007 — lásd §7)
└── SpaceOS.Modules.Manufacturing.Tests/

spaceos-workers-identity/
├── SpaceOS.Workers.Identity.Domain/
├── SpaceOS.Workers.Identity.Application/
├── SpaceOS.Workers.Identity.Infrastructure/
├── SpaceOS.Workers.Identity.Api/                  (port: 5008)
└── SpaceOS.Workers.Identity.Tests/
```

**Contracts NuGet v1.5.0** — a current `v1.4.0` (Phase 5 Cutting) kell bumpelni az `IManufacturingProvider` + `IWorkerIdentityProvider` interface-ekkel. Ezt **Day 1 előtt kell megtenni**.

---

## 3. Cross-module dependency — kritikus prereq státusz

⚠️ **A spec header "Cutting Phase 5 DEPLOYED ✅" KORAI jelölés — Phase 5 nincs még deployolva.**

A valódi prereq mátrix (2026-04-28 állapot):

| Prereq | Repo | Státusz | Blocker? |
|--------|------|---------|----------|
| **Cutting Phase 4 DEPLOYED** | `spaceos-modules-cutting` | ✅ DEPLOYED (496 teszt) | — |
| **Cutting Phase 5 DEPLOYED** | `spaceos-modules-cutting` | ❌ MÉG NEM — review + implementáció van folyamatban | **IGEN — Manufacturing nem indulhat** |
| **Modules.Abstractions `DeriveCncPlan()` → `SignedCncPlan`** | `spaceos-modules-abstractions` | ❌ NEM DEPLOYED — jelenleg `IReadOnlyList<CncOperation>` | **IGEN — CncTask.Schedule() invariant erre épül** |
| **Kernel `module_subscriptions` tábla** | `spaceos-kernel` | ❌ HIÁNYZIK — nincs migration | **IGEN — inbox subscription regisztráció** |
| **Kernel `OutboxBackgroundWorker` cross-module HTTP fan-out** | `spaceos-kernel` | ❌ HIÁNYZIK — jelenleg csak `ISignalROutboxFanOut` + `IHashChainOutboxSink`, mTLS HTTP dispatch nincs | **IGEN — Manufacturing Inbox nem kap eseményt** |
| **Contracts v1.4.0** | `spaceos-modules-contracts` | ✅ DEPLOYED | — |
| **Contracts v1.5.0 bump** | `spaceos-modules-contracts` | ❌ NEM — `IManufacturingProvider` + `IWorkerIdentityProvider` hiányzik | Pre-implementation szükséges |

**Összes prereq effort: ~4.5 nap** (párhuzamosan futtatható a Manufacturing scaffold Day 1–4-gyel, de Day 5 saga handler integrációs tesztek ezektől függnek).

### Kernel OutboxBackgroundWorker aktuális állapota

A `OutboxBackgroundWorker.ProcessBatchAsync()` jelenleg:
```
outbox message → ISignalROutboxFanOut (lokális) → IHashChainOutboxSink (audit)
```

Cross-module mTLS HTTP dispatch nincs implementálva. A `module_subscriptions` tábla sem létezik. Mindkettő Kernel terminál feladata (~2.5 nap összesen).

---

## 4. Workers.Identity — megosztott service? Kapcsolódás Cutting worker consent-hez?

**Igen, Workers.Identity a jövőbeli egyetlen authoritative forrás worker identitásra — de a Cutting Phase 4/5 worker consent-jével párhuzamos, nem felváltja Day 1-en.**

### Jelenlegi állapot

- Cutting Phase 4: `IWorkerSecurityPolicy` + `WorkerConsentWithdrawn` event — a Cutting saját belső worker fogalmán alapul
- Manufacturing Phase 1: `WorkerIdentity` aggregate + `spaceos-workers-identity` service — teljes bounded context

### Expand-contract migrációs terv (README Day 9 + Day 17)

| Nap | Teendő |
|-----|--------|
| Day 9 | Cutting Phase 4 **expand-contract Phase 1** — adatmásolás: meglévő Cutting worker adatokat `wi` sémába másol (idempotens SQL migration) |
| Day 17 | Cutting Phase 4 **expand-contract Phase 2** — cutover: `IWorkerIdentityProvider` HTTP hívás Manufacturing → Workers.Identity (`HttpWorkersIdentityProviderClient` BE-04) |

**Kockázat:** A Day 17-es cutover a Cutting Phase 4-et is módosítja. Ez a Cutting terminálnak is feladat. Ajánlás: ezt a lépést Root koordinálja a Cutting terminálnak kiadandó külön inbox üzenettel.

**Workers.Identity v2 (Workers.Identity Phase 1 deployolása után):** TPM, FIDO2, MFA, IP-allowlist — teljesen külön, Phase 1-et nem érinti.

---

## 5. Implementációs sorrend — track-ok

**4 párhuzamos track, 22 nap → ~7-8 wall-time nap.**

| Track | Fókusz | Napok |
|-------|--------|-------|
| **A — Domain + Application** | 3 aggregate (Order saga, EdgeBanding, CNC + PlanIntegrityHash), 10 VO, 10 Specification (BE-03 + AsSplitQuery), 12 command/handler/validator (shape-only), 6 query, Inbox processor, ResultExtensions | Day 1–17 |
| **B — Infrastructure + Persistence** | M-0001..M-0004 migrations, DbContext + OutboxInterceptor (BE-01), Repository-k (spec-only), InboxHmacVerifier (BE-05 Singleton), IHttpClientFactory named clients (BE-04), mTLS + SPKI pin | Day 1–17 |
| **C — API + Inbox + Tests** | Minimal API endpoints, InboxEndpoints (SEC-08 mTLS-only), OpenAPI snapshot (BE-10), ≥215 teszt, performance tests | Day 1–17 |
| **D — Workers.Identity** | Dedikált track, `spaceos-workers-identity` repo teljes implementáció | Day 1–12 |

**Kritikus szinkronizáló pontok:**
- Day 5: Saga handler integráció — prereq Kernel + Abstractions kell ✅/❌
- Day 9: Cutting expand-contract Phase 1 (data copy) — Track D
- Day 14: mTLS integration — Kernel OutboxDispatcher kész kell legyen
- Day 17: Cutting expand-contract Phase 2 cutover — külön Cutting terminál task

**Inbox bontás ajánlott sorrendje (4 inbox + prereq inbox-ok):**

| # | Terminál | Tartalom |
|---|----------|----------|
| Prereq #0 | Kernel | `module_subscriptions` tábla + cross-module HTTP fan-out OutboxBackgroundWorker kiterjesztés (~2.5 nap) |
| Prereq #1 | Abstractions | `DeriveCncPlan()` → `SignedCncPlan` patch + Contracts v1.5.0 bump (~1 nap) |
| Prereq #2 | Cutting | Phase 5 implementáció (folyamatban) |
| Manufacturing #1 | Manufacturing (Track A) | Domain + Application teljes 22 nap |
| Manufacturing #2 | Manufacturing (Track B) | Infrastructure + Persistence teljes 22 nap |
| Manufacturing #3 | Manufacturing (Track C) | API + Tests teljes 22 nap |
| Manufacturing #4 | Manufacturing (Track D) | Workers.Identity teljes 12 nap |

**A Prereq #0 és #1 párhuzamosan futhatnak a Manufacturing scaffold Day 1–4-gyel, de Day 5-től a saga integration tesztek blokkolva vannak ha a Kernel prereq nincs kész.**

---

## 6. Effort validálás — ~22-26 nap reális?

**22 nap szoros de reális — 26 nap biztonságos becslés. 4 párhuzamos agent-tel ~7-8 wall-time nap.**

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---------|--------------|-------|-----------|
| Cutting Phase 5 deploy csúszik | Közepes | Manufacturing teljes blokkolás | Phase 5 prioritizálva (Phase 4 prereq teljesül) |
| Kernel cross-module fan-out komplexebb mint becsült | Közepes | +1-2 nap prereq | mTLS + HMAC spec jól körülírt a tervdokban |
| Workers.Identity KEK rotation TPM nélkül (VPS) | Közepes | Fallback-only, de működik | Stub provider + opt-in per-tenant |
| Cutting expand-contract Phase 2 cutover | Alacsony | Cutting + Manufacturing koordináció | Root kiadja a Cutting terminálnak Day 17 körül |
| 215 teszt szoros határidő | Közepes | Test count gate miss | Track C dedikált, legyen buffer nap |

---

## 7. Port conflict — 5006 (Procurement!) és 5008

⚠️ **KONFIRMED CONFLICT: Procurement port 5006, DEPLOYED.**

| Service | Spec port | Tényleges helyzet | Javasolt port |
|---------|-----------|-------------------|---------------|
| Procurement | 5006 | DEPLOYED ✅ | 5006 (változatlan) |
| **Manufacturing** | 5006 | **CONFLICT** | **5007** |
| **Workers.Identity** | 5008 | Szabad (nincs konflikt) | **5008** ✅ |

**Deployment runbook, Nginx config, systemd service, Orchestrator proxy — mind 5007-re kell beállítani Manufacturing-hoz.**

### Bónusz — PartnerTier port conflict

Az MSG-ARCH-014 (PartnerTier Architecture) válaszomban a `SpaceOS_PartnerTier_Architecture_v1.md` dokumentum szintén **port 5006**-ot specifikál. Ez is konfliktusos Procurement-tel. Javaslat:
- PartnerTier: **5009**

Root frissítse a PartnerTier architekturális dokumentumot (§10 Deployment) 5006 → 5009-re, mielőtt a PartnerTier implementáció inbox üzenete elmegy.

---

## Architecture notes (ADR-javaslatok)

| ADR | Döntés | Indok |
|-----|--------|-------|
| ADR-MFG-01 | `ManufacturingOrder` counter columns — nem külön aggregat | Distributed counter overhead > single aggregate lock |
| ADR-MFG-02 | Inbox-pattern (pull, HTTP mTLS) — nem event bus | Az SpaceOS jelenlegi infra nem tartalmaz message broker-t |
| ADR-MFG-03 | Workers.Identity külön bounded context — nem Cutting bővítés | Single responsibility: worker auth ≠ machining logic |

---

## Definition of Done — ellenőrzőlista

- [x] Tervdok elolvasva és validálva (43 finding, 0 nyitott CRITICAL/HIGH)
- [x] Scope határ tiszta (Phase 1 vs Phase 2 vs Logistics)
- [x] Két új repo: scaffold terv kész, Contracts v1.5.0 bump szükséges
- [x] Cross-module prereq audit: **3 blokkoló prereq hiányzik** (Cutting Phase 5 deploy, Kernel fan-out, Abstractions SignedCncPlan)
- [x] Workers.Identity: megosztott service, expand-contract Day 9 + Day 17 migrációs terv
- [x] Implementációs sorrend: 4 track × 22 nap → ~7-8 wall-time nap
- [x] Effort: 22-26 nap reális
- [x] **PORT CONFLICT**: Manufacturing spec 5006 → **5007** kell; Workers.Identity 5008 OK
- [x] **BÓNUSZ PORT CONFLICT**: PartnerTier spec 5006 → **5009** kell
- [x] Outbox response kész

