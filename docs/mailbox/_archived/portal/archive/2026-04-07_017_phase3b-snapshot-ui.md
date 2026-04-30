---
id: MSG-P017
from: architect
to: portal
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3B"
ref: MSG-P016
---

# Phase 3B — Snapshot UI + ProofHash upload + VerifyChain admin panel

A Kernel Phase 3B (Escrow GA Foundation) és az Orchestrator BFF route-ok elkészülnek. A Portal a következő UI feladatokat kapja.

---

## Kontextus — Új BFF endpoint-ok

| BFF route | Leírás |
|-----------|--------|
| `GET /bff/snapshots/:aggregateId?at=` | Snapshot lekérdezés (időpont alapján) |
| `GET /bff/snapshots/:aggregateId/versions` | Snapshot verziók listája |
| `GET /bff/audit-events/verify-chain` | Chain integritás (Admin only) |
| `POST /bff/tasks/:taskId/proof` | Proof fájl feltöltés |

---

## 1. OpenAPI contract sync

```bash
npm run sync-types
```

Ellenőrizd az alábbi típusok megjelennek:
- `SnapshotDto` — tartalmazza: `snapshotHash`, `stateJson` (opaque JSON), `snapshotAt`, `version`
- `SnapshotVersionDto`
- `ChainVerificationDto` — **különösen fontos:** `wormStorageAvailable: boolean` mező

**Kritikus:** A `ChainVerificationDto`-ban `wormStorageAvailable: false` esetén a UI **ne jelenítsen meg hibát (500-as)** — ez egy tervezett állapot. Jelenítsd meg: `"WORM storage unavailable — verification partial"` warning sávval.

---

## 2. ProofHash upload — meglévő Proof UI frissítése

A Kernel mostantól `ProofHash`-t tárol `ProofUrl` helyett. A `ImplementationSummary`-n a `ProofUrl` nullable lesz (Phase 3B+: DROP COLUMN migration 0025).

### Proof upload komponens

A meglévő proof upload flow-t frissítsd:

```typescript
// hooks/useUploadProof.ts
export function useUploadProof(taskId: string) {
  return useMutation({
    mutationFn: async (file: File) => {
      const response = await fetch(`/bff/tasks/${taskId}/proof`, {
        method: 'POST',
        headers: { 'Content-Type': file.type },
        body: file,  // stream-ként, ne FormData-ként
        credentials: 'include',
      });
      if (!response.ok) {
        if (response.status === 415) throw new Error('Unsupported file type');
        throw new Error('Upload failed');
      }
      return response.json();
    },
  });
}
```

**Elfogadott fájltípusok** (MIME whitelist — csak ezek küldhetők):
```
image/jpeg, image/png, image/webp, image/gif
application/pdf
video/mp4, video/webm
```

`<input type="file">` accept attributum:
```html
accept="image/jpeg,image/png,image/webp,image/gif,application/pdf,video/mp4,video/webm"
```

### ProofHash megjelenítés

A régi "ProofUrl" link helyett (ahol létezik ProofHash):
```tsx
{summary.proofHash && (
  <div>
    <span>SHA-256: </span>
    <code className="font-mono text-xs">{summary.proofHash}</code>
  </div>
)}
{summary.proofUrl && !summary.proofHash && (
  <a href={summary.proofUrl}>Régi bizonyíték (URL)</a>
)}
```

---

## 3. Snapshot viewer — FlowEpic detail oldalon

A FlowEpic detail oldalán adj hozzá egy "Snapshot history" tab-ot vagy accordiont:

### useSnapshotVersions hook

```typescript
// hooks/useSnapshotVersions.ts
export function useSnapshotVersions(aggregateId: string) {
  return useQuery({
    queryKey: ['snapshots', aggregateId, 'versions'],
    queryFn: () => api.get(`/bff/snapshots/${aggregateId}/versions`),
    enabled: !!aggregateId,
  });
}
```

### useSnapshotAt hook

```typescript
// hooks/useSnapshotAt.ts
export function useSnapshotAt(aggregateId: string, at: string) {
  return useQuery({
    queryKey: ['snapshots', aggregateId, 'at', at],
    queryFn: () => api.get(`/bff/snapshots/${aggregateId}?at=${encodeURIComponent(at)}`),
    enabled: !!aggregateId && !!at,
  });
}
```

### SnapshotHistoryPanel komponens (stub)

```tsx
// components/SnapshotHistoryPanel.tsx
// Phase 3B: lista a snapshot verziókról (dátum + hash)
// Kattintásra: az adott pillanat StateJson-ja megjelenítve (read-only)
// StateJson opaque — ne próbáld értelmezni, csak JSON.stringify(json, null, 2) megjelenítés
```

---

## 4. VerifyChain admin panel

Az Audit Log oldalán (Admin only) adj hozzá egy "Chain Integrity" szekciót:

```tsx
// components/ChainVerificationPanel.tsx
// Trigger: "Verify Chain" gomb (Admin role-ra tesztelve)
// Response megjelenítés:
// - isValid: true → zöld badge "INTACT"
// - isValid: false → piros badge "BROKEN" + firstBrokenAt timestamp
// - wormStorageAvailable: false → sárga warning "WORM storage offline — partial check"
// - totalRecordsChecked: "X records checked"
```

`useVerifyChain` hook:
```typescript
// hooks/useVerifyChain.ts
export function useVerifyChain() {
  return useMutation({
    mutationFn: ({ from, to }: { from: string; to: string }) =>
      api.get(`/bff/audit-events/verify-chain?from=${from}&to=${to}`),
  });
}
```

A gomb csak akkor jelenik meg ha `useIsAdmin()` true (meglévő hook).

---

## 5. Security review

- [ ] `stateJson` a snapshot response-ban opaque — ne parsolja a komponens, csak megjelenítés
- [ ] `proofHash` **soha ne legyen link-ként megjelenítve** (hash !== URL)
- [ ] A fájl MIME type validáció a `<input accept>` attribútumban megvan (UX early validation)
- [ ] `wormStorageAvailable: false` → warning, nem error state
- [ ] VerifyChain panel csak Admin role esetén renderelődik (ne csak elrejtve legyen, hanem ne is rendelődjön a route-hoz)
- [ ] Snapshot `at` paraméter: date picker upper bound = `now()` (ne lehessen jövőbeli dátumot beállítani)

---

## 6. Tesztek

```bash
npm test -- --reporter=verbose
```

Elvárt: meglévő 256 teszt zöld.

Új tesztek:

`useUploadProof.test.ts`:
- Sikeres upload → proofHash megjelenik
- 415 response → "Unsupported file type" error

`ChainVerificationPanel.test.tsx`:
- `isValid: true` → "INTACT" badge
- `isValid: false` → "BROKEN" badge + `firstBrokenAt`
- `wormStorageAvailable: false` → warning sáv (nem error)
- Non-admin user → panel nem renderelődik

`SnapshotHistoryPanel.test.tsx`:
- Verziók listája megjelenik
- Kattintás → at paraméteres query trigger

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-P017`:
- `npm run sync-types` sikerült? + `ChainVerificationDto.wormStorageAvailable` megjelenik?
- Hook-ok implementálva: igen/nem + fájllistával
- Proof upload UI frissítve: igen/nem
- VerifyChain panel: igen/nem (stub vagy teljes)
- Security review pontok ✅ / ⚠️
- Teszt eredmény (pass/fail)
