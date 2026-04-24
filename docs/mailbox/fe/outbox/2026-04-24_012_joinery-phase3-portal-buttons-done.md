---
id: MSG-FE-012-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-012
created: 2026-04-24
---

# FE-012 DONE — Joinery Phase 3: Batch PDF + Anyaglista gombok

## Commit

`f153e46` — feat: FE-012 — Batch PDF + Anyaglista buttons on OrderDetailPage

## Elvégzett munka

### API réteg — `src/api/joineryApi.ts`

| Metódus | Endpoint | Leírás |
|---|---|---|
| `createBatch(orderIds)` | `POST /bff/joinery/gyartasilap/batch` | Batch generálás indítása |
| `getBatchStatus(batchId)` | `GET /bff/joinery/gyartasilap/batch/{id}` | Polling státusz lekérdezés |
| `downloadBatch(batchId)` | `GET /bff/joinery/gyartasilap/batch/{id}/download` | window.open → ZIP |
| `downloadAnyaglista(orderIds)` | `POST /bff/joinery/anyaglista` | Blob → anchor download → PDF |

### Hooks

| Hook | Fájl | Funkció |
|---|---|---|
| `useBatchPdf(orderId)` | `src/hooks/useBatchPdf.ts` | Polling (2s interval), Idle→Pending→Generating→Ready/Failed |
| `useAnyaglista(orderId)` | `src/hooks/useAnyaglista.ts` | Szinkron blob download, URL.createObjectURL + anchor click |

### Komponensek

| Komponens | Fájl | Testid-k |
|---|---|---|
| `BatchPdfButton` | `src/components/BatchPdfButton.tsx` | `batch-pdf-section`, `batch-pdf-generate-btn`, `batch-pdf-polling`, `batch-pdf-download-btn`, `batch-pdf-error`, `batch-pdf-retry-btn` |
| `AnyaglistaButton` | `src/components/AnyaglistaButton.tsx` | `anyaglista-section`, `anyaglista-download-btn`, `anyaglista-error` |

### OrderDetailPage — "Dokumentumok" szekció

`data-testid="documents-section"` — a meglévő tételek és status history között, 2 oszlopos grid layout.

### Tesztek (+9, összesen 99)

| # | Teszt | Fájl |
|---|---|---|
| 1 | BatchPdfButton: Generálás gomb Idle-ban | BatchPdfButton.test.tsx |
| 2 | BatchPdfButton: klikk → start hívás | BatchPdfButton.test.tsx |
| 3 | BatchPdfButton: spinner Generating-nél | BatchPdfButton.test.tsx |
| 4 | BatchPdfButton: Letöltés gomb Ready-nél | BatchPdfButton.test.tsx |
| 5 | BatchPdfButton: hibaüzenet Failed-nél | BatchPdfButton.test.tsx |
| 6 | AnyaglistaButton: Letöltés gomb renderelés | AnyaglistaButton.test.tsx |
| 7 | AnyaglistaButton: klikk → download hívás | AnyaglistaButton.test.tsx |
| 8 | AnyaglistaButton: hibaüzenet megjelenítés | AnyaglistaButton.test.tsx |
| 9 | OrderDetailPage: Dokumentumok szekció render | OrderDetailPage.test.tsx |

## DoD ellenőrzés

- [x] `BatchPdfButton` + `useBatchPdf` (polling, Idle/Pending/Generating/Ready/Failed, download)
- [x] `AnyaglistaButton` + `useAnyaglista` (blob download)
- [x] OrderDetailPage "Dokumentumok" szekció (documents-section testid)
- [x] `pnpm build` → 0 error
- [x] `pnpm test` → 99/99 pass (90 + 9 új)
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 hiba
- [x] git commit `f153e46` + push (main)
