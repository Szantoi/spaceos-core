# SpaceOS — Sales Core Front-Office Contract Reconciliation
## Prototípus (`window.sim`) ↔ Modules.Sales v4 ↔ Portal World v4-final

> **Verzió:** v1.1 — 2026-06-15
> **Státusz:** ✅ IMPLEMENTÁCIÓRA KÉSZ (0. lépés a Slice 1-hez — nincs új arch-planner pipeline)
> **Slice:** Slice 1 — **Sales Core** (Customer + Quote FSM + Quote→Order konverzió, Sales world off-mock)
> **v1.1 delta:** R1 vékony Walking Skeleton (Slice 1a) a teljes modul (Slice 1b) előtt · R2 foundation-first sequencing (Slice 2 közvetlen utána) · V1/V2 session-0 verify-pontok a track-ekbe építve
> **Döntés-előzmény:** Út C (production Portal + valódi backend); vertikum #1 = belsőépítészet/bútor asztalos platformon; verticalizáció = meglévő T-shape (ADR-018/019/020), nincs új framework
> **Forrásdokumentumok:** `SpaceOS_Modules_Sales_Architecture_v4.md` (kontrakt) · prototípus `CLAUDE.md` / `PROJECT_STATUS.md` / `ENTITY_LINKS.md` (front office) · `SpaceOS_Portal_World_Architecture_v4_final.md` (Portal)
> **Kulcs-megállapítás:** A Sales v4-en **nem kell módosítani** a Slice 1-hez. Minden gap **deferral**, nem amendment.
> **Repo (hand-off):** `spaceos-modules-sales` (új, 5009) · `apps/joinerytech` (Portal, Sales world off-mock) · `spaceos-doorstar-portal/CONTRACT_ISSUES.md` (seed lent)

---

## 0. Mit old meg ez a dokumentum (és mit nem)

| | Tartalom |
|---|---|
| **Megold** | Megnevezi a Slice 1 pontos backend-felületét; leköti a prototípus front-office modelljét a deployolt-ready Sales v4 kontraktra; kijelöli a Portal Sales-world bekötését; seedeli a CONTRACT_ISSUES-t; átadja Claude Code-nak. |
| **NEM old meg** | Nem írja újra a Sales v4-et (kész). Nem tervezi a Brief/Concept/Project/RFQ rétegeket (Slice 2+, külön arch-planner). Nem deployol (Claude Code + VPS runbook dolga). |

**Walking Skeleton (Slice 1a — vékony, ELŐSZÖR):** egy belsőépítész/asztalos tenant felhasználója → **Customer** felvitel (`Register`) → **Quote** (Draft) tételekkel (`Create` + `AddLine`) → **Send** (ContentHash freeze) → **Accept** → **Convert** → outbox → **Joinery `/internal/orders/from-quote`** receiver (KÉSZ) → Order létrejön → Quote `Converted`, `ConvertedOrderId` visszaíródik → a Sales world UI a valódi API-ból renderel (mock le). **Ez a teljes E2E-loop a minimális endpoint-halmazzal** (Customer.Register · Quote.Create/AddLine/Send/Accept/RequestConversion/CompleteConversion + a hozzájuk tartozó GET-ek) — pár nap, és a rendszer „megmozdul" a VPS-en.

**Slice 1b — teljes Sales modul:** a vékony skeleton után a Sales v4 §13 teljes scope-ja (24 endpoint, Reject, Promote/Deactivate/Archive, link/refresh, pipeline funnel + conversion-rate, ≥ teszt-cél). Ugyanaz a kód-bázis, additív bővítés — nincs újraírás.

> **R1 (LOCKED):** Slice 1a → 1b sorrend. A DoD §6-ban a 1a külön exit-gate, a 1b a Sales v4 §8 teljes DoD-ja.
> **R2 (LOCKED):** foundation-first — Slice 2 (Interior/Project, §7) **közvetlenül** a Slice 1 után, külön `spaceos-arch-planner` pipeline-nal; csak akkor párhuzamosítjuk, ha az interior-demó hamarabb kell.

---

## 1. Pre-decisions (lezárva, egy mondat)

| # | Döntés | Választás |
|---|--------|-----------|
| **PD-01** | Slice 1 backend-határa | Csak Customer + flat Quote + konverzió; minden hierarchikus/al-folyamat (Brief, Concept, RFQ, line-tree, Project) deferred. |
| **PD-02** | Sales v4 módosítás Slice 1-hez | **Nincs.** A v4 IMPLEMENTÁCIÓRA KÉSZ kontraktja fedezi a Slice 1 felületet; a gapek deferral-ok. |
| **PD-03** | Prototípus gazdag réteg sorsa | A FE megtartja **kliens-oldali display-only** rétegként a flat persisted Quote felett (nem perzisztál), amíg a megfelelő modul megépül — nincs adatvesztés, nincs strukturális adósság. |
| **PD-04** | Verticalizáció-seam | `QuoteLine.SourceTemplateId` (soft-ref → `Modules.Abstractions` deployolt Graph Engine) = a domain-agnostic kötés; a prototípus `tplId` ide képződik. |

