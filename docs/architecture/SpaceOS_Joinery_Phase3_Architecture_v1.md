# SpaceOS — Joinery Phase 3 Architecture (Portal integráció)
## Batch PDF letöltés · Anyaglista export · Doorstar Portal gombok

> **Verzió:** v1.0 — 2026-04-24
> **Státusz:** DRAFT — Architect spec, Gábor approval szükséges
> **Scope:** Doorstar Portal + Orchestrator BFF bővítés (Joinery API változatlan)
> **Előfeltétel:** Joinery Phase 2 DEPLOYED (387 teszt), Doorstar Portal DEPLOYED (323 teszt)
> **Referencia:** JOINERY_CONTEXT.md, ORCH_CONTEXT.md, `spaceos-doorstar-portal/`

---

## 0. Architekturális döntések

| # | Kérdés | Döntés | Indoklás |
|---|--------|--------|----------|
| JP3-01 | Melyik portálon? | **Doorstar Portal** (`portal.joinerytech.hu`) | Ez az aktív gyártói portál; a Design Portal (`joinerytech.hu`) Turborepo monorepo — más életciklus |
| JP3-02 | Hol jelenjen meg? | **OrderDetailPage** — meglévő gombsor bővítés | A felhasználó már itt van amikor a rendelést nézi; nem kell külön oldal |
| JP3-03 | BFF proxy szükséges? | **Igen** — 4 új Orchestrator route | A Joinery API-t közvetlenül nem éri el a portál; BFF proxy pattern kell (Doorstar minta) |
| JP3-04 | Melyik role? | **ManufacturerOnly** (Joinery API enforced) | A BFF JWT-t forward-ol; a Joinery endpoint ellenőrzi a `ManufacturerOnly` auth policyt + tenant_id RLS-t |
| JP3-05 | Batch állapot polling | **Polling 2s interval, max 60 próba (2 perc)** | SSE overkill (batch generálás tipikusan <10s); Orchestrator SSE infrastruktúra nincs Joinery route-okra |
| JP3-06 | MinIO presigned URL | **BFF proxy redirect** — a Joinery `/batch/{id}/download` már presigned URL-re redirect-ol (302) | A frontend a BFF-en keresztül hívja → a Joinery 302-t ad → a böngésző követi a presigned URL-t → közvetlen MinIO letöltés |
| JP3-07 | Anyaglista UX | **Szinkron** — a QuestPDF generálás <1s, nem kell FSM/polling | Az anyaglista endpoint direkt PDF-et ad vissza (nem MinIO) |

---

## 1. Melyik portálon? (Q1)

**Doorstar Portal** (`portal.joinerytech.hu`, repo: `spaceos-doorstar-portal/`).

**Indoklás:**
- A Joinery rendelések (DoorOrder) a Doorstar portálon jelennek meg
- A Design Portal (`joinerytech.hu`) Turborepo — a Joinery route-ok ott is megjelennek, de a Doorstar portal az aktív gyártói felület
- A Design Portal Joinery route-ok egy későbbi fázisban (ha a Turborepo `joinery-ui` csomag kész) kerülnek bővítésre — ez NEM Phase 3 scope

---

## 2. UI wireframe — OrderDetailPage bővítés (Q2)

### 2.1 Jelenlegi állapot

