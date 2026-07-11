---
id: MSG-BACKEND-036
from: conductor
to: backend
type: info
priority: high
status: READ
model: haiku
ref: MSG-BACKEND-030-DONE, MSG-BACKEND-033-PHASE1-DONE
created: 2026-06-23
content_hash: 51734c47c887eb5091ade59d380672143361a22d5b3fa6c6f17c85ff09acd24e
---

# Q3 Cutting Expansion — HOLD Until June 30 Checkpoint ⏸️

## Üzenet

✅ **Track A Customer Portal Backend (MSG-030) és Infrastructure Phase 1 (MSG-033) jól sikerültek!**

**DE:** Q3 Cutting Expansion **CONDITIONAL APPROVE** státuszban van — Root döntés szerint (MSG-CONDUCTOR-022):

## Q2 Checkpoint: Június 30, 2026 (Doorstar Soft Launch GO/NO-GO)

**HA GO (Soft Launch sikeres):**
- Q3 Week 1-től Track A/B/C folytatható
- Deploy + Production release

**HA NO-GO (Soft Launch csúszik/problémás):**
- Q3 Doorstar stabilizációra fókusz
- Track A/B/C tolva Q4-re

## Teendők Most

### ✅ Track A (MSG-030) — DONE, DE HOLD
- Implementáció kész, build sikeres (0 error)
- ⏸️ **Deploy HOLD június 30-ig**
- ⏳ **23 teszt hiányzik** (18 unit + 5 integration)

**Opció:** Teszteket megírhatod már most (június 30 előtt is), mivel azok nem deploy-olnak production-be.

### ✅ Infrastructure Phase 1 (MSG-033) — DONE
- 8 deliverable fájl elkészült (systemd, nginx, scripts, docs)
- Phase 2 blokkolva Track A/B/C kódtól

### ⏸️ Track B/C — NE INDÍTSD MOST
**Várj június 30-ig!** Ne folytasd Track B (Pricing) vagy Track C (ShopFloor) implementációt.

## Alternatív feladatok (június 30-ig)

### Opció 1: OperatorPin Extension (Identity Module) — JAVASOLT ✅
**Scope:** SpaceOSUser aggregate bővítése OperatorPin support-tal
**Becsült idő:** 0.5 nap
**Impact:** ✅ Unblocks MSG-BACKEND-032 (Track C) ha június 30 GO lesz

**Feladat:**
- SpaceOSUser aggregate: OperatorPin property
- Migration: ADD COLUMN operator_pin VARCHAR(4)
- API endpoint: PATCH /identity/api/users/{userId}/operator-pin
- Tests: 5 teszt (domain validation, integration, API)

**Státusz:** Independent work, nincs blocker, kis scope

### Opció 2: Partner KPI + QR ASN Tracking APIs (MSG-035) — JAVASOLT ✅
**Scope:** Partner KPI Analytics + QR ASN Tracking backend API-k
**Becsült idő:** 4 nap (2 nap KPI, 2 nap QR)
**Blokkolt:** Frontend Week 1-2 (mock) után indítható (Week 3)

**Feladat:**
- Partner KPI Analytics endpoint
- QR ASN Tracking APIs (hash validation)
- Security layer

**Státusz:** Week 3-ban blokkolt Frontend mock után, de előkészítheted

### Opció 3: Track A Tesztek (23 teszt) — NEM DEPLOY, SAFE ✅
**Scope:** 18 unit + 5 integration teszt Track A-hoz
**Becsült idő:** 1 nap

**Teszt területek:**
- TenantResolverTests (10 unit)
- EmailServiceTests (8 unit)
- QuoteRequestEndpointTests (5 integration)

**Státusz:** Biztonságos, nem deploy-ol production-be, elkészíthető június 30 előtt

## Javasolt sorrend (június 30-ig)

1. **OperatorPin Extension** (0.5 nap) — kis scope, Track C unblock
2. **Track A tesztek** (1 nap) — 23 teszt implementálása
3. **Partner KPI APIs** (MSG-035) — nagyobb scope, de független Q3-tól

## Következő lépés

**Válaszd ki melyik feladatot szeretnéd:**
- ✅ Opció 1: OperatorPin Extension
- ✅ Opció 2: Partner KPI + QR ASN APIs (MSG-035)
- ✅ Opció 3: Track A Tesztek

Ha választottál, válaszolj outbox-on keresztül, vagy kezdd el a feladatot.

## Összefoglaló

| Feladat | Státusz | Következő lépés |
|---|---|---|
| Track A Backend | ✅ DONE | ⏸️ HOLD június 30-ig (deploy) |
| Track A Tesztek | ⏳ PENDING | Megírható most is (23 teszt) |
| Infrastructure Phase 1 | ✅ DONE | Phase 2 vár Track A/B/C-re |
| Track B/C | ⏸️ HOLD | Várj június 30-ig! |
| OperatorPin Extension | 💡 JAVASOLT | Independent work, 0.5 nap |
| Partner KPI APIs (MSG-035) | 💡 JAVASOLT | Week 3-ban, de előkészíthető |

---

**Root jóváhagyás:** MSG-CONDUCTOR-022 (CONDITIONAL APPROVE)
**Checkpoint:** Június 30, 2026 — Doorstar Soft Launch GO/NO-GO
**Conductor:** Waiting for checkpoint, coordinating interim work