---

## 2. Kontrakt-mapping — prototípus ↔ Sales v4 ↔ Portal

Jelmagyarázat: ✅ MATCH (Slice 1 fedi) · 🔁 NÉV-MAP (FE label-réteg) · ⏸ DEFER (Slice 2+) · ⚠️ CI (kontrakt-gap, lent)

### 2.1 Customer

| Prototípus (`window.sim`) | Sales v4 | Slice 1 |
|---|---|---|
| ügyfél: cég/magánszemély, kontakt | `Customer` (Type Individual/Company, DisplayName, ContactName/Email/Phone, BillingAddress) | ✅ |
| ügyfél-státusz | `CustomerStatus` Lead → Active → Inactive | ✅ |
| adószám (cég) | `CompanyTaxNumber` | ✅ |
| `customerNote` / `customerProfile` / 360° nézet | — | ⏸ (Customer-extension v2 vagy Notes-modul) |
| `ordersForCustomer` (ügyfél rendelései) | cross-module **read** Joinery-ből (ADR-039 sync loopback) | ⏸ (read-aggregátor Slice 1.1) |
| belsőépítész → több végügyfél/site | `LinkedTenantId` csak platform-actorra; B2C site-fa | ⏸ (Brief-fa `site` rétegével együtt) |
| B2B kézfogás link | `LinkToPlatformActor` / `LinkVerificationStatus` (None/Pending/Verified, Kernel B2BHandshake-derived) | ✅ (seam kész; aktiválás Kernel-oldal ⏸) |

### 2.2 Quote (a központi FSM)

| Prototípus | Sales v4 | Slice 1 |
|---|---|---|
| `quotes[].status`: `draft→sent→approved→converted` | `QuoteStatus`: Draft → Sent → **Accepted** → Converted | 🔁 `approved`→`Accepted` FE-label |
| mellék: `rejected` | `Rejected` (+ `RejectionReason`) | ✅ |
| mellék: `expired` | — (OPEN-02, nincs v1-ben) | ⏸ → FE: `ValidUntil` lejárt = **számított** display-állapot, NEM tárolt status |
| mellék: `archived` | `IsArchived` flag (nem FSM-állapot) | ✅ |
| ajánlat tételek + összegek | `_lines[]` + `TotalNet/Vat/Gross` (Domain-computed, RULE 1) | ✅ |
| ajánlat-szám | `QuoteNumber` `Q-YYYY-NNNNN` (race-free generator) | ✅ |
| pénznem | `Currency` (per-quote, HUF default) | ✅ (multi-currency ⏸) |
| `approveQuote(id)` → rendelés | `RequestConversion` → outbox → Joinery → `CompleteConversion(orderId)` | ✅ (a Joinery receiver KÉSZ) |
| `forwardQuote` → gyermek-ajánlat (draft) | — | ⏸ |
| `createFeeQuoteForQuote` (díj-ajánlat + kapu) | — | ⏸ |
| Send-kori immutability | `Send()` → `ContentHash` freeze (lines+totals+customerRef) | ✅ |

### 2.3 QuoteLine

| Prototípus | Sales v4 | Slice 1 |
|---|---|---|
| tétel: leírás, mennyiség, egységár, ÁFA, kedvezmény | `QuoteLine` (Description, Quantity, UnitPrice `Money`, VatRate, DiscountPercent, SortOrder) | ✅ |
| tétel-típus | `QuoteLineType` Product/Service/Custom/Discount | ✅ |
| sablon-hivatkozás (`tplId`) | `SourceTemplateId` (soft-ref → Abstractions Graph Engine) | ✅ **(verticalizáció-seam)** |
| `uid` / `parentUid` / `subMode` (al-tétel hierarchia, Σ-rollup) | — (FLAT lista, csak `SortOrder`) | ⚠️ **CI-001** → Slice 1: flat persist; al-fa display-only kliensen |
| `source {world,kind,ref,label}` forrás-zárt sor (techreq/rfq/concept) | — | ⏸ (a forrás-modulokkal együtt: RFQ/Concept) |
| számozás `10/20/30 + 10.1/10.2` | `SortOrder` (egész) | 🔁 FE számozás-réteg `SortOrder` felett |