```
┌─────────────────────────────────────────────────────────┐
│ Rendelés: ORD-2026-0042                                 │
│ Státusz: Calculated ✅                                   │
│                                                         │
│ [📋 Beküldés]  [🔄 Kalkulálás]  [✂️ Cutting lista]     │
│                                                         │
│ Tételek:                                                │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ 1. Beltéri ajtó 80x200 standard    2 db            │ │
│ │ 2. Beltéri ajtó 90x210 prémium     1 db            │ │
│ └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Phase 3 bővítés

```
┌─────────────────────────────────────────────────────────┐
│ Rendelés: ORD-2026-0042                                 │
│ Státusz: Calculated ✅                                   │
│                                                         │
│ [📋 Beküldés]  [🔄 Kalkulálás]  [✂️ Cutting lista]     │
│                                                         │
│ ── Dokumentumok ──────────────────────────────────────── │
│                                                         │
│ [📄 Gyártási lap PDF]  [📦 Anyaglista PDF]              │
│                                                         │
│  ↓ (Gyártási lap kattintás után)                        │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ⏳ Gyártási lap generálása...  (2/60)               │ │
│ │ ████████░░░░░░░░░░░░░                               │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│  ↓ (Kész)                                               │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ✅ Gyártási lap kész!                                │ │
│ │ [📥 Letöltés (ZIP)]   [🔄 Újragenerálás]            │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ Tételek:                                                │
│ ...                                                     │
└─────────────────────────────────────────────────────────┘
```

### 2.3 Gomb megjelenési feltételek

| Gomb | Mikor látható | Mikor disabled |
|------|---------------|----------------|
| "Gyártási lap PDF" | `order.status === 'Calculated'` | Batch Pending/Generating (polling fut) |
| "Anyaglista PDF" | `order.status === 'Calculated'` | Generálás folyamatban |
| "Letöltés (ZIP)" | Batch `status === 'Ready'` | — |
| "Újragenerálás" | Batch `status === 'Ready' \|\| 'Failed'` | — |

### 2.4 Állapot diagram — Gyártási lap UX

```
[Gyártási lap PDF] kattintás
  → POST /bff/joinery/gyartasilap/batch
  → batchId kapás
  → polling: GET /bff/joinery/gyartasilap/batch/{batchId}/status (2s)
  → status === 'Generating' → progress bar
  → status === 'Ready' → [Letöltés] gomb megjelenés
  → status === 'Failed' → hibaüzenet + [Újrapróbálás] gomb
  → [Letöltés] kattintás → GET /bff/joinery/gyartasilap/batch/{batchId}/download
  → 302 → presigned MinIO URL → böngésző letöltés
```

### 2.5 Állapot diagram — Anyaglista UX

```
[Anyaglista PDF] kattintás
  → POST /bff/joinery/anyaglista/generate  (body: { orderId })
  → szinkron válasz: PDF binary
  → böngésző letöltés (blob URL)
```

---

## 3. BFF route bővítés (Q3)

### 3.1 Új Orchestrator route-ok

**Fájl:** `spaceos-orchestrator/src/routes/joinery.route.ts` (meglévő fájl bővítése)

| BFF route | HTTP | Proxy target (Joinery :5002) | Auth |
|-----------|------|------------------------------|------|
| `/bff/joinery/gyartasilap/batch` | POST | `/api/gyartasilap/batch` | `requireAuth` |
| `/bff/joinery/gyartasilap/batch/:batchId/status` | GET | `/api/gyartasilap/batch/:batchId/status` | `requireAuth` |
| `/bff/joinery/gyartasilap/batch/:batchId/download` | GET | `/api/gyartasilap/batch/:batchId/download` | `requireAuth` |
| `/bff/joinery/anyaglista/generate` | POST | `/api/anyaglista/generate` | `requireAuth` |

### 3.2 Proxy implementáció

A meglévő Joinery proxy minta alapján (`joinery.route.ts`):

```typescript
// Gyártási lap batch
router.post('/batch', requireAuth, proxyLimiter, async (req, res) => {
  await proxyToJoinery(req, res, 'POST', '/api/gyartasilap/batch');
});

router.get('/batch/:batchId/status', requireAuth, async (req, res) => {
  await proxyToJoinery(req, res, 'GET', `/api/gyartasilap/batch/${req.params.batchId}/status`);
});

router.get('/batch/:batchId/download', requireAuth, async (req, res) => {
  // 302 redirect átadás — a presigned URL-re redirect-ol a Joinery
  await proxyToJoinery(req, res, 'GET', `/api/gyartasilap/batch/${req.params.batchId}/download`);
});

// Anyaglista
router.post('/generate', requireAuth, proxyLimiter, async (req, res) => {
  await proxyToJoinery(req, res, 'POST', '/api/anyaglista/generate');
});
```

### 3.3 Route mount

```typescript
// src/index.ts — hozzáadni:
import { gyartasilapRouter } from './routes/gyartasilap.route';
import { anyaglistaRouter } from './routes/anyaglista.route';

