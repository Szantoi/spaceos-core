---
id: MSG-BACKEND-105
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: sonnet
created: 2026-07-02
content_hash: 3dc22e4ecf564d735ed322360d5eb390509326331638a08ec2fbdaee712defdb
---

# JoineryTech Backend API Architektúra Tervezés

## Kontextus

A **JoineryTech Portal** jelenleg egy localStorage-alapú prototípus, de a terv szerint **valódi backend-re kell átállni**:
- **Jelenlegi:** React + Babel + localStorage (`window.sim` store)
- **Cél:** localStorage → valódi backend API (REST/GraphQL)
- **Lokáció:** `/opt/spaceos/docs/joinerytech/`
- **Verzió:** 3.33
- **Modulok:** 40+ (CRM, Sales, Production, Warehouse, HR, Maintenance, EHS, QA, DMS, stb.)

## Feladat

Tervezd meg a JoineryTech backend API architektúráját és az átállási roadmapot:

### 1. API Architektúra Tervezés

**Technológiai döntések:**
- Backend framework választás (.NET 8 / Node.js / egyéb?)
- API stílus (REST / GraphQL / mindkettő?)
- Adatbázis (PostgreSQL / MongoDB / egyéb?)
- Authentication/Authorization (JWT / OAuth / session-based?)
- Real-time kommunikáció szükségessége (WebSocket / SSE?)

**Architektúra minták:**
- Microservices vs Monolith (vagy Modular Monolith)?
- CQRS, Event Sourcing alkalmazhatósága?
- API Gateway szükségessége?

**Elvárás:**
- Technológiai stack javaslat indoklással
- Architektúra diagram (magas szintű)
- Döntési mátrix (előnyök/hátrányok)

### 2. Adatmodell Mapping

A jelenlegi `window.sim` store szerkezete → backend adatbázis modell:

**Főbb entitások (lásd app-store.jsx):**
- `sim.quotes` (ajánlatok)
- `sim.orders` (rendelések)
- `sim.customers` (ügyfelek)
- `sim.materials` (anyagok/készlet)
- `sim.catalog` (katalógus)
- `sim.employees` (dolgozók)
- `sim.leads`, `sim.opportunities` (CRM)
- `sim.projects` (projektek)
- `sim.assets`, `sim.workOrders` (karbantartás)
- `sim.ehsIncidents`, `sim.ehsRisks` (munkavédelem)
- `sim.qaInspections` (minőségbiztosítás)
- `sim.documents` (dokumentumtár)
- ... és még 20+ egyéb entitás

**Elvárás:**
- Entitás→Táblázat mapping terv
- Relációk és indexek tervezése
- Migrációs stratégia (hogyan kerül át a localStorage adat?)

### 3. Auth/Auth Stratégia

A jelenlegi `portal.jsx` jogosultság rendszer → backend auth:

**Jelenlegi jogosultságok:**
- Fiók-típusok: belső munkatárs, B2B partner, viszonteladó, B2C ügyfél
- Jogosultság-alapú hozzáférés (pl. `quote.create`, `crm.manage`, `hr.manage`)
- Profil-alapú világ-aktiválás

**Elvárás:**
- JWT token-alapú auth terv
- Role-Based Access Control (RBAC) modell
- Multi-tenant architektúra (ha releváns)
- Session management stratégia

### 4. API Endpoints Specifikáció

Tervezd meg a főbb API endpoint-okat:

**Példa modulokra:**
- **CRM:** `GET /api/leads`, `POST /api/leads/{id}/convert`, `GET /api/opportunities`
- **Sales:** `GET /api/quotes`, `POST /api/quotes`, `PUT /api/quotes/{id}/approve`
- **Production:** `GET /api/production-orders`, `POST /api/production-orders/{id}/release`
- **Warehouse:** `GET /api/inventory`, `POST /api/inventory/movements`

**Elvárás:**
- OpenAPI/Swagger spec vázlat (nem kell teljes, de 5-10 endpoint részletesen)
- Request/Response modellek
- Error handling stratégia

### 5. localStorage → Backend Migráció Roadmap

**Átállási terv:**
- Fokozatos migráció vs. Big Bang?
- Mely modulok menjenek át először? (Critical path)
- Backward compatibility stratégia (localStorage fallback?)
- Testing stratégia az átállás során

**Elvárás:**
- Fázisos migráció terv (Phase 1, 2, 3...)
- Rizikó analízis
- Rollback terv

## Fájlok és erőforrások

**Kulcsfontosságú dokumentumok:**
- `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` - projekt áttekintés
- `/opt/spaceos/docs/joinerytech/CLAUDE.md` - FSM állapotgépek, entitás definíciók
- `/opt/spaceos/docs/joinerytech/app-store.jsx` - teljes store struktúra

**Kapcsolódó SpaceOS backend kód (referencia):**
- `/opt/spaceos/spaceos-kernel/` - .NET 8 példa
- `/opt/spaceos/spaceos-orchestrator/` - Node.js példa

## Sikerkritérium

1. **Backend Architektúra Terv** dokumentum készül:
   - Technológiai stack döntések indoklással
   - Magas szintű architektúra diagram
   - API design principles

2. **Adatmodell Mapping Terv:**
   - Entitás-táblázat mapping
   - Relációk és indexelés terv

3. **Auth/Auth Specifikáció:**
   - RBAC modell részletezése
   - Token management stratégia

4. **API Endpoints Catalog:**
   - OpenAPI spec vázlat (5-10 főbb endpoint)

5. **Migráció Roadmap:**
   - Fázisos átállási terv
   - Rizikó analízis és mitigáció

## Megjegyzés

Ez egy **tervezési feladat** - nem kell implementálni, csak megtervezni a backend architektúrát és az átállási utat. A cél egy **átfogó terv dokumentum**, ami alapján a backend fejlesztés later phase-okban elindítható.

**Fontos szempontok:**
- **FSM-alapú architektúra** - a backend is támogassa az állapotgépeket
- **Domén-független** felé haladás (verticalizálhatóság más iparágakra)
- **Production-ready** cél - skálázhatóság, biztonság, monitoring