### 2.4 Front-office al-folyamatok (mind DEFER)

| Prototípus réteg | Hova tartozik (cél) | Slice |
|---|---|---|
| **Brief-fa** (`quote→site→area→room→furniture→part`, Q&A, history, DMS-link) | greenfield `Modules.Brief` v. Sales-extension | ⏸ Slice 2 |
| **Concept** (koncepció, `startConceptFromQuoteRequest`, díj/bútorsor → ajánlatba) | greenfield `Modules.Concept` / Interior | ⏸ Slice 2 |
| **QuoteRequest** (technical/external al-ajánlat FSM `kert→folyamatban→kesz`) | Sales-extension v. Interior | ⏸ Slice 2 |
| **RFQ** (`createRfqFromQuote`, `importRfqResultToQuote`, forrás-zárt visszaemelés) | greenfield `Modules.Procurement` (v2 KÉSZ!) RFQ-ág | ⏸ Slice 2/3 |
| **composition → BOM** (`compositionToQuote`) | `Modules.Cabinet` (deployolt) + Graph Engine | ⏸ Slice 2 |
| **Project + szakág-koordináció** (`createProjectFromQuote`, dependencies, install-kapu, schedule) | greenfield `Modules.Project` | ⏸ Slice 2 (külön arch-planner) |
| **Floorplan / Térrendezés** (geometria) | greenfield `Modules.Interior` (geometria — §4.5 szerint backend-érték alacsony) | ⏸ Slice 3 |

> **PD-03 garancia:** ezek a rétegek a prototípusban **kliens-oldali display-only**-ként megmaradhatnak a flat persisted Quote felett — a felhasználó látja és validálja a UX-et, miközben szerver-oldalon csak a Slice 1 felület perzisztál. Nincs strukturális adósság: amikor egy réteg modult kap, a megfelelő `source`-zárt sorok és linkek visszaírhatóak.

### 2.5 Portal (Sales world)

| Portal v4-final állapot | Slice 1 igény | Slice 1 |
|---|---|---|
| Sales world = **mock-only** | Live wiring a Sales API-ra | ⚠️ **CI-002** |
| BFF route-tábla: `/bff/api/*` · `/bff/cutting/*` · `/bff/manufacturing/*` — **nincs Sales** | Sales API elérés (2026-04-30 topológia: FE → nginx → `sales:5009` `/sales/api/*`, JWT) | ⚠️ **CI-002** |
| `/w/sales/...` screen-ek | Customer-lista/detail, Quote-lista/detail/szerkesztő, Pipeline funnel | ✅ (screen-ek mappolva §4) |
| token in-memory only (SEC-FE-01) | változatlan | ✅ |

---

## 3. CONTRACT_ISSUES (seed)

> Másolandó: `spaceos-doorstar-portal/CONTRACT_ISSUES.md`

### CI-001 — QuoteLine al-tétel hierarchia hiánya
- **Severity:** NON-BLOCKER
- **Issue:** A prototípus tétel-fája (`parentUid`/`subMode`, Σ-rollup, `source`-zárt sorok) nincs a Sales v4 flat `QuoteLine`-ban.
- **Expected (FE):** hierarchikus tételek al-összegekkel.
- **Actual (BE):** flat lista (`SortOrder`).
- **Workaround (Slice 1):** flat persist a `_lines[]`-ben; az al-fa és a számozás (`10.1`) **kliens-oldali display-only** a flat sorok felett. A `source`-zárt sorok a forrás-modulokkal (RFQ/Concept) érkeznek.
- **Resolution path:** Slice 2 — Quote line-tree mint owned sub-entity VAGY a forrás-modulok `source`-referenciái; nem Slice 1 blokkoló.

### CI-002 — Portal Sales-world route + BFF mapping hiánya
- **Severity:** BLOCKER (Slice 1-re)
- **Issue:** A Portal v4-final BFF-táblája Sales előtti; a Sales world mock-only.
- **Expected (FE):** `/w/sales/*` a valódi Sales API-t hívja.
- **Actual:** nincs Sales upstream/route.
- **Workaround:** —
- **Resolution path:** (a) nginx upstream `sales` → `127.0.0.1:5009`, `location /sales/api/ { proxy_pass ... }` JWT-vel (2026-04-30 topológia, nincs Orchestrator-proxy); (b) Portal React Query base `/sales/api`; (c) `worldCatalog.ts` Sales world `enabledModules`-szűrése `sales` capability-re. **Track-B (FE) feladat.**

