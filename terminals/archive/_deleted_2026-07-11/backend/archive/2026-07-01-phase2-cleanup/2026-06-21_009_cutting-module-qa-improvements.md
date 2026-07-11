---
id: MSG-BACKEND-009
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-CONDUCTOR-006
product: spaceos
created: 2026-06-21
processed: 2026-06-21
content_hash: 448f4a2784a26f6c3dec5f4edbd68c8c28632ad546def2e38f4ecade75a1818d
---

# Cutting Modul Q&A Improvements — 2. Ügyfél Felkészülés

## Kontextus

A **Doorstar Soft Launch LIVE** (2026-06-17). A következő üzleti prioritás a **2026 Q3 Szabászat modul** stabilizálása és **2. ügyfél** fogadása.

**Cutting modul jelenlegi állapot:**
- ✅ 949 teszt PASS (939+10)
- ✅ API működik: POST /api/cutting/plans, GET /api/cutting/sheets/{id}/nesting
- ✅ Backend modulok: Domain, Application, Infrastructure, Analytics, Execution
- ✅ Nesting algoritmus NuGet csomag

---

## Feladat: Cutting Modul Stabilizációs Audit + Javítások

### 1. Test Coverage Audit ✅

**Feladat:**
- Futtasd a `dotnet test --collect:"XPlat Code Coverage"` parancsot
- Azonosítsd a <80% lefedettségű modulokat
- Írd ki a test coverage report-ot

**Cél:** Minden modul >80% code coverage

---

### 2. API Endpoint Hiányzó Funkciók

**Ellenőrizd:**

| Funkció | Endpoint | Status | Prioritás |
|---|---|---|---|
| Batch scheduling | `POST /api/cutting/plans/{date}/assign-batch` | ✅ Megvan | ✅ |
| Waste analytics | `GET /api/cutting/analytics/waste` | ✅ Megvan | ✅ |
| Machine OEE | `GET /api/cutting/analytics/oee` | ❓ Ellenőrizd | HIGH |
| Daily schedule | `GET /api/cutting/planning/{planId}/daily/{date}` | ✅ Megvan | ✅ |
| Offcut tracking | `POST /api/cutting/offcuts` | ❓ Ellenőrizd | MEDIUM |

**Hiányzó endpoint-ok implementálása:**
- Ha OEE endpoint nincs → implementáld
- Ha Offcut tracking nincs → implementáld

---

### 3. Error Handling Audit

**Ellenőrizd:**
- Minden POST/PUT endpoint ad-e vissza validation error-t bad input-nál?
- 500-as hibák loggolva vannak?
- Domain exception-ök helyesen map-elődnek HTTP status kódokra?

**Javítások:**
- Add hozzá a hiányzó input validációkat
- Ensure all 500s are logged with context

---

### 4. Performance Testing

**Feladat:**
- Futtass benchmark tesztet nagy adathalmazra (100+ order, 500+ part)
- Nesting algoritmus teljesítmény: <10s / 100 part
- Database query optimalizáció: n+1 query audit

**Cél:** Nesting generation <10s, API response time <500ms (p95)

---

### 5. Integration Tests

**Feladat:**
- E2E integration test: POST cutting plan → nesting → assign batch → complete
- Ellenőrizd, hogy az FSM (Draft → Planned → Running → Done) működik
- Teszt: inventory reservation integration (panel reservation)

**Cél:** Full workflow integration test coverage

---

## Definition of Done

- [ ] Test coverage >80% minden modulon
- [ ] Hiányzó API endpoint-ok implementálva (OEE, Offcut tracking)
- [ ] Error handling audit befejezve, validation javítások
- [ ] Performance benchmark futott, <10s nesting, <500ms API
- [ ] Integration tests írva (E2E workflow)
- [ ] `dotnet build` ✅ 0 error
- [ ] `dotnet test` ✅ 0 failed

---

## Referenciák

- **Cutting modul path:** `/opt/spaceos/backend/spaceos-modules-cutting/`
- **Tests:** `tests/SpaceOS.Modules.Cutting.Tests/`
- **API Endpoints:** `src/SpaceOS.Modules.Cutting.Api/Endpoints/`

---

## Prioritás

**MEDIUM** — 2-3 nap munka, nem blokkoló de fontos a 2. ügyfél előtt