app.use('/bff/joinery/gyartasilap', gyartasilapRouter);
app.use('/bff/joinery/anyaglista', anyaglistaRouter);
```

**Alternatíva:** Egy fájlba (`joinery.route.ts`) a meglévő order route-ok mellé — a fájl méretétől függ. Ha a jelenlegi `joinery.route.ts` rövid → egy fájl. Ha hosszú → külön route fájlok.

---

## 4. Auth (Q4)

### 4.1 Auth chain

```
Portal → [Bearer JWT] → Orchestrator BFF → [requireAuth middleware] → JWT validálás
  → [JWT forwarding] → Joinery API → [ManufacturerOnly policy] → tenant_id RLS
```

**Nincs új auth munka.** A chain már működik:
1. **Portal:** `apiClient` interceptor csatolja a Bearer tokent (Keycloak OIDC, meglévő)
2. **BFF:** `requireAuth` middleware validálja a JWT-t (JWKS RS256, meglévő)
3. **Joinery:** `ManufacturerOnly` auth policy + `TenantId` ellenőrzés (Phase 2, meglévő)

### 4.2 Keycloak role requirement

A felhasználónak a `ManufacturerOnly` policyt kell teljesítenie:
- `spaceos_tenants` claim-ben a `tenant_type` legyen `"Manufacturer"`
- Doorstar userek (test-admin, designer) már rendelkeznek ezzel

---

## 5. UX flow — batch generálás (Q5)

### 5.1 Polling stratégia

```typescript
// hooks/useBatchGeneration.ts
const POLL_INTERVAL_MS = 2000;     // 2 másodperc
const MAX_POLL_COUNT = 60;          // 2 perc maximum

function useBatchGeneration(orderId: string) {
  const [batchId, setBatchId] = useState<string | null>(null);
  const [status, setStatus] = useState<BatchStatus>('idle');

  const generateMutation = useMutation({
    mutationFn: () => gyartasilapApi.createBatch(orderId),
    onSuccess: (data) => {
      setBatchId(data.batchId);
      setStatus('polling');
    },
  });

  // Polling with react-query
  const statusQuery = useQuery({
    queryKey: ['batch-status', batchId],
    queryFn: () => gyartasilapApi.getBatchStatus(batchId!),
    enabled: !!batchId && status === 'polling',
    refetchInterval: (query) => {
      const data = query.state.data;
      if (!data) return POLL_INTERVAL_MS;
      if (data.status === 'Ready' || data.status === 'Failed') return false;
      return POLL_INTERVAL_MS;
    },
  });

  // ...
}
```

### 5.2 Miért NEM SSE

| Szempont | Polling | SSE |
|----------|---------|-----|
| Komplexitás | Alacsony (React Query `refetchInterval`) | Magas (új SSE infra kell az Orchestrator-ban a Joinery route-okra) |
| Batch idő | Tipikusan <10s | Overkill <10s-re |
| Orchestrator impact | 0 (proxy, stateless) | Új SSE proxy + connection management |
| Skálázhatóság | 30 request/perc/batch (elfogadható) | Jobb, de irreleváns 1-2 concurrent batch-nél |

---

## 6. Presigned URL stratégia (Q6)

### 6.1 Jelenlegi flow (Joinery Phase 2)

```
GET /api/gyartasilap/batch/{batchId}/download
  → Joinery: MinIO presigned URL generálás (1 óra expiry)
  → 302 Redirect → https://minio.internal:9000/...?X-Amz-Signature=...
```

### 6.2 Phase 3 flow (BFF-en keresztül)

```
Portal: GET /bff/joinery/gyartasilap/batch/{batchId}/download
  → Orchestrator BFF: proxy → Joinery :5002
  → Joinery: presigned URL generálás → 302 Location header
  → Orchestrator: 302 visszaadás a Portal-nak
  → Böngésző: követi a 302-t → közvetlen MinIO letöltés

