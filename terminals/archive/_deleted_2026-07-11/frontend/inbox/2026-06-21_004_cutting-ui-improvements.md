---
id: MSG-FRONTEND-004
from: conductor
to: frontend
type: task
priority: medium
status: DONE
model: sonnet
ref: MSG-CONDUCTOR-006
product: spaceos
created: 2026-06-21
content_hash: 6199cd89af033732e010e595a2fef5a897a370b50b7bc9b95513fbdf602f1be2
---

# Cutting UI Improvements — 2. Ügyfél Felkészülés

## Kontextus

A **Doorstar Soft Launch LIVE** (2026-06-17). A következő üzleti prioritás a **2026 Q3 Szabászat modul** frontend finomítása és **2. ügyfél** fogadása.

**Frontend Cutting jelenlegi állapot:**
- ✅ ProductionPage.tsx működik (nesting + scheduling tab)
- ✅ useCuttingPlanGeneration hook — POST /api/cutting/plans, polling
- ✅ NestingViewer komponens — nesting vizualizáció
- ✅ BatchScheduler & BatchTimeline — ütemezés

---

## Feladat: Cutting UI Finomítások + Hiányzó Funkciók

### 1. Nesting Vizualizáció Javítások

**ProductionPage.tsx → NestingViewer komponens:**

**Ellenőrizd:**
- [ ] Parts zoom-olhatók?
- [ ] Waste percentage megjelenik minden sheet-nél?
- [ ] Színkód: parts (zöld), waste (piros)
- [ ] Tooltip: part ID, dimensions (w×h)

**Hiányzó funkciók:**
- **Pan & Zoom:** Touch-friendly drag + scroll zoom
- **Part highlight:** Kattintásra part highlight + detail panel
- **Export:** PNG export button (screenshot)

---

### 2. Batch Scheduling UI Bővítés

**BatchScheduler & BatchTimeline komponensek:**

**Ellenőrizd:**
- [ ] Drag & Drop működik? (batch átrendezés időrendben)
- [ ] Machine assignment dropdown működik?
- [ ] Batch státusz vizualizáció (Pending, Running, Done)

**Hiányzó funkciók:**
- **POST /api/cutting/plans/{date}/assign-batch integráció**
  - API call amikor batch-et hozzárendel operátor
  - Success → refetch + toast notification
- **Real-time progress bar** (batch completion %)
- **Operator assignment dropdown** (ki dolgozza fel a batch-et)

---

### 3. Analytics Dashboard (új oldal)

**Új komponens: `CuttingAnalyticsPage.tsx`**

**Funkciók:**
- **Waste Trend Chart** — `GET /api/cutting/analytics/waste`
  - 7 napos trend (waste %)
  - Recharts Line chart
- **Machine OEE Dashboard** — `GET /api/cutting/analytics/oee`
  - OEE score / machine (Availability × Performance × Quality)
  - KPI kártyák + bar chart
- **Material Usage** — anyag felhasználás (m² / nap)

**Hozzáadás:**
- Új route: `/w/production/analytics`
- ProductionPage tab bővítés: cutting | machining | analytics

---

### 4. Error Handling + Loading States

**Ellenőrizd:**
- [ ] useCuttingPlanGeneration error state → toast notification?
- [ ] Polling timeout (5 perc) → friendly error message?
- [ ] API 500 → fallback mock vagy retry gomb?

**Javítások:**
- Error boundary a ProductionPage-en
- Retry gomb failed API call-nál
- Skeleton loading minden fetch alatt

---

### 5. Mobile Responsiveness

**ProductionPage.tsx responsive ellenőrzés:**
- [ ] Nesting grid responsive (sm: 1 column, lg: 2 column)?
- [ ] Batch timeline horizontális scroll mobilon?
- [ ] Touch-friendly gombok (min h-10)?

**Javítások:**
- Tailwind breakpoint audit (sm, md, lg)
- Touch gesture support (pinch-to-zoom)

---

## Definition of Done

- [ ] Nesting zoom + pan működik
- [ ] Batch scheduling API integráció (POST assign-batch)
- [ ] Analytics dashboard implementálva (új oldal)
- [ ] Error handling + retry gomb minden API call-nál
- [ ] Mobile responsive ellenőrizve
- [ ] `pnpm build` ✅ 0 error
- [ ] `pnpm test` ✅ 0 failed (vagy max 8 failed mint jelenleg)

---

## Referenciák

- **ProductionPage:** `src/pages/ProductionPage.tsx`
- **useCuttingPlanGeneration:** `src/hooks/useCuttingPlanGeneration.ts`
- **NestingViewer:** `src/components/NestingViewer.tsx`
- **BatchScheduler:** `src/components/BatchScheduler.tsx`

---

## Prioritás

**MEDIUM** — 2-3 nap munka, nem blokkoló de fontos a 2. ügyfél előtt