### CI-003 — Quote `Expired` állapot hiánya
- **Severity:** NON-BLOCKER
- **Issue:** Prototípus `expired` státusz; Sales v4 FSM nem tartalmazza (OPEN-02).
- **Workaround:** FE `ValidUntil < now` → **számított** "lejárt" badge, nem tárolt status; a funnel az Accepted/Sent szerint számol.
- **Resolution path:** Sales v2 — `Expired` FSM-állapot + funnel-pontosítás.

---

## 4. Portal Sales-world screen → Sales v4 endpoint mapping

| Screen (`/w/sales/...`) | Sales v4 endpoint (`/sales/api/*`) | RBAC |
|---|---|---|
| Customer lista | `GET /sales/api/customers?status&search&skip&take` | TenantUser+ read |
| Customer detail | `GET /sales/api/customers/{id}` | TenantUser+ |
| Customer felvitel/szerkesztés | `POST /sales/api/customers` · `PATCH .../{id}` | SalesUser |
| Customer link refresh (B2B) | `POST /sales/api/customers/{id}/link/refresh` | TenantAdmin |
| Customer promote/deactivate/archive | `POST .../{id}/promote` · `/deactivate` · `/archive` | TenantAdmin |
| Quote lista (funnel-szűrt) | `GET /sales/api/quotes?status&customerId&skip&take` | TenantUser+ |
| Quote detail (+lines) | `GET /sales/api/quotes/{id}` | TenantUser+ |
| Quote létrehozás + tétel add/remove (Draft) | `POST /sales/api/quotes` · `POST .../{id}/lines` · `DELETE .../{id}/lines/{lineId}` | SalesUser |
| Quote Send | `POST /sales/api/quotes/{id}/send` | SalesUser |
| Quote Accept / Reject | `POST .../{id}/accept` · `/reject` | SalesUser |
| Quote Convert | `POST /sales/api/quotes/{id}/convert` (10 req/min/tenant, SEC-S-06) | SalesUser |
| Pipeline funnel | `GET /sales/api/pipeline/funnel` · `/conversion-rate` | TenantUser+ |

> A 24 endpoint pontos shape-je a Sales v4 §6.1-ben; a fenti a Slice 1 FE-binding-listája. A FE label-réteg magyarul jelenít (`Accepted`→"Elfogadva", stb.).

---

## 5. Claude Code hand-off — két track

### Track A — `spaceos-modules-sales` implementáció (backend)
- **Forrás:** `SpaceOS_Modules_Sales_Architecture_v4.md` §13 (~20.5 nap track-plan, DoD §8). **Ez a dokumentum nem ír felül semmit** — a v4 a source of truth.
- **Sorrend (R1):** előbb a **Slice 1a vékony skeleton** (minimális endpoint-halmaz, lásd §0) → E2E-loop élő → utána **Slice 1b** a teljes §13 scope. Ugyanaz a repo, additív.
- **Prereq (mind ✅, lásd Sales v4 header):** Joinery `/internal/orders/from-quote` KÉSZ · Kernel `/api/internal/tenants/{id}` · Keycloak VPS · Modules.Identity DEPLOYED.
- **V1 verify (session-0):** e dokumentum §2 mappingje a prototípus **memória-fájljaiból** készült (nem a `window.sim`/`app-store.jsx` forrásból), a §4 endpoint-tábla a v4 DoD-ból (nem a §6.1 pontos definícióiból). **A §6.1 az autoritatív** — a §4-et ahhoz igazítsd, ha eltér.
- **Indítás:** `spaceos-arch-planner` NEM kell (v4 kész). Közvetlen Claude Code session a v4 §13.2 master prompttal.

#### Discovery (session-nyitó shell)
```bash
ls -la ~/spaceos-modules-sales 2>/dev/null || echo "új repo — scaffold a v4 §4 solution-fa szerint"
psql -h localhost -U gabor -d spaceos -c "\dn" | grep spaceos_sales || echo "schema létrehozandó (S-0001)"
ss -ltnp | grep 5009 || echo "5009 szabad"
curl -s http://127.0.0.1:5002/health && echo " ← Joinery receiver él (konverzió-cél)"
grep -r "from-quote" ~/spaceos-modules-joinery/src 2>/dev/null | head   # receiver megléte
```

#### Agent instruction block
```
You are the spaceos-sales Claude Code agent.
Implement Modules.Sales per SpaceOS_Modules_Sales_Architecture_v4.md (§13 track plan, §8 DoD).
Source of truth = that v4 document. This reconciliation file only scopes Slice 1 and DEFERS
front-office extensions — do NOT implement Brief/Concept/RFQ/line-tree/Project.
Golden Rules + approved packages per repo CLAUDE.md. xUnit v3 + Moq, ConfigureAwait(false),
AsNoTracking() on reads, Result<T> handlers, domain events + PopDomainEvents/Dispatch.
Close every DoD checkbox in Sales v4 §8 before merge. Port 5009 loopback, schema spaceos_sales, RLS FORCE.
On any contract gap vs the prototype FE: write to CONTRACT_ISSUES.md, do NOT invent contract.
```

