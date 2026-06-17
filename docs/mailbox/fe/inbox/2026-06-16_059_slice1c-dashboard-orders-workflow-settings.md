---
id: MSG-FE-059
from: pipeline
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FE-058-DONE
created: 2026-06-16
---

# FE-059 — Slice 1C: Dashboard + Orders + Workflow + Settings + Analytics mock-mentes

## Kontextus

A FE Domain Matrix Slice 1 API integrációs sprint keretében. FE-056 (Production/Supervisor/Design), FE-057 (AiPage/MfgPrep), FE-058 (QualityPage) DONE. Most a CORE domain fennmaradó PARTIAL oldalai következnek: mock fallback teljes eltávolítása, EndpointPending ahol a backend endpoint nem létezik.

Referencia: `docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md`
API audit részletek: `frontend/joinerytech-portal/API_INTEGRATION_STATUS.md`

---

## Feladatok (5 oldal)

### 1. DashboardPage — mock fallback eltávolítás

**Backend:** Kernel `/api/*` + Joinery `/joinery/*`
**Teendő:**
- Azonosítsd a mock fallback import/usage pontokat (pl. `MOCK_DASHBOARD`, `MOCK_METRICS`, stb.)
- Ha az API endpoint létezik → csak a fallback logikát töröld, a real API hívás maradjon
- Ha az API endpoint **nem létezik** → `EndpointPending endpoint="GET /..." [?]` banner
- Mock importokat töröld

### 2. OrdersPage — mock fallback eltávolítás

**Backend:** Joinery `/joinery/*`
**Teendő:**
- Mock fallback eltávolítás
- `GET /joinery/api/orders` létezik (page+pageSize params) → real API marad
- `?status=` szűrés **nem létezik** a Joinery-ben (FE-058 konfirmálta) → ha ilyen hívás van, `EndpointPending` banner
- Mock importok törlése

### 3. WorkflowPage — mock fallback eltávolítás

**Backend:** Kernel `/api/*`
**Teendő:**
- Mock fallback eltávolítás
- `GET /api/flows`, `GET /api/tools/workstations` léteznek → real API marad
- Ahol endpoint nem létezik → `EndpointPending` banner
- Mock importok törlése

### 4. SettingsPage — részleges mock fallback eltávolítás

**Backend:** Kernel + Abstractions + Identity
**Teendő:**
- Az elérhető szekciókhoz (Users, Templates stb.) a mock fallback-et töröld
- `Roles`, `Partners`, `Stages` szekciók → **deferred** (ha mock-ból jön, hagyj `EndpointPending` bannert)
- Ne törd el a már működő Identity UsersPanel és TemplatesPanel integrációkat

### 5. AnalyticsPage — mock fallback eltávolítás

**Backend:** Cutting `/cutting/*`
**Teendő:**
- Mock fallback eltávolítás
- Ahol Cutting endpoint létezik → real API marad
- Ahol nem → `EndpointPending endpoint="GET /cutting/api/..." [?]` banner
- Mock importok törlése

---

## Elvárások

- `pnpm build` zöld (0 error)
- `pnpm test --run` zöld (tesztszám: 724 vagy magasabb)
- Minden mock import törölve (vagy `EndpointPending`-re cserélve)
- Minden `EndpointPending` banner `[?]` jellel jelölve (nem létező backend endpoint)

---

## DONE outbox üzenet tartalma (kötelező)

```
id: MSG-FE-059-DONE
from: fe
to: root
type: done
ref: MSG-FE-059

Összefoglalás: [mit csináltál]
Oldalak: [mi lett mock-mentes, mi kapott EndpointPending]
Nyitott [?]: [endpoint-ok amik hiányoznak]
Tesztek: [N]/[N] zöld
```

Skill: `/spaceos-terminal`