FONTOS: Az Orchestrator proxy-nak NEM kell követnie a redirect-et!
A `followRedirects: false` beállítás szükséges a proxy-ban.
```

### 6.3 MinIO hálózati elérhetőség

A MinIO `127.0.0.1:9000`-on hallgat. A presigned URL `localhost:9000`-re mutat — ez a böngészőből NEM érhető el!

**Megoldás opciók:**

| Opció | Leírás | Ajánlás |
|-------|--------|---------|
| **A) Nginx proxy** | `/minio/` location → `127.0.0.1:9000` | ✅ Egyszerű, már van nginx pattern |
| B) BFF stream | Orchestrator letölti a fájlt és stream-eli a portálnak | Lassabb, memóriaigényes |
| C) MinIO publikus bind | MinIO-t publikusra kötni | 🔴 Biztonsági kockázat |

**Javasolt: Opció A — Nginx reverse proxy a MinIO-hoz**

```nginx
# /etc/nginx/sites-available/spaceos — HOZZÁADNI:
location /minio/ {
    # Csak presigned URL-ek — nincs auth header, a signature az URL-ben van
    proxy_pass http://127.0.0.1:9000/;
    proxy_set_header Host $host;
    
    # Presigned URL-ek csak GET (letöltés)
    limit_except GET {
        deny all;
    }
}
```

A Joinery MinIO presigned URL generálásban a `Host` paramétert `joinerytech.hu/minio` -ra kell állítani:

```csharp
// GyartasilapMinioStorage.cs — presigned URL generálás
var presignedUrl = await _minioClient.PresignedGetObjectAsync(
    new PresignedGetObjectArgs()
        .WithBucket(bucket)
        .WithObject(objectKey)
        .WithExpiry(3600)  // 1 óra
);
// A presigned URL-ben a host-ot át kell írni:
// localhost:9000 → joinerytech.hu/minio
```

**Kockázat:** Ez a MinIO `endpoint` konfigurációt érinti a Joinery env-ben. Ellenőrizni kell, hogy a MinIO client-et hogyan inicializálja a Joinery — és a presigned URL domain-jét a publikus domain-re kell állítani.

---

## 7. Fázisolás (Q7)

### Phase 3 MVP (launch-hoz szükséges)

| # | Feature | Modul | Effort |
|---|---------|-------|--------|
| 1 | 4 Orchestrator BFF proxy route | ORCH | 0.5 nap |
| 2 | Nginx MinIO proxy location | INFRA | 0.25 nap |
| 3 | Joinery MinIO presigned URL domain fix | JOINERY | 0.25 nap |
| 4 | Portal: "Gyártási lap PDF" gomb + batch polling UX | PORTAL | 1.5 nap |
| 5 | Portal: "Anyaglista PDF" gomb + szinkron letöltés | PORTAL | 0.5 nap |
| 6 | Portal: unit tesztek (gomb feltételek, polling, error) | PORTAL | 1.0 nap |
| 7 | E2E: batch happy path (create → poll → download) | PORTAL/E2E | 0.5 nap |
| **Phase 3 MVP total** | | | **4.5 nap** |

### Phase 3.5 (post-launch)

| # | Feature | Effort | Trigger |
|---|---------|--------|---------|
| 8 | Batch history (korábbi generálások listája) | 1.0 nap | User feedback |
| 9 | "Mindent letöltés" gomb (gyártási lap + anyaglista + cutting list ZIP) | 1.5 nap | Gyártói feedback |
| 10 | PDF preview (in-browser PDF.js viewer, nem letöltés) | 1.0 nap | UX javítás |
| 11 | Design Portal bővítés (Turborepo joinery-ui) | 2.0 nap | Turborepo kész |
| **Phase 3.5 total** | | **~5.5 nap** | |

---

## 8. Effort összesítő (Q8)

### Phase 3 MVP — 4.5 nap

| Track | Nap | Feladat |
|-------|-----|---------|
| ORCH | 0.5 | 4 BFF proxy route (gyartasilap.route.ts + anyaglista.route.ts) + tesztek |
| INFRA | 0.25 | Nginx MinIO proxy + certbot |
| JOINERY | 0.25 | MinIO presigned URL host config fix |
| PORTAL | 1.5 | Gyártási lap gomb + batch polling hook + progress UI + error handling |
| PORTAL | 0.5 | Anyaglista gomb + blob download |
| PORTAL | 1.0 | Unit tesztek (~15: gomb feltételek, polling states, download) |
| E2E | 0.5 | E2E happy path: create batch → poll → download ZIP |

### Végrehajtási sorrend

```
JOINERY (MinIO host fix)
  → INFRA (Nginx MinIO proxy)
  → ORCH (BFF route-ok)
  → PORTAL (gombok + UX)
  → E2E (happy path)
