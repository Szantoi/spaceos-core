---
id: MSG-FRONTEND-022
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_0037_consensus.md
created: 2026-06-23
content_hash: 8563118db0387ea7b36b956697ac085bdb73984bfc15036dc20e25e7b4001ca9
---

# Partner KPI Widget + QR ASN Tracking (Phase 1-2)

## Összefoglalás

Implementáld a Partner KPI Widget-et és a QR ASN Tracking Phase 1 (mock) + Phase 2 (production integration) feature-eket a JoineryTech portálon.

**Becslés:** 7 nap (2 nap KPI + audit, 3 nap QR Phase 1, 2 nap QR Phase 2)
**Scope:** Week 1-2 KPI + QR mock/production, Week 3 backend API integration (Backend MSG-035-tel párhuzamos)

## Implementációs fázisok

### Week 1: KPI Widget + Adatminőség Audit (2 nap)

**Day 1: KPI komponensek**
```
src/components/partners/
├── PartnerKpiCard.jsx
│   ├── Props: partnerId, dateRange
│   ├── <Badge color="orange">⚠️ {missing_data_count} hiányzó adat</Badge>
│   ├── onClick(metric) → drill-down filtered OrdersTable
│   └── useEffect: calculateKPIs(orders, filters)
├── KpiCalculator.js  // Pure function
├── DateRangePicker.jsx  // Native <input type="date">
└── DataQualityAlert.jsx  // Audit eredmény megjelenítés
```

**Day 1-2: Adatminőség audit**
- `sim.partners[].orders` adatminőség audit
- Missing data analízis
- DataQualityAlert komponens elkészítése

**Day 2: Stakeholder demo**
- KPI dashboard + audit report
- QR tracking justification prezentálása

### Week 2: QR Phase 1 Mock (3 nap)

**Day 3: Mock infrastruktúra**
```javascript
src/data/mock-asn.js
└── generateASN(poId) → {asn, qrPayload, timestamp}

src/lib/offline-asn.js
├── validateFromCache(payload) → instant feedback
├── queueForSync(receiptData)
└── syncPendingReceipts() → background POST
```

**Day 4: ASN Generator komponens**
```
src/components/suppliers/
├── AsnGenerator.jsx
│   ├── qrcode.react library (7KB)
│   ├── Mock Phase: localStorage.setItem(`asn_${id}`, JSON.stringify(data))
│   └── Print button → PDF/shipping label
```

**Day 5: QR Scanner + Receipt Confirm**
```
src/components/suppliers/
├── QrScanner.jsx
│   ├── <input type="file" accept="image/*" capture="environment">
│   ├── jsQR library decode
│   ├── Offline-first: validateFromCache(payload) THEN backend
│   └── Auto-match → ReceiptConfirmDialog
│
├── ReceiptConfirmDialog.jsx
│   └── Quantity verification + notes
│
└── OfflineSyncQueue.jsx
    ├── LocalStorage: pendingReceipts[]
    ├── window.addEventListener('online', syncPending)
    └── Sync status indicator
```

**Output:** End-to-end mock flow, warehouse demo-ready

### Week 3: QR Phase 2 Production Integration (2 nap)

**Blokkolt:** Backend MSG-035 (Week 3 API-k) elkészülte után

**Day 6-7: Backend integráció**
- Frontend → backend API integráció
- Hash validation logic
- Sync mechanism finalizálás

**Backend API-k (Backend implementálja):**
```
POST /api/suppliers/asn/generate
├── Request: { poId, items[], expectedDate }
├── Response: { asn, qrPayload, printableUrl }

POST /api/inbound/receipt/scan
├── Request: { qrPayload, scannedBy, actualQuantity }
├── Response: { valid, po, hashVerified, nextAction }
```

## Adatmodell (Backend definiálja, Frontend használja)

```javascript
asn: {
  id: "ASN-20260622-001",
  poId: "PO-12345",
  supplierId: "SUP-01",
  expectedDate: "2026-06-25",
  qrPayload: "ASN|PO|DATE|sha256(ASN+PO+DATE+SERVER_SECRET)",
  status: "SHIPPED" | "RECEIVED" | "PENDING_SYNC",
  offlineScannedAt?: timestamp
}
```

## UX Flow

1. **Supplier desktop:** "Feladás" → ASN modal → QR generálás → Nyomtat
2. **Warehouse worker mobil:** "Szkennelés" tab → Camera/upload → Dekódol → Cache hit OR backend call → "PO-12345, 20 db?" → Confirm
3. **Offline:** Queue → Badge jelzi pending count → Auto-sync when online → Email notification

## State Management

Component local state (useState), native date input MVP-hez.

## Nyitott kérdések (válaszolj BLOCKED-ként ha blokkolnak)

**2. Adataudit scope:** Week 1 audit csak `sim.partners[].orders` vagy teljes Doorstar history? (Időbecslés +1 nap lehet)

**5. Mobil device support:** Céleszközök specifikációja? (Camera API compatibility, iOS/Android fallback stratégia)

**6. Offline sync konfliktus:** Mi történik ha két worker ugyanazt az ASN-t szkenneli offline? (Last-write-wins vs merge strategy) — válassz pragmatikus megoldást vagy kérdezz vissza

**7. Stakeholder demo timing:** Week 1 audit után egyeztetés szükséges QR scope finomításhoz, vagy auto-proceed Phase 1-re? — folytatd Phase 1-gyel ha nincs blokkoló

## Definition of Done

- [ ] Week 1: KPI Widget + DataQualityAlert működik, audit report kész
- [ ] Week 2: QR mock flow end-to-end (ASN generate → QR scan → receipt confirm → offline queue)
- [ ] Week 3: Backend API integráció, hash validation, production-ready
- [ ] Komponensek responsive (desktop + mobil)
- [ ] Offline sync működik (localStorage + sync queue)
- [ ] README frissítve (komponens dokumentáció)

## Kapcsolódó dokumentumok

- Consensus terv: `/opt/spaceos/docs/planning/queue/2026-06-22_0037_consensus.md`
- Backend API task: `terminals/backend/inbox/2026-06-23_035_*` (Week 3 blocker)
