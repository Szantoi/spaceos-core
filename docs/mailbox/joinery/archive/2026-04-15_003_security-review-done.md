---
id: MSG-JOINERY-004-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-004
created: 2026-04-15
---

# MSG-JOINERY-004-DONE — Security Review (Q2 Pre-launch)

## Összefoglaló

**Verdict: PASS** — Nincs CRITICAL találat. 1 MEDIUM javítva, 2 MEDIUM + 4 LOW root koordinációt igényel.

Teljes riport: `SECURITY_REPORT_JOINERY.md`

---

## Területenként — összefoglaló

### 1. Authentication & Authorization ✅ (1 MEDIUM)

- Minden endpoint `ManufacturerOnly` route group alá van kötve ✓
- Cross-tenant resource → 404 (nem 403, tenant enumeration ellen) ✓
- **MEDIUM-2**: `ValidateAudience = false` — bármely token elfogadott ha azonos realm + Manufacturer claim → ajánlott `ValidateAudience = true` + `kernel-api` audience
- **MEDIUM-3**: Ha `Jwt:Authority` és `JWT_AUTHORITY` mindkettő hiányzik, az app elindul de minden hívást elutasít — fail-fast startup guard javasolt

### 2. Row-Level Security ✅

- RLS FORCE mind a 7 tenant-táblán megvan ✓
- `TenantSessionInterceptor` paraméteres `set_config` minden connection open/close-on ✓
- `DoorOrders`, `DoorItems`, `CuttingListSnapshots`, `ProductionSheetCaches`, `JoineryOutboxEntries` + 2 egyéb tábla védve ✓

### 3. PDF Generálás ✅

- QuestPDF text renderelés — **nem HTML template**, HTML injection strukturálisan lehetetlen ✓
- Fájlútvonal csak `Guid.ToString("N")` + SHA-256 hex — path traversal lehetetlen ✓
- DB-06 CHECK constraint (`!~ '^\.\.'`) második védelmi réteg ✓
- `DoorOrderRevertedEventHandler` törlés előtt `File.Exists()` ellenőr ✓
- **LOW-4**: QuestPDF wildcard pin (`2024.12.*`) — rögzíteni javasolt exact verzióra

### 4. Input Validation ✅ (MEDIUM-1 JAVÍTVA)

- `DoorDimensions.Create()`: min (≤0) + max press size (2600/3000mm) validálva ✓
- EF Core csak paraméteres query (nincs string concat) ✓
- **MEDIUM-1 JAVÍTVA**: `pageSize` most `Math.Clamp(pageSize, 1, 100)` — unbounded TAKE megakadályozva

### 5. Sensitive Data ✅

- Nincs stack trace a response-ban ✓
- PII (ClientName) nem jelenik meg Info/Debug logban ✓
- Cross-tenant kalkulációs adat nem szivárog ✓

### 6. OrchestratorClient ✅

- 10s per-attempt timeout ✓
- 4xx → azonnali hiba, nincs retry ✓
- 5xx → exponential backoff retry (0s, 2s, 5s) ✓

### 7. OWASP Top 10 ✅

- A1 (Access Control): cross-tenant → 404 ✓
- A3 (Injection): csak paraméteres EF query ✓
- A4 (Insecure Design): ManufacturerOnly policy minden route-on ✓
- A5 (Misconfiguration): nincs hardcoded secret, `appsettings.json` placeholder ✓

---

## Javított problémák

### MEDIUM-1 FIX — pageSize clamp

```diff
// SpaceOS.Modules.Joinery.Api/Endpoints/DoorOrderEndpoints.cs
- var result = await mediator.Send(new ListDoorOrdersQuery(tenantId, page, pageSize), ct)
+ var clampedPageSize = Math.Clamp(pageSize, 1, 100);
+ var result = await mediator.Send(new ListDoorOrdersQuery(tenantId, page, clampedPageSize), ct)
```

---

## Root koordinációt igénylő pontok

| ID | Súlyosság | Leírás | Javasolt intézkedés |
|---|---|---|---|
| MEDIUM-2 | Közepes | `ValidateAudience = false` | `ValidateAudience = true` + `kernel-api` audience bekapcsolása — Kernel JWT konfiggal összehangolva |
| MEDIUM-3 | Közepes | Nincs startup fail-fast ha JWT Authority hiányzik | `ArgumentNullException.ThrowIfNull(authority)` startup-ban |
| LOW-1 | Alacsony | `Database.MigrateAsync()` production indításkor | Productionban letiltani vagy pre-deploy stepbe mozgatni |
| LOW-2/3 | Alacsony | `FromSqlRaw` unqualified táblanév | `spaceos_joinery."JoineryOutboxEntries"` prefix |
| LOW-4 | Alacsony | QuestPDF wildcard verzió | Exact version pin + approved package listára felvenni |

---

## Tesztek

`dotnet build` → 0 error, 0 warning
`dotnet test` → 202/202 passed
