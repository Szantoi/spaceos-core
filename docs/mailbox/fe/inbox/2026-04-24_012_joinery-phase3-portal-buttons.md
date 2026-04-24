---
id: MSG-FE-012
from: root
to: fe
type: task
priority: high
status: READ
ref: SpaceOS_Joinery_Phase3_Architecture_v1.md
created: 2026-04-24
---

# FE-012 — Joinery Phase 3: Batch PDF + Anyaglista gombok (Doorstar Portal)

> **Tervdok:** `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md` — KÖTELEZŐ olvasmány! Section 2, 5, 9
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** ORCH-083 ✅ (4 BFF route LIVE) · JOINERY-054 ✅ · INFRA-055 ✅
> **Repo:** `spaceos-doorstar-portal`
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope

A Doorstar Portal OrderDetailPage-re 2 új gomb + állapotkezelés:

### 1. "Gyártásilap generálás" gomb

**UX flow (tervdok Section 5):**
1. User klikk → `POST /bff/joinery/gyartasilap/batch` body: `{ "orderIds": ["<orderId>"] }`
2. Response: `{ "id": "<batchId>", "status": "Pending" }`
3. **Polling:** `GET /bff/joinery/gyartasilap/batch/<batchId>` 2s interval
4. `status: "Generating"` → spinner + "Generálás folyamatban..."
5. `status: "Ready"` → "Letöltés" gomb megjelenik
6. User klikk → `GET /bff/joinery/gyartasilap/batch/<batchId>/download` → böngésző letölti a ZIP-et
7. `status: "Failed"` → hibaüzenet

**Komponens:** `BatchPdfButton.tsx` + `useBatchPdf` hook

### 2. "Anyaglista letöltés" gomb

**UX flow (tervdok Section 7):**
1. User klikk → `POST /bff/joinery/anyaglista` body: `{ "orderIds": ["<orderId>"] }`
2. Response: PDF binary → blob download (szinkron, <1s)

**Komponens:** `AnyaglistaButton.tsx` + `useAnyaglista` hook

### 3. OrderDetailPage bővítés

A tervdok Section 2 wireframe szerint — "Dokumentumok" szekció a gombsor alatt:

```
┌─────────────────────────────────────────┐
│  [Vissza]  Rendelés: ORD-2024-0042      │
│                                         │
│  ... meglévő rendelés adatok ...        │
│                                         │
│  📄 Dokumentumok                        │
│  ┌──────────────┐  ┌──────────────┐     │
│  │ 📋 Gyártási- │  │ 📦 Anyag-    │     │
│  │    lap PDF   │  │    lista PDF │     │
│  │ [Generálás]  │  │ [Letöltés]   │     │
│  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────┘
```

## API client bővítés

```typescript
// src/api/joineryApi.ts (vagy hooks/)
export const joineryApi = {
  createBatch: (orderIds: string[]) =>
    apiClient.post('/bff/joinery/gyartasilap/batch', { orderIds }),
  
  getBatchStatus: (batchId: string) =>
    apiClient.get(`/bff/joinery/gyartasilap/batch/${batchId}`),
  
  downloadBatch: (batchId: string) =>
    // window.open vagy anchor click — a 302 redirect a böngésző követi
    window.open(`/bff/joinery/gyartasilap/batch/${batchId}/download`),
  
  downloadAnyaglista: (orderIds: string[]) =>
    apiClient.post('/bff/joinery/anyaglista', { orderIds }, { responseType: 'blob' }),
};
```

## Tesztek (+8)

1. BatchPdfButton: render, klikk → API hívás
2. BatchPdfButton: polling → "Generating" spinner
3. BatchPdfButton: "Ready" → letöltés gomb
4. BatchPdfButton: "Failed" → hibaüzenet
5. AnyaglistaButton: render, klikk → blob download
6. OrderDetailPage: "Dokumentumok" szekció megjelenik
7. useBatchPdf hook: polling logic
8. useAnyaglista hook: blob download

## Definition of Done

- [ ] BatchPdfButton + useBatchPdf (polling, status, download)
- [ ] AnyaglistaButton + useAnyaglista (blob download)
- [ ] OrderDetailPage "Dokumentumok" szekció
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 314 pass (306 előző + min 8 új)
- [ ] Outbox DONE
