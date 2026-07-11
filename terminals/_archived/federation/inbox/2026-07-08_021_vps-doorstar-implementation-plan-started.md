---
processed: 2026-07-08
id: MSG-CABINET-BRIDGE-021
from: root
to: cabinet
type: info
priority: high
status: READ
created: 2026-07-08
ref: MSG-ROOT-040
content_hash: e505a17662837866471b35acd09029566d31292090a6580f8a0f3a1b82021cdc
---

# [VPS→CABINET] Doorstar Implementation Plan Started — Backend dolgozik rajta

Szia Cabinet! Köszi a blocking validation lezárását és a scope finalizálást (MSG-ROOT-040). Nagyszerű munka! 🎯

## Státusz: Backend Task Dispatched

✅ **MSG-BACKEND-194** dispatched: Doorstar Production Workflow Implementation Plan
- OpenAPI contract draft (REST endpoints, DTOs, event publikálás)
- Backend task breakdown (Domain/Application/Infrastructure/API + Integration tests)
- Frontend task breakdown (jelölve, ne implementálja - Frontend terminál fogja)
- Integration points dokumentálása (CuttingJob, OrderItem események)
- Timeline estimate (backend ~4 nap, frontend ~2 nap párhuzamos)

## Scope Confirmation (amit Backend kap)

- ✅ **6 STAGE** végleges (Szabászat/Előgyártás → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható)
- ✅ **2-szintű FSM**: `ProductionJob.Status` (aggregate) + `WorkflowStep.Status` (6 STAGE)
- ✅ **Event integráció**: `CuttingJob.CuttingCompleted` (auto-step) + `OrderItem.OrderConfirmed` (job creation)
- ✅ **Mobil-first UI**: Koppintós STAGE progress, real-time push tulajnak/sales-nek
- ✅ **Layer 2 DRIVER**: `spaceos-modules-production` (.NET 8, DDD/CQRS/FSM)

## Timeline

- **Backend Implementation Plan**: 1-2 nap (OpenAPI draft + task breakdown + estimate)
- **Review + finomítás**: 0.5 nap (Cabinet + VPS)
- **Implementáció indítás**: utána azonnal (backend 4 nap, frontend 2 nap párhuzamos)

## Következő Lépések

1. **Backend DONE várás**: MSG-BACKEND-194 outbox (1-2 napon belül)
2. **OpenAPI draft megosztás**: Cabinet feedback-re (aszinkron vagy Zoom/Meet egyeztetés)
3. **Implementáció indítás**: Backend + Frontend párhuzamos fejlesztés (4-6 nap)
4. **Pilot test prep**: Cabinet staging deploy + Doorstar műhelyvezető pilot (Week 5-6?)

## Aszinkron vagy Sync Egyeztetés?

Ti javasoltátok közös session-t (Zoom/Meet) az OpenAPI contract egyeztetéshez. Nekünk mindkettő jó:

- **Aszinkron**: Backend készít draft → ti review-zzátok a hídon → iterálunk
- **Sync**: Backend draft után Zoom/Meet (30-60 perc, API contract + edge cases)

Melyik a jobb nektek? Időpont javaslat (ha sync)?

---

📋 VPS Root válasz — Doorstar Implementation Plan Started (2026-07-08 22:31 UTC)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
