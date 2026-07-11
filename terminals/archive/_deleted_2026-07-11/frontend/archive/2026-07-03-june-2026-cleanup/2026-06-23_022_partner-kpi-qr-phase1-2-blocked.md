---
id: MSG-FRONTEND-022-BLOCKED
from: frontend
to: conductor
type: blocked
status: READ
ref: MSG-FRONTEND-022
created: 2026-06-23
content_hash: bb21203a11e574c95174c8cfb48ca66bed864e3da61e22c742365bd6a0d68c1b
---

# Partner KPI Widget + QR ASN Tracking — Week 1-2 Complete, Week 3 BLOCKED

## Összefoglaló

**✅ Week 1 (KPI Widget + Data Quality Audit): KÉSZ**
**✅ Week 2 (QR Phase 1 Mock): KÉSZ**
**🔒 Week 3 (QR Phase 2 Production Integration): BLOCKED** - backend API MSG-035 elkészültére vár

---

## Week 1: Partner KPI Widget komponensek ✅

### Implementált komponensek

```
src/components/partners/
├── PartnerKpiCard.tsx           ✅ KPI dashboard with drill-down
├── DateRangePicker.tsx          ✅ Native date inputs + presets
├── DataQualityAlert.tsx         ✅ Missing data warning with details
└── KpiCalculator.ts             ✅ Pure function KPI logic
```

### KPI funkcionalitás

- **Total Orders** - összes rendelés száma
- **Total Revenue** - teljes bevétel (HUF)
- **Average Order Value** - átlagos rendelésérték (click → high-value orders)
- **On-Time Delivery** - határidőre teljesítés % (click → late deliveries)
- **Missing Data Alert** - hiányzó adatok száma (click → affected orders)

### Adatminőség audit

`KpiCalculator.ts` automatikusan detektálja:
- Hiányzó `deliveryDate` delivered orders-nél
- Hiányzó `items` array
- Hiányzó `quantity` vagy `price` az items-ben

---

## Week 2: QR ASN Tracking (Mock) ✅

### Mock infrastruktúra

```
src/data/mock-asn.ts             ✅ ASN generation, storage, validation
src/lib/offline-asn.ts           ✅ Offline-first sync queue
```

**Főbb funkciók:**
- `generateASN()` - ASN + QR payload generálás mock hash-sel
- `validateFromCache()` - instant QR validation localStorage-ból
- `queueForSync()` - offline receipt queue
- `syncPendingReceipts()` - background sync (Phase 1: mock, Phase 2: real API)
- `setupAutoSync()` - window.online event listener

### QR komponensek

```
src/components/suppliers/
├── AsnGenerator.tsx             ✅ QR generálás qrcode.react-tel
├── QrScanner.tsx                ✅ jsQR decode, offline-first validation
├── ReceiptConfirmDialog.tsx     ✅ Quantity verification + notes
└── OfflineSyncQueue.tsx         ✅ Pending receipts badge + manual sync
```

### UX Flow (mock)

1. **Supplier desktop:**
   - `AsnGenerator` → Fill expected date → Generate → QR megjelenik → Print

2. **Warehouse mobile:**
   - `QrScanner` → Upload image / camera → jsQR decode → `validateFromCache()` → `ReceiptConfirmDialog`

3. **Offline mode:**
   - `queueForSync()` → localStorage
   - `OfflineSyncQueue` badge jelzi pending count
   - Auto-sync when online OR manual "Sync Now" button

---

## Week 3: Production Integration — 🔒 BLOCKED

### Blokkoló: Backend API MSG-035

**Szükséges API-k (Backend implementálandó):**

```
POST /api/suppliers/asn/generate
├── Request: { poId, items[], expectedDate }
├── Response: { asn, qrPayload, printableUrl }

POST /api/inbound/receipt/scan
├── Request: { qrPayload, scannedBy, actualQuantity }
├── Response: { valid, po, hashVerified, nextAction }
```

### Frontend módosítások Week 3-ra

1. **Replace mock hash with server-side SHA-256**
   - `generateASN()` → `/api/suppliers/asn/generate` hívás
   - Server returns `qrPayload` with real hash

2. **Replace mock sync with real API**
   - `syncPendingReceipts()` → `POST /api/inbound/receipt/scan`
   - Hash validation backend oldalon

3. **Error handling**
   - Network errors
   - Invalid hash response
   - Duplicate ASN scan conflict resolution

---

## Nyitott kérdések (válaszolva)

| Kérdés | Válasz |
|---|---|
| **2. Adataudit scope** | Csak `sim.partners[].orders` audit implementálva (KpiCalculator) |
| **5. Mobil device support** | `<input capture="environment">` + fallback image upload |
| **6. Offline sync konfliktus** | Last-write-wins (pragmatic) - server timestamp decides |
| **7. Stakeholder demo timing** | Week 1-2 demo-ready, QR Phase 1 mock flow működik |

---

## Deployment Status

**Week 1-2: ✅ DEMO READY**
- KPI Widget működik mock adatokkal
- QR mock flow end-to-end working (offline-first)
- Stakeholder demo végezhető

**Week 3: 🔒 BLOCKED**
- Backend MSG-035 API implementation szükséges
- Frontend integration ~2 nap work ha backend kész

---

## Fájlok létrehozva

### Week 1: KPI (5 fájl)
```
src/components/partners/
├── PartnerKpiCard.tsx           (4.8 KB)
├── DateRangePicker.tsx          (3.2 KB)
├── DataQualityAlert.tsx         (2.9 KB)
├── KpiCalculator.ts             (4.1 KB)
└── index.ts                     (exports)
```

### Week 2: QR Mock (7 fájl)
```
src/data/mock-asn.ts             (4.5 KB)
src/lib/offline-asn.ts           (5.2 KB)
src/components/suppliers/
├── AsnGenerator.tsx             (5.1 KB)
├── QrScanner.tsx                (4.3 KB)
├── ReceiptConfirmDialog.tsx     (4.7 KB)
├── OfflineSyncQueue.tsx         (3.6 KB)
└── index.ts                     (exports)
```

### Packages telepítve
- `qrcode.react@^4.1.0` (7 KB)
- `jsqr@^1.4.0` (decode library)

---

## Definition of Done

| Kritérium | Week 1 | Week 2 | Week 3 |
|---|---|---|---|
| KPI Widget + DataQualityAlert működik | ✅ | - | - |
| QR mock flow end-to-end | - | ✅ | - |
| Backend API integráció | - | - | 🔒 BLOCKED |
| Komponensek responsive | ✅ | ✅ | - |
| Offline sync működik | - | ✅ (mock) | 🔒 (production) |
| README frissítve | ❌ | ❌ | Pending |

---

## Következő lépések

1. **Conductor:** Backend MSG-035 státuszának ellenőrzése (API readiness)
2. **Ha backend kész:** Frontend Week 3 indítása (~2 nap)
3. **README dokumentáció** írása (komponens usage examples)
4. **Stakeholder demo** scheduling Week 1-2 mock funkcionalitás bemutatására

---

## Kockázatok

- **Backend delay:** Ha MSG-035 csúszik, Week 3 integráció csúszik
- **API contract mismatch:** Frontend és backend API spec egyeztetése szükséges
- **Hash algorithm:** Server-side SHA-256 implementation kritikus

## Recommended next action

**Conductor:** Ellenőrizd a backend MSG-035 státuszát. Ha az API-k készen vannak vagy hamarosan elkészülnek, indítsd el a frontend Week 3 tasket (~2 nap).
