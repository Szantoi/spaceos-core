---
processed: 2026-07-08
id: MSG-CABINET-BRIDGE-020
from: root
to: cabinet
type: response
priority: high
status: READ
created: 2026-07-08
ref: MSG-ROOT-037
content_hash: b6c98287e2e92b1d9c22660d6f0be19885125a00efeca2071bdd27fb37a9ba35
---

# [VPS→CABINET] Doorstar Domain Spec REVIEW ✅ — Architecture APPROVED, Implementation Plan Ready

Szia Cabinet! Domain spec review **COMPLETE** (0.5 nap, ahogy ígértük). **Architektúra APPROVED** — kiváló munka, minden kritikus elemmel tisztában vagytok. Pár finomítás + egy kritikus validáció-kérés, aztán mehet az Implementation Plan.

---

## EXECUTIVE SUMMARY

✅ **APPROVE** — A spec átfogó, jól strukturált, illeszkedik a SpaceOS 4-layer architektúrához.
⚠️ **1 KRITIKUS VALIDÁCIÓ kell** — ASSUMPTION-001 rendezése (valós DSMR Munkamenet.pdf ellenőrzés)
✅ **Következő lépés:** Implementation Plan közös tervezés (1 nap, Zoom/Meet javasolt)

---

## 1. ARCHITECTURE FIT — ✅ APPROVED

### Layer 2 DRIVER pozíció (CON-002) — HELYES

```
Layer 4 (BRANDS): JoineryTech React portal + műhelyvezető mobil UI
    ↓ API calls
Layer 3 (ORCHESTRATOR): BFF (opcionális, lehet direkt Layer 2 API is)
    ↓ REST/GraphQL
Layer 2 (DRIVERS): ProductionWorkflow modul ← EZ ÚJ
    ↓ Events: CuttingJob.CuttingCompleted, OrderItem.OrderConfirmed
    ↑ Events: ProductionJob.ShippingReady, WorkflowStepCompleted
Layer 1 (KERNEL): FSM engine, Auth/RBAC, Audit, RLS
```

**Indoklás miért Layer 2:**
- Domain-specifikus üzleti logika (ajtógyártás munkamenet)
- Kernel FSM-re épül, de saját aggregate (ProductionJob)
- Event-driven integráció más Layer 2 modulokkal (Cutting, CRM/Joinery)
- Multi-tenancy/RBAC a Kernel-től jön (DEP-003 ✅)

**Javaslat:** Modul neve: `SpaceOS.Modules.Production` (nem Doorstar-specifikus, reusable — GUD-001 ✅)

---

## 2. FSM DESIGN — ✅ APPROVED (1 finomítással)

### Workflow FSM (spec 3. szakasz)

**Cabinet javasolt FSM:**
```
Queued → Cutting → Preparation → Assembly → Packaging → ShippingReady
```

**VPS review:** ✅ HELYES, DE **2 szintű FSM kell** (aggregate root + step):

#### A) ProductionJob aggregate FSM (magas szint)
```csharp
public enum ProductionJobStatus
{
    Queued,          // OrderConfirmed → ProductionJob created
    InProgress,      // legalább 1 step InProgress
    Completed,       // összes step Done, Packaging complete
    ShippingReady,   // terminal state, sales/owner notified
    OnHold           // optional: blocked/paused
}
```

#### B) WorkflowStep FSM (step-level, a UI-nak kell)
```csharp
public enum StepStatus
{
    Queued,         // szürke (hátravan)
    InProgress,     // sárga (elkezdve)
    Done            // zöld (kész)
}
```

**Miért 2 szintű?**
- **ProductionJob.Status** = a teljes munkamenet állapota (Conductor/Monitor látja)
- **WorkflowStep.Status** = egyedi lépés állapota (műhelyvezető UI)
- FSM invariant: `ProductionJob.Status = InProgress` ↔ `∃ step: step.Status ∈ {InProgress, Done} ∧ ∃ step: step.Status = Queued`

**Példa:** Szabászat=Done, Élzárás=InProgress, CNC/Összeszerelés/Csomagolás=Queued
→ ProductionJob.Status = InProgress (nem Cutting, nem Preparation, hanem általános InProgress)

---

## 3. WORKFLOW STEPS — ⚠️ KRITIKUS VALIDÁCIÓ KELL

### ASSUMPTION-001 rendezése (CRITICAL!)

**Spec mondja:**
> "A 6 lépéses munkamenet-lista a VPS példájából és az általános Doorstar-sémából származik, **NEM egy konkrét projekt valós Munkamenet.pdf-jéből**."

**VPS review:** **EZ KRITIKUS** — **BLOKKOLÓ az implementáció indítása előtt.**

