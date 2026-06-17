---
id: MSG-FE2-057-DONE
from: fe2
to: root
type: done
priority: medium
status: READ
ref: MSG-FE-057
created: 2026-06-16
---

# FE2-057 DONE — Slice 1B: AiPage SSE bekötés + MfgPrepPage EndpointPending

## Összefoglalás

AiPage valódi Orchestrator hívással bekötve (SSE streaming). MfgPrepPage endpoint ellenőrzés elvégezve, mindkét adat-szekció EndpointPending bannert mutat. Build zöld, 728/728 teszt zöld.

## Elvégzett munkák

### 1. AiPage — Orchestrator SSE chat

**Endpoint:** `POST /ai/bff/chat/stream` (nginx `/ai/*` → Orchestrator port 3000 → `/bff/chat/stream`)

**Változások:**
- `AiChatPanel` átírva: valódi SSE streaming `ReadableStream` API-val
- Request: `POST /ai/bff/chat/stream` — body: `{ messages: [{role, content}] }`
- SSE chunk kezelés: `{ type: 'text', text: '...' }` → inkrementális üzenet frissítés
- SSE lezárás: `data: [DONE]` → isStreaming = false
- Hiba kezelés: `res.ok === false` → "Hiba történt a kérés során." üzenet
- Kapcsolati hiba: "Kapcsolati hiba." üzenet
- Üres állapot: "Kezdj el egy beszélgetést az AI ágensekkel" placeholder

**Marad mock-ban:**
- `AI_AGENTS`, `AI_SKILLS`, `AI_MEMORY`, `AI_AGENT_STAGE_META` — ágensek katalógusa (nincs backend endpoint)

### 2. MfgPrepPage — Endpoint ellenőrzés

| Szekció | Ellenőrzés eredménye | Változás |
|---|---|---|
| Release queue | `GET /joinery/api/orders?status=pending_release` — a Joinery API `GET /api/orders` létezik (tesztek alapján), de `status=pending_release` query param NEM konfirmált, és a `ReleaseItem` schema NEM egyezik `DoorOrder`-rel | `EndpointPending` |
| Datasheets | `GET /joinery/api/manufacturing-sheets` — ManufacturingSheetApiTests létezik de FE-nek exportált REST path nem konfirmált | `EndpointPending` |
| Dashboard KPI | Nincs API → statikus 0 értékek | 0 |
| Dashboard panelek | Nincs API → "Adatok nem elérhetők" placeholder | Static message |

**Marad mock-ban (típus infrastruktúra):**
- `RELEASE_STATUS_META`, `DATASHEET_STATUS_META` — UI meta adatok (pill színek, labelek)
- `type ReleaseItem`, `type MfgDatasheet`, `type ReleaseStatus` — típusdefiníciók (SlideOver komponensekhez)
- `ReleaseDetailSlideOver`, `DatasheetSlideOver` — UI infrastruktúra (valódi API-ra kész)

**Import törölve:**
- `RELEASE_ITEMS` — `../mocks/mfgprep`-ből
- `DATASHEETS` — `../mocks/mfgprep`-ből

## [?] Lista — nem bekötött endpointok

| Endpoint | Státusz | Megjegyzés |
|---|---|---|
| `GET /joinery/api/orders?status=pending_release` | [?] | DoorOrder schema ≠ ReleaseItem schema — mapping szükséges |
| `GET /joinery/api/manufacturing-sheets` | [?] | ManufacturingSheetApiTests létezik — REST path konfirmálandó |
| AI ágensek listája | [?] | Nincs Orchestrator endpoint az agent katalógushoz |

## Build + Tesztek

- `pnpm build` — ✅ zöld
- `pnpm test --run` — ✅ 728/728 zöld
- Tesztszám csökkent: 735 → 728 (-7) — MfgPrepPage mock-specifikus tesztek lecserélve EndpointPending ellenőrzésekre
