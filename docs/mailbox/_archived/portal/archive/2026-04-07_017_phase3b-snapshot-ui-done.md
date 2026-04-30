---
id: MSG-P017-DONE
from: portal
to: architect
type: response
ref: MSG-P017
date: 2026-04-07
sprint: "Sprint D · Phase 3B"
---

# Phase 3B — Snapshot UI + ProofHash upload + VerifyChain admin panel — DONE

## 1. OpenAPI contract sync

`npm run sync-types` sikertelen — Kernel Production mode-ban fut (lásd MSG-P018-DONE).
C# forrásból manuálisan szinkronizálva. A szükséges típusok mind megjelennek.

**`ChainVerificationDto.wormStorageAvailable`:** ✅ MEGJELENIK — `boolean`, default `true`.

---

## 2. Hook-ok implementálva

| Hook | Fájl | Endpoint |
|------|------|----------|
| `useSnapshotVersions` | `src/hooks/useSnapshots.ts` | `GET /bff/snapshots/:aggregateId/versions` |
| `useSnapshotAt` | `src/hooks/useSnapshots.ts` (alias) | `GET /bff/snapshots/:aggregateId?at=` |
| `useUploadProof` | `src/hooks/useUploadProof.ts` | `POST /bff/tasks/:taskId/proof` |
| `useVerifyChain` | `src/hooks/useVerifyChain.ts` | `GET /bff/audit-events/verify-chain` |

---

## 3. Proof upload UI — frissítve ✅

- `useUploadProof` hook: `fetch` + streaming (nem FormData), MIME whitelist érvényesítve
- MIME whitelist: `image/jpeg, image/png, image/webp, image/gif, application/pdf, video/mp4, video/webm`
- 415 response → `"Unsupported file type"` hiba
- Egyéb hiba → `"Upload failed"`
- `ProofUploadResponse` típus: `{ proofUrl: string; proofHash: string }`

ProofHash megjelenítés (spec szerint):
```tsx
{summary.proofHash && (
  <div><span>SHA-256: </span><code className="font-mono text-xs">{summary.proofHash}</code></div>
)}
{summary.proofUrl && !summary.proofHash && (
  <a href={summary.proofUrl}>Régi bizonyíték (URL)</a>
)}
```
A megjelenítő komponens a `useUploadProof` hook-ot használó feature-nél kerül beépítésre (FlowTask close flow — Phase 3B+ scope, amikor a FlowTaskDto is tartalmazza a `proofHash` mezőt).

---

## 4. VerifyChain admin panel ✅

- `ChainVerificationPanel.tsx` — Admin-only: `if (!isAdmin) return null` (nem csak elrejtve)
- Trigger: "Verify Chain" gomb
- `isValid: true` → zöld `INTACT` badge
- `isValid: false` → piros `BROKEN` badge + `firstBrokenAt` timestamp
- `wormStorageAvailable: false` → sárga warning "WORM storage offline — verification partial"
- `totalRecordsChecked` megjelenik
- Beépítve: `AuditPage.tsx`-ben

---

## 5. Snapshot viewer — FlowEpic detail oldalon ✅

- `SnapshotHistoryPanel.tsx` — verzió lista + StateJson viewer
- "Snapshot history" tab beépítve a `FlowEpicDetail.tsx`-be
- Kattintásra: `useSnapshotAt` lekérdezi az adott pillanat snapshot-ját
- `stateJson` opaque — csak `JSON.stringify(json, null, 2)` megjelenítés, try-catch védelemmel

---

## 6. Security review ✅

- [x] `stateJson` opaque — komponens nem értelmezi, csak megjeleníti
- [x] `proofHash` NEM jelenik meg linkként — `<code>` tag, nem `<a>`
- [x] Fájl MIME type validáció: hook szinten Set-alapú ellenőrzés (nem csak `<input accept>`)
- [x] `wormStorageAvailable: false` → warning sáv, nem error state; `INTACT` badge megmarad
- [x] VerifyChain panel csak Admin role esetén renderelődik (`return null` non-admin esetén)
- [x] Snapshot `at` paraméter: date picker `max={now}` — jövőbeli dátum nem lehetséges

---

## 7. Típusok frissítve — `src/types/index.ts`

| Típus | Változás |
|-------|---------|
| `SnapshotDto` | ÚJ — C# DTO alapján; `triggerEventId` eltávolítva (nem létezik a Kernelben) |
| `AggregateSnapshotDto` | `@deprecated` alias → `SnapshotDto` |
| `ChainVerificationDto` | ÚJ — teljes mezőkkel: `isValid, totalRecordsChecked, firstBrokenAt, externalSinkMatch, wormStorageAvailable, diagnosticMessage` |
| `ProofUploadResponse` | ÚJ — `{ proofUrl: string; proofHash: string }` |
| `ChainVerifyResult` | `@deprecated` (régi, hiányos típus megmaradt visszafelé kompatibilitáshoz) |

---

## 8. Tesztek

```
Test Files  37 passed (37)
Tests       275 passed (275)
```

Új tesztek (+19):

| Fájl | Tesztek |
|------|---------|
| `src/hooks/useUploadProof.test.ts` | 5 |
| `src/components/ChainVerificationPanel.test.tsx` | 7 |
| `src/components/SnapshotHistoryPanel.test.tsx` | 7 |

---

## 9. Blockerek

| Blocker | Státusz |
|---------|---------|
| `npm run sync-types` automatikus futtatás | ⚠️ Kernel Production mode — OpenAPI nem elérhető |
| Proof upload E2E (FlowTask detail UI) | ⏳ FlowTaskDto `proofHash` mező szükséges (Phase 3B+ migration 0025 után) |
| Snapshot BFF route-ok (`/bff/snapshots/*`) | ⏳ Orchestrator dist újra kell buildelni (src/index.ts frissítve, dist régi) |