### Track B — `apps/joinerytech` Sales world off-mock (frontend/BFF)
- **Feladat:** CI-002 feloldása. nginx Sales upstream + `/sales/api/` proxy (JWT, 2026-04-30 topológia) · Portal React Query base `/sales/api` · `worldCatalog.ts` Sales capability-szűrés · a §4 screen→endpoint binding · mock-réteg eltávolítása · CI-001 display-only line-tree megtartása a flat sorok felett · CI-003 számított "lejárt" badge.
- **Prereq:** Track A `sales:5009` DEPLOYED (Slice 1a esetén már a minimális endpoint-halmaz elég a wiringhez).
- **V2 verify (session-0):** a CI-002 nginx-útvonal (`FE → nginx → sales:5009`, Orchestrator-proxy nélkül) a 2026-04-30 topológia-feltevésből jön; a Portal v4-final BFF-táblája még `/bff/*`-ot mutat. **Erősítsd meg a tényleges jelenlegi Portal→modul wiringet** (nginx conf + Portal API base) a binding előtt — ha eltér, a CI-002 resolution-patht ahhoz igazítsd.
- **Megkötés:** token in-memory only (SEC-FE-01); a deferred rétegek (Brief/Concept/Project) UI-ja maradhat prototípus-szinten display-only, de **nem** köt élő API-ra.

---

## 6. Definition of Done

### 6a. Slice 1a — vékony skeleton exit (ELŐSZÖR)
- [ ] `sales:5009` systemd DEPLOYED, loopback-only, schema `spaceos_sales` RLS FORCE
- [ ] Minimális endpoint-halmaz él: Customer.Register · Quote.Create/AddLine/Send/Accept/RequestConversion/CompleteConversion + GET-ek
- [ ] **E2E happy-path:** Customer → Quote(Draft+lines) → Send → Accept → Convert → Joinery Order létrejön → Quote `Converted` + `ConvertedOrderId` ✅ (staging Doorstar tenant)
- [ ] V1 verify lezárva (§4 a §6.1-hez igazítva)
- [ ] Track B: Sales world a vékony felületet a valódi API-ból rendereli (mock le erre a részre); V2 verify lezárva; CI-002 zárva
- [ ] Token DevTools-verify: 0 token localStorage-ben (SEC-FE-01)

### 6b. Slice 1b — teljes Sales modul exit
- [ ] **Track A:** Sales v4 §8 DoD minden gate ✅ (24 endpoint, Reject/Promote/Deactivate/Archive, link/refresh, pipeline funnel + conversion-rate, ≥ teszt-cél, 0 warning)
- [ ] CI-001 / CI-003 NON-BLOCKER workaround a UI-ban verifikálva (display-only fa + számított "lejárt")
- [ ] `CONTRACT_ISSUES.md` aktualizálva (CI-001/002/003 státusz)
- [ ] `Codebase_Status_YYYYMMDD.md` frissítve (Sales DEPLOYED, új teszt-count, új migration-count)
- [ ] Deferred-lista (§2.4) rögzítve a Slice 2 backlog-ban (Brief/Concept/RFQ/line-tree/Project/Floorplan)

---

## 7. Mi jön Slice 1 után (nem most)

| Sorrend | Slice | Tartalom | Prereq |
|---|---|---|---|
| 2 | **Interior/Project greenfield** | `Modules.Project` (szakág-koordináció) + Concept/Brief; külön `spaceos-arch-planner` v1→v4 | Sales DEPLOYED (Customer/Quote alap) |
| 2.x | RFQ-ág | `Modules.Procurement` v2 (KÉSZ) RFQ + `source`-zárt visszaemelés | Sales + Procurement v2 deploy |
| 3 | Floorplan/Térrendezés | `Modules.Interior` geometria (backend-érték alacsony — utolsó) | Slice 2 |

---

*SpaceOS — Sales Core Front-Office Contract Reconciliation v1.1 · 2026-06-15 · Architect session*
*0. lépés a Slice 1-hez · Sales v4 változatlan · R1 vékony skeleton (1a) → teljes modul (1b) · R2 foundation-first · V1/V2 verify a session-0 checklistjén · 3 CONTRACT_ISSUE · 2 Claude Code track · 7 deferred réteg*
