---
id: MSG-O010
from: architect
to: orchestrator
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3B"
ref: MSG-O009
---

# Phase 3B — Új BFF route-ok + Proof upload proxy

A Kernel Phase 3B implementálja az Escrow GA Foundation-t (AggregateSnapshot, Outbox, ProofHash, VerifyChain). Az Orchestrator BFF rétegnek az alábbi feladatokat kell elvégeznie.

---

## Kontextus — Új Kernel endpoint-ok (Phase 3B)

| Method | Kernel route | Leírás | Auth |
|--------|-------------|--------|------|
| `GET`  | `/api/snapshots/{aggregateId}?at=` | Snapshot lekérdezés adott időpontban | JWT + TenantScope |
| `GET`  | `/api/snapshots/{aggregateId}/versions?page=&pageSize=` | Snapshot verziók listája | JWT + TenantScope |
| `GET`  | `/api/audit-events/verify-chain?from=&to=` | Hash chain integritás ellenőrzés | JWT + AdminOnly |
| `POST` | `/api/tasks/{taskId}/proof` | Proof fájl feltöltés (streaming) | JWT + TenantScope |

---

## 1. BFF proxy route-ok

### Snapshot routes

```typescript
// GET /bff/snapshots/:aggregateId
router.get('/bff/snapshots/:aggregateId',
  requireAuth,
  validateUuid('aggregateId'),
  (req, res) => kernelProxy.forward(req, res, 'GET',
    `/api/snapshots/${req.params.aggregateId}${buildQueryString(req.query)}`));

// GET /bff/snapshots/:aggregateId/versions
router.get('/bff/snapshots/:aggregateId/versions',
  requireAuth,
  validateUuid('aggregateId'),
  (req, res) => kernelProxy.forward(req, res, 'GET',
    `/api/snapshots/${req.params.aggregateId}/versions${buildQueryString(req.query)}`));
```

Query string passthrough: `?at=`, `?page=`, `?pageSize=`

### VerifyChain route (Admin only)

```typescript
// GET /bff/audit-events/verify-chain
router.get('/bff/audit-events/verify-chain',
  requireAuth,
  requireAdmin,   // 403 ha nem Admin role
  (req, res) => kernelProxy.forward(req, res, 'GET',
    `/api/audit-events/verify-chain${buildQueryString(req.query)}`));
```

`requireAdmin` middleware: JWT `role` claim === `'Admin'` → 403 ha hiányzik.

### Proof upload route (streaming)

```typescript
// POST /bff/tasks/:taskId/proof
// FONTOS: ne bufferelj — pass-through stream a Kernel felé
router.post('/bff/tasks/:taskId/proof',
  requireAuth,
  validateUuid('taskId'),
  (req, res) => kernelProxy.forwardStream(req, res, 'POST',
    `/api/tasks/${req.params.taskId}/proof`));
```

**Kritikus:** a Kernel streaming upload-ot vár (`Request.Body` direkt). Az Orchestrator **ne bufferelj** multipart/form-data-t — pipe a request stream-et közvetlenül tovább.

Ellenőrizd hogy az `express.json()` middleware **NEM fut le** ezen a route-on (másold a Phase 2 T-03 SSE proxy mintáját ahol az `express.json()` body parser ki volt zárva).

---

## 2. KernelClient bővítés

```typescript
// Snapshot methods
async getSnapshot(aggregateId: string, at: string): Promise<SnapshotDto>
async getSnapshotVersions(aggregateId: string, page: number, pageSize: number): Promise<PagedList<SnapshotVersionDto>>

// Audit methods
async verifyChain(from: string, to: string): Promise<ChainVerificationDto>

// Proof upload
async uploadProof(taskId: string, stream: NodeJS.ReadableStream, contentType: string): Promise<ProofReceiptDto>
```

`ChainVerificationDto`:
```typescript
interface ChainVerificationDto {
  isValid: boolean;
  firstBrokenAt: string | null;
  totalRecordsChecked: number;
  wormStorageAvailable: boolean;  // ha false: nem 500, hanem warning UI-n
  diagnosticMessage: string | null;
}
```

---

## 3. Content-Type validáció — proof upload

A Kernel MIME whitelist-et érvényesít (`image/*`, `application/pdf`, `video/*`), de az Orchestrator BFF oldalon is érdemes előszűrni:

```typescript
const ALLOWED_PROOF_MIME_TYPES = new Set([
  'image/jpeg', 'image/png', 'image/webp', 'image/gif',
  'application/pdf', 'video/mp4', 'video/webm'
]);

// Middleware a proof upload route-on:
if (!ALLOWED_PROOF_MIME_TYPES.has(req.headers['content-type']?.split(';')[0] ?? '')) {
  return res.status(415).json({ error: 'Unsupported media type' });
}
```

---

## 4. Security review

- [ ] UUID validáció: `aggregateId`, `taskId` params — UUID regex előtt nem kerül proxy path-ba
- [ ] `verifyChain` route: csak Admin role — `requireAdmin` middleware meghívva
- [ ] Proof upload: `express.json()` body parser nem fut le ezen a route-on
- [ ] `Authorization: Bearer` header változatlan a Kernel felé (JWT nem módosul)
- [ ] `X-SpaceOS-Brand` header forwarding — mint a többi BFF route

---

## 5. Tesztek

```bash
npm test
```

Elvárt: meglévő 114 teszt zöld. Új tesztek:

`snapshot.proxy.test.ts`:
- `GET /bff/snapshots/:id` → JWT forwarding, UUID validáció, query string passthrough
- `GET /bff/snapshots/:id/versions` → pagination params passthrough
- Nem-UUID path → 400

`verify-chain.proxy.test.ts`:
- Non-admin JWT → 403 (BFF szinten)
- Admin JWT → proxy a Kernel felé
- `wormStorageAvailable: false` response → BFF továbbítja (nem 500-ra konvertálja)

`proof-upload.proxy.test.ts`:
- Nem engedélyezett MIME → 415 (BFF szinten, Kernel előtt)
- Stream forwarding: express.json() nem buffereli

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-O010`:
- BFF route-ok implementálva: igen/nem + fájllistával
- KernelClient bővítve: igen/nem
- Proof upload stream proxy: bufferelt vagy stream?
- Security review pontok ✅ / ⚠️
- Teszt eredmény (pass/fail)