```

A Joinery + INFRA párhuzamosan futhat (nem függenek egymástól). Az ORCH a Joinery DONE után indulhat. A Portal az ORCH DONE után.

---

## 9. Komponens struktúra (Portal bővítés)

### 9.1 Új fájlok

```
spaceos-doorstar-portal/src/
├── api/
│   ├── gyartasilapApi.ts           # NEW — batch create, status, download
│   └── anyaglistaApi.ts            # NEW — generate, download
├── hooks/
│   ├── useBatchGeneration.ts       # NEW — polling hook
│   └── useAnyaglistaExport.ts      # NEW — szinkron letöltés hook
├── components/
│   ├── BatchGenerationPanel.tsx    # NEW — progress bar, status, download gomb
│   └── DocumentsSection.tsx        # NEW — "Dokumentumok" section wrapper
```

### 9.2 Módosított fájlok

```
spaceos-doorstar-portal/src/
├── pages/
│   └── OrderDetailPage.tsx         # MODIFIED — DocumentsSection hozzáadás
```

### 9.3 API client

```typescript
// api/gyartasilapApi.ts
export const gyartasilapApi = {
  createBatch: (orderId: string) =>
    apiClient.post<{ batchId: string }>('/bff/joinery/gyartasilap/batch', {
      orderIds: [orderId],   // v1: egy rendelés per batch
    }),

  getBatchStatus: (batchId: string) =>
    apiClient.get<BatchStatusResponse>(`/bff/joinery/gyartasilap/batch/${batchId}/status`),

  downloadBatch: (batchId: string) => {
    // 302 redirect → presigned MinIO URL → böngésző letöltés
    window.location.href = `/bff/joinery/gyartasilap/batch/${batchId}/download`;
  },
};

// api/anyaglistaApi.ts
export const anyaglistaApi = {
  generate: async (orderId: string) => {
    const response = await apiClient.post('/bff/joinery/anyaglista/generate',
      { orderId },
      { responseType: 'blob' },
    );
    // Blob letöltés
    const url = URL.createObjectURL(response.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = `anyaglista-${orderId}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  },
};
```

---

## 10. Tesztelési terv

### Unit tesztek (Portal, ~15)

| Komponens | Teszteset | db |
|-----------|-----------|-----|
| `BatchGenerationPanel` | idle → polling → ready → download, failed → retry, max poll timeout | 5 |
| `DocumentsSection` | Gomb megjelenési feltételek (Calculated required) | 3 |
| `useBatchGeneration` | Mutation + polling lifecycle, error handling | 4 |
| `useAnyaglistaExport` | Blob download, error | 2 |
| API functions | Request format, headers | 1 |

### BFF tesztek (Orchestrator, ~4)

| Teszteset | db |
|-----------|-----|
| Proxy route hívás + JWT forwarding | 2 |
| 302 redirect átadás (batch download) | 1 |
| Auth nélküli request → 401 | 1 |

### E2E teszt (~2)

| Szcenárió | db |
|-----------|-----|
| Happy path: batch create → poll → Ready → download | 1 |
| Anyaglista generálás → PDF letöltés | 1 |

**Total: ~21 új teszt**

---

## 11. Nyitott kérdések

| # | Kérdés | Hatás |
|---|--------|-------|
| 1 | A Joinery MinIO client endpoint konfigurációja — hogyan van az `appsettings.json`-ben? | A presigned URL domain-jét kell módosítani (`localhost:9000` → `joinerytech.hu/minio`) |
| 2 | A `POST /api/gyartasilap/batch` body-ja — `orderIds` array, vagy egyetlen `orderId`? | Determinálja a frontend API hívást |
| 3 | A Design Portal (`joinerytech.hu`) Joinery route-ok prioritása — Phase 3 vagy 3.5? | Turborepo `joinery-ui` csomag állapotától függ |
