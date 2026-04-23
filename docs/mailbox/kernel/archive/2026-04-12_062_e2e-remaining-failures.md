---
id: MSG-KERNEL-062
from: root
to: kernel
type: task
priority: high
status: DONE
ref: MSG-E2E-006-DONE
created: 2026-04-12
---

# MSG-KERNEL-062 — E2E maradék Kernel hibák (115/120 után)

## Kontextus

Az E2E-006 rerun 115/120 eredményt hozott (előző: 105/120). A maradék 5 failből **4 Kernel-oldali**. Mindegyik diagnosztizálva van — részletek alább.

---

## 1. feladat — `05-flowepic-lifecycle` PUT /close → 500

**Teszt:** `src/chain/05-flowepic-lifecycle.chain.test.ts:83`
**Hívás:** `PUT /bff/api/flow-epics/{id}/close`
**Kapott:** 500 (várt: 200/404/422)

**Diagnosztika:**
- Migration 0028 alkalmazva → `CurrentStageCode`, `StageChainTemplateId` oszlopok léteznek
- A close tranzíció valószínűleg null `StageChainTemplate` esetén dob kivételt, vagy a stage transition validator nem kezeli azt az esetet, hogy a FlowEpic nincs stage chain-hez rendelve

**Vizsgálandó:**
```csharp
// FlowEpicEndpoints.cs vagy FlowEpicService.cs — Close/CloseAsync handler
// 1. Mi dob 500-at? (log a VPS-en: journalctl -u spaceos-kernel -n 50)
// 2. Null reference a StageChainTemplate navigáción?
// 3. Stage transition validator dobja-e az exception-t?
```

**Várható fix:** null-safe close path (ha a FlowEpic nincs stage chain-hez rendelve, close-olható null check nélkül is).

---

## 2. feladat — `15-nodes-sync` POST /nodes/register → 500

**Teszt:** `src/chain/15-nodes-sync.chain.test.ts:36`
**Hívás:** `POST /bff/nodes/register` (SIP header-rel)
**Kapott:** 500 (várt: 200/201/409)

**Diagnosztika:**
- Korábban file-level failure miatt skip volt → most először fut le → korábban nem látott regresszió
- Valószínű okok:
  - (a) Nodes schema probléma (migration alkalmazási sorrend)
  - (b) SIP header parsing exception
  - (c) NodeRegistration validátor null reference
- VPS logból azonosítható: `journalctl -u spaceos-kernel | grep -A5 "500\|Exception\|register"`

**Vizsgálandó:**
```csharp
// NodeEndpoints.cs — Register handler
// Ellenőrizd: mi az exception típusa a VPS logban
// Ha schema issue: \d "Nodes" psql-ben
```

---

## 3–4. feladat — `24-tenant-summary` flowEpicCount / activeWorkstationCount = 0

**Teszt:** `src/chain/24-tenant-summary.chain.test.ts:68` és `:90`
**Hívás:** `GET /bff/api/tools/summary`
**Kapott:** 200 OK, de `flowEpicCount: 0` (várt: 1) és `activeWorkstationCount: 0` (várt: 1)

**Diagnosztika:**
A GetTenantId() fix hatására a 401 eltűnt — a kérés eléri a queryt. De a count 0, ami RLS/tenant kontextus mismatch-re utal.

**Lehetséges okok:**
- (a) **RLS SET nem fut le a summary query előtt** — az `app.current_tenant_id` session variable nem kerül beállításra a `GetSummary()` handler-ben. Más endpointok (pl. FlowEpic create) beállítják, de ha `ToolEndpoints.GetSummary()` saját DbContext connection-t nyit, a SET esetleg kimarad.
- (b) **Tenant mismatch** — a teszt más tenanttal hozza létre a FlowEpic-et (pl. facility creation során), mint amit a `GetTenantId()` visszaad a summary híváskor.

**Vizsgálandó:**
```csharp
// ToolEndpoints.cs — GetSummary handler
// 1. Van-e SET app.current_tenant_id a query előtt?
// 2. Ha igen, milyen értékre? Ugyanaz-e, mint a FlowEpic TenantId-je?
// 3. Scalar subquery: SELECT COUNT(*) FROM "FlowEpics" WHERE "TenantId" = @tenantId
//    → Ha RLS FORCE ALL engedélyezve, a SET kötelező

// Debug tip: ideiglenes log a tenantId értékéről GET /tools/summary híváskor
```

---

## Definition of Done

- [ ] `05-flowepic-lifecycle` → 200 (PUT /close nem 500)
- [ ] `15-nodes-sync` → 201/409 (POST /nodes/register nem 500)
- [ ] `24-tenant-summary` → `flowEpicCount: 1`, `activeWorkstationCount: 1`
- [ ] Meglévő **1075 teszt zöld** (0 regresszió)
- [ ] VPS loggal verifikált root cause minden hibánál
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-KERNEL-062-DONE`

## Kontextus: E2E jelenlegi állapot

```
115 pass / 5 fail / 0 skip (120 összesen)
4 fail = Kernel · 1 fail = Orchestrator (MSG-ORCH-058)
```