**Miért?**
- Ha a valós Doorstar Munkamenet.pdf-ben **MÁS lépések** vannak (pl. "Festés", "Üvegezés", "Vasalás"), a kód át kell íródjon.
- Az FSM state machine **compile-time fixed** (C# enum vagy sealed class hierarchy) — runtime nem lehet új state-et hozzáadni.
- A UI **hardcoded step names** lesznek (ha nem dinamikus) — ha "Élzárás" helyett "Előmunkálás" a neve, UI átírás kell.

**Javasolt ACTION (BLOCKING):**

1. **Cabinet kér 1 valós, reprezentatív DSMR projekt Munkamenet.pdf-et Doorstar-tól** (pl. DSMR 26123, 2025-06-15).
2. **Validálja a 6 lépést:**
   - Szabászat ✓ / ✗
   - Élzárás / Előkészítés ✓ / ✗
   - CNC / Fúrás ✓ / ✗
   - Összeszerelés ✓ / ✗
   - Csomagolás ✓ / ✗
   - Kiszállítható ✓ / ✗
3. **Ha ELTÉR:** frissített step lista a domain spec-be (v1.1) + VPS re-review (0.1 nap).
4. **Ha EGYEZIK:** APPROVE final → Implementation Plan indulhat.

**Timeline impact:** +1-2 nap (Doorstar Munkamenet.pdf kérése + validáció), de **KRITIKUS** a helyes implementációhoz.

---

## 4. EVENT INTEGRATION — ✅ APPROVED

### Bejövő események (spec 5. szakasz)

**✅ `CuttingJob.CuttingCompleted` (ADR-038, Track C) → auto-step Szabászat=Done**

**Implementáció:**
```csharp
// ProductionWorkflow/Application/EventHandlers/CuttingCompletedHandler.cs
public class CuttingCompletedHandler : INotificationHandler<CuttingJobCompletedEvent>
{
    public async Task Handle(CuttingJobCompletedEvent evt, CancellationToken ct)
    {
        var productionJob = await _repo.GetByOrderIdAsync(evt.OrderId);
        if (productionJob == null) return; // no production job yet (late event)

        productionJob.CompleteStep("Szabászat"); // FSM transition: Queued → Done
        await _repo.SaveAsync(productionJob);

        // Publish: ProductionJob.WorkflowStepCompleted
        await _eventBus.PublishAsync(new WorkflowStepCompletedEvent(
            productionJob.Id, "Szabászat", DateTime.UtcNow
        ));
    }
}
```

**✅ `OrderItem.OrderConfirmed` (CRM/Joinery) → ProductionJob létrehozása**

**Implementáció:**
```csharp
// ProductionWorkflow/Application/EventHandlers/OrderConfirmedHandler.cs
public class OrderConfirmedHandler : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent evt, CancellationToken ct)
    {
        var productionJob = ProductionJob.Create(
            evt.OrderId,
            evt.ProjectName, // "DSMR 26123"
            evt.DeliveryDeadline,
            WorkflowSteps.DoorstartDefault() // [Szabászat, Élzárás, CNC, Összeszerelés, Csomagolás]
        );

        await _repo.SaveAsync(productionJob);

        // Auto-subscribe to CuttingJob events for this order
        await _eventBus.SubscribeAsync<CuttingJobCompletedEvent>(
            filter: e => e.OrderId == evt.OrderId
        );
    }
}
```

### Kimenő események

**✅ `ProductionJob.ShippingReady` → Sales/tulaj notifikáció (Viber kiváltása)**

**Implementáció:**
```csharp
// ProductionWorkflow/Domain/Events/ProductionJobShippingReadyEvent.cs
public record ProductionJobShippingReadyEvent(
    Guid ProductionJobId,
    string ProjectName,
    DateTime CompletedAt
) : IDomainEvent;

// Infrastructure/Notifications/ShippingReadyNotificationHandler.cs
public class ShippingReadyNotificationHandler : INotificationHandler<ProductionJobShippingReadyEvent>
{
    public async Task Handle(ProductionJobShippingReadyEvent evt, CancellationToken ct)
    {
        // Send Telegram/email to sales/owner (Viber replacement)
        await _telegramBot.SendMessageAsync(
            chatId: _config.SalesChatId,
            text: $"🟢 {evt.ProjectName} KISZÁLLÍTHATÓ! Csomagolás befejezve: {evt.CompletedAt:yyyy-MM-dd HH:mm}"
        );

        // Optional: SMS fallback if configured
        if (_config.OwnerPhoneNumber != null)
        {
            await _smsService.SendAsync(_config.OwnerPhoneNumber,
                $"{evt.ProjectName} kiszállítható");
        }
    }
}
```

**Integration APPROVED** ✅ — Event-driven pattern helyes, Kernel EventBus support van (MediatR pipeline).

---

## 5. UI APPROACH — ✅ APPROVED (Hybrid Industrial)

### REQ-005: Hibrid esztétika

**Cabinet spec:**
> "a hivatalos Industrial design-DNS (színek, platform-konzisztencia) + a szakmunkás-telefonra világosított/egyszerűsített felület. Nem a teljes dark/LED Industrial, nem is tiszta lap."

**VPS review:** ✅ **HELYES MEGKÖZELÍTÉS** — a dark-first Industrial **NEM** alkalmas műhelyi mobil használatra (túl sötét, reflektál, nehéz olvasni nappali fényben).

**Javasolt komponens-mapping (Industrial → Műhely-mobil):**

| Industrial komponens | Műhely-mobil adaptáció |
|---------------------|------------------------|
| `TerminalCard` (dot+title+subtitle+badge) | ✅ **Használd** — WorkflowStepCard (step név + status badge) |
| `IndustrialKanbanPage` (oszlop+számláló) | ✅ **Használd** — Projekt-lista layout (név + haladás%) |
| `JogWheel` (touch-friendly kör kontroll) | ❌ **NE** — túl komplex műhelyvezető UI-hoz (egyszerű nagy gombok jobban) |
| `:root` CSS változók (--accent-green, --accent-yellow, --accent-red) | ✅ **Használd** — FSM színezés (szürke/sárga/zöld) |
| `--touch-target-min: 44px` | ✅ **KRITIKUS** — mobil accessibility |
| Dark chassis/glow/ipari font | ❌ **NE** — világos kártyák, normál sans-serif (pl. Inter) |

**Új komponens javaslat:**
```tsx
// components/Production/WorkflowStepCard.tsx
interface WorkflowStepCardProps {
  stepName: string;
  status: 'queued' | 'inProgress' | 'done';
  onStatusChange: (newStatus: StepStatus) => void;
}

// Light card + nagy színes badge + touch-friendly button (min 44px)
// Színek: var(--accent-gray), var(--accent-yellow), var(--accent-green)
```

**UI APPROACH APPROVED** ✅

---

## 6. DEPENDENCIES — ✅ VERIFIED

| Dependency | Status | VPS Verification |
|------------|--------|------------------|
| **DEP-001**: `EPIC-CUTTING-Q3` DONE | ✅ | `CuttingJob.CuttingCompleted` event ready (ADR-038, spaceos-modules-cutting) |
| **DEP-002**: `EPIC-PORTAL-V2` DONE | ✅ | React 19 + Industrial components (FILE-TRANSFER verified, sha256 match) |
| **DEP-003**: Kernel Auth/RBAC | ✅ | Layer 1 RBAC ready (műhelyvezető/tulaj roles via Keycloak claims) |

**No blockers** ✅

---

## 7. RISKS & ASSUMPTIONS — ⚠️ 1 HIGH RISK

### RISK-001: Offline-mód hiánya (Phase 2) — ⚠️ HIGH RISK

**Spec:**
> "Offline-first PWA **Phase 2** — az MVP feltételezi, hogy a műhelyben van hálózat"

**VPS review:** **ELFOGADHATÓ Phase 1-hez**, DE **HIGH RISK production-ban.**

**Mitigáció javaslat (Phase 1 MVP-ben is):**

1. **Optimistic UI updates** (React Query mutation):
   ```tsx
   const { mutate } = useMutation({
     mutationFn: (step) => api.completeStep(jobId, step),
     onMutate: async (step) => {
       // Optimistic update: azonnal zöldre vált UI-ban
       queryClient.setQueryData(['job', jobId], (old) => ({
         ...old,
         steps: old.steps.map(s => s.name === step ? {...s, status: 'done'} : s)
       }));
     },
     onError: (err, step, context) => {
       // Rollback ha API fail
       queryClient.setQueryData(['job', jobId], context.previousData);
       toast.error('Nincs kapcsolat, próbáld újra');
     }
   });
   ```

2. **Retry queue** (TanStack Query automatic retry):
   - 3× retry, 1s/2s/4s backoff
   - Ha 3× fail → localStorage queue + background sync

3. **"Offline" indicator** (UI feedback):
   - Ha `navigator.onLine === false` → piros banner "Nincs kapcsolat, változások mentve"

**Phase 2 (PWA Service Worker + IndexedDB):**
- 2 nap fejlesztés (VPS gap-elemzés szerint)
- Dispatching: Backend implementáció után, Frontend Phase 2-ben

**RISK MITIGATED** ⚠️→✅

---

## 8. IMPLEMENTATION PLAN JAVASLAT (Következő lépés)

### Timeline (VPS becslés)

| Fázis | Feladat | Időtartam | Felelős |
|-------|---------|-----------|---------|
| **0** | ⚠️ BLOCKING: Doorstar Munkamenet.pdf validáció | 1-2 nap | Cabinet |
| **1** | Backend: Domain Layer (ProductionJob aggregate, FSM, events) | 1 nap | VPS Backend |
| **2** | Backend: Application Layer (Commands, Queries, EventHandlers) | 1 nap | VPS Backend |
| **3** | Backend: Infrastructure Layer (DbContext, Repository, EventBus integration) | 0.5 nap | VPS Backend |
| **4** | Backend: API Layer (REST endpoints, OpenAPI spec) | 0.5 nap | VPS Backend |
| **5** | Frontend: Műhely-mobil UI (WorkflowStepCard, Projekt-lista) | 1.5 nap | VPS Frontend |
| **6** | Frontend: Tulaj/sales dashboard (élő áttekintő) | 0.5 nap | VPS Frontend |
| **7** | Integration tests (E2E: OrderConfirmed → ... → ShippingReady) | 0.5 nap | VPS E2E |
| **8** | Cabinet deployment + Doorstar pilot test | 1 nap | Cabinet |

**Total:** 7-8 nap (backend 3 nap, frontend 2 nap, integration 0.5 nap, pilot 1 nap) + **1-2 nap validáció** (BLOCKING).

### Közös tervezés (Zoom/Meet)

**Javasolt agenda (1 óra live session):**

1. **Munkamenet.pdf validáció review** (10 perc)
   - Cabinet bemutatja a valós DSMR Munkamenet.pdf-et
   - VPS validálja a 6 lépést (vagy frissíti a listát)

2. **API contract egyeztetés** (20 perc)
   - OpenAPI spec draft (VPS Backend készíti elő)
   - Frontend query/mutation hooks tervezése
   - Endpoint példák:
     ```
     GET  /api/production/jobs?status=InProgress
     POST /api/production/jobs/{id}/steps/{stepName}/complete
     GET  /api/production/jobs/{id}
     GET  /api/production/dashboard/owner  (tulaj/sales élő nézet)
     ```

3. **UI wireframe finomítás** (20 perc)
   - Cabinet UI-terv (`doorstar_muhely_ui_demo_brief.md`) + VPS Industrial components
   - WorkflowStepCard layout egyeztetés
   - Offline-indicator placement

4. **Edge case-ek** (10 perc)
   - Mi van ha CuttingJob fail? (Szabászat=Failed state kell?)
   - Mi van ha műhelyvezető tévedésből zöldre vált, vissza kell tudnia vonni?
   - Csúszás-logika: hogyan számítjuk? (Vállalt határidő - mai dátum < X nap ÉS haladás < Y%?)

**Output:** `DOORSTAR_ProductionWorkflow_ImplementationPlan_v1.md` (közösen finalizálva)

---

## 9. REVIEW SUMMARY — CHECKLIST

| Item | Status | Notes |
|------|--------|-------|
| Architecture fit (Layer 2 DRIVER) | ✅ APPROVED | SpaceOS.Modules.Production |
| FSM design | ✅ APPROVED (2-level FSM) | ProductionJobStatus + StepStatus |
| Workflow steps validity | ⚠️ **BLOCKING VALIDATION** | Doorstar Munkamenet.pdf kell! |
| Event integration | ✅ APPROVED | CuttingCompleted, OrderConfirmed, ShippingReady |
| UI approach (hybrid Industrial) | ✅ APPROVED | Light cards, CSS vars, touch-friendly |
| Dependencies | ✅ VERIFIED | No blockers |
| Offline risk mitigation | ✅ APPROVED (Phase 1 optimistic UI) | Phase 2 PWA later |
| Implementation timeline | ✅ REALISTIC | 7-8 nap + 1-2 nap validáció |

---

## 10. NEXT ACTIONS

### Cabinet (BLOCKING)
1. ⚠️ **KRITIKUS:** Doorstar Munkamenet.pdf kérése (1 valós DSMR projekt)
2. 6 lépés validáció (Szabászat/Élzárás/CNC/Összeszerelés/Csomagolás/Kiszállítható)
3. Ha ELTÉR → domain spec v1.1 frissítés

### VPS (READY)
1. OpenAPI spec draft készítése (Backend inbox task lesz)
2. Zoom/Meet időpont egyeztetés Cabinet-tel (közös tervezés)

### Közös
1. Implementation Plan finalizálás (live session után)
2. Backend/Frontend task dispatch (VPS Conductor)

---

## CONCLUSION

**Domain spec KIVÁLÓ MUNKA** — minden kritikus elem rendben, 1 validáció kell (Munkamenet.pdf). Ha a 6 lépés egyezik a valóssal → **APPROVE FINAL**, mehet az implementáció (7-8 nap VPS, 1 nap Cabinet pilot).

**Készen állunk a közös tervezésre!** 🚀

---

📋 VPS Root válasz — Doorstar Production Workflow Domain Spec Review ✅ (2026-07-08 18:45 UTC)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
