# SpaceOS — Architektúra Q&A (2026-04-17)

> Kódbázis-alapú válaszok 6 tervezési kérdésre a Doorstar fejlesztés előtt.

---

## 1. OpenAPI spec / Swashbuckle

**Felkonfigurálva van, de csak Development módban él.**

- Package: `Swashbuckle.AspNetCore 6.9.0` (`SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj`)
- Spec URL: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger/index.html`
- JWT Bearer security scheme regisztrálva ✅
- `PagedListSchemaFilter` + `EnumStringSchemaFilter` ✅
- **VPS-en (Production) → 404** — a `UseSwagger` egy `if (IsDevelopment())` blokk mögé van zárva (`Program.cs` ~422. sor)

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "SpaceOS v1"));
}
```

**Tennivaló (döntés szükséges):**

| Opció | Előny | Hátrány |
|---|---|---|
| `dotnet swagger tofile` build-time generálás | Prod-safe, CI-ba integrálható | Extra build lépés |
| `ASPNETCORE_ENVIRONMENT=Development` VPS-en | Azonnal elérhető | Prod security kockázat (dev error pages, stb.) |

Ha az API client generálás a cél (ADR-07), a `dotnet swagger tofile` az ajánlott irány.

---

## 2. Project aggregate

**Nincs.** Sem a Kernelben, sem a Joinery modulban nem létezik Project aggregate.

Ami létezik:

- **`ProjectInfo`** — Value Object a `DoorOrder` aggregáton belül (`SpaceOS.Modules.Joinery.Domain/ValueObjects/ProjectInfo.cs`)

```csharp
public sealed record ProjectInfo
{
    public string? ClientName    { get; init; }
    public string? ClientAddress { get; init; }
    public string? ClientPhone   { get; init; }
    public DateOnly? DeliveryDate { get; init; }
}
```

Ez metaadat a rendelésen — nem önálló aggregate, nem perzisztált külön táblában.

**Lehetséges irányok (döntés szükséges):**

| Opció | Leírás | Hol élne |
|---|---|---|
| A — Project = DoorOrder alias | Csak UI névadás, nincs új aggregate | Nincs teendő |
| B — Project = több DoorOrder összefogója | Egy ügyfélnek több rendelés egy projektben | Joinery modul |
| C — Project = Kernel-szintű entitás | FlowEpic-hez kötött, cross-module projekt | Kernel |

A jelenlegi architektúrában a **FlowEpic** (`spaceos-kernel`) játssza a projekt-szerű szerepet — ez köti össze a munkafolyamatokat, és minden `DoorOrder`-nek van `FlowEpicId`-ja. Ha a Project = FlowEpic alias elegendő, nincs szükség új aggregate-re.

---

## 3. Keycloak realm struktúra

**Egy realm + group-os tenant separation — már él élesben.**

```
Realm:   spaceos
Groups:  doorstar-kft          ← tenant = Keycloak group
Clients: kernel-api
         orchestrator-bff
         portal-app
         test-runner
```

**Tenant azonosítás:** `tid` claim a JWT token-ben (Script Mapper adja). A Kernel `MapInboundClaims = false` beállítással olvassa (KERNEL-082 fix, 2026-04-16).

**Nincs realm-per-tenant** — tudatos ADR döntés:
- Realm-per-tenant: magas adminisztrációs komplexitás + resource-igény KKV skálán
- Single realm + `tid` claim: egyszerű, jól skálázható, új tenant = új Keycloak group + user

**Tenant switching UX következménye:** új token kell különböző `tid`-del — ez PKCE flow újrafuttatást jelent.

---

## 4. DoorStar role model

**Jelenlegi éles szerepek (Keycloak `spaceos` realm):**

| Szerepkör | Kernel policy | Hatáskör |
|---|---|---|
| `Joiner` | `ReadPolicy` | Olvasás |
| `Designer` | `ReadPolicy` + `WritePolicy` | Olvasás + írás |
| `Admin` | `ReadPolicy` + `WritePolicy` + `AdminPolicy` | Teljes hozzáférés |

**Kernel Stage Registry RBAC (külön réteg):**

| Szerepkör | Policy | Hatáskör |
|---|---|---|
| `SystemAdmin` | `SystemAdminPolicy` | Rendszerszintű |
| `TenantAdmin` | `TenantAdminPolicy` | Tenant kezelés |
| `StageOperator` | `StageOperatorPolicy` | Gyártási lépések |

**Joinery modul:** `ManufacturerOnly` policy — csak gyártói hozzáférés.

**Nem létező, tervezendő szerepek:**

| Igény | Javasolt szerepnév | Megjegyzés |
|---|---|---|
| Projektmenedzser | `PM` vagy `ProjectManager` | Projektek létrehozása, státusz követés |
| Műhelyvezető | `Workshop` vagy `ShopFloor` | Gyártási feladatok kezelése |
| Csak olvasó / riporting | `Viewer` vagy `ReportingOnly` | Nincs write access |

Ha ezek szükségesek, Keycloak realm + Kernel `AddPolicy` kiegészítés kell.

---

## 5. Deploy target

**VPS + systemd + PM2 — nincs Docker, nincs Kubernetes.**

```
Kernel          → systemd (spaceos-kernel.service)        port 5000  loopback-only
Joinery         → systemd (spaceos-joinery.service)       port 5002  loopback-only
Abstractions    → systemd (spaceos-abstractions.service)  port 5003  loopback-only
Inventory       → systemd (spaceos-inventory.service)     port 5004  loopback-only
Cutting         → systemd (spaceos-cutting-svc.service)   port 5005  loopback-only
Procurement     → systemd (spaceos-procurement.service)   port 5006  loopback-only
Orchestrator    → PM2 (spaceos-orchestrator)              port 3000  loopback-only
Portal          → nginx static files                      port 443   public HTTPS
Keycloak        → systemd                                 port 8080  loopback-only
PostgreSQL      → systemd                                 port 5433  loopback-only
```

**Nginx** kezeli a TLS-t (Let's Encrypt, joinerytech.hu + asztalostech.hu SAN cert) és proxy-z a BFF-re.

**CI/CD:** jelenleg nincs automatizált pipeline. Deploy folyamat:
```bash
git pull origin develop
rm -rf publish          # GOTCHA: kötelező az inkrementális build előtt
dotnet publish -c Release -o publish
sudo systemctl restart <service>
curl -s http://127.0.0.1:<port>/healthz
```

Részletek: `docs/knowledge/deployment/DEPLOYMENT_RUNBOOK.md` és `KNOWN_GOTCHAS.md`.

---

## 6. SpaceOS / Doorstar branding

**Jelenleg nincs definiált brand rendszer.**

- A Tailwind config üres: `theme: { extend: {} }` — nincs custom color palette
- Nincs logó asset a repóban
- Az `App.css` Vite/React starter template maradvány (`var(--accent)` változók)
- A `#3B5AD8` **nem szerepel** a kódbázisban

**Ha `#3B5AD8` primary — ez elfogadható kiindulópont.** Javasolt design token struktúra:

```js
// tailwind.config.ts
theme: {
  extend: {
    colors: {
      primary:   { DEFAULT: '#3B5AD8', hover: '#2D46B0', light: '#EEF1FC' },
      secondary: { DEFAULT: '#1E293B' },
      accent:    { DEFAULT: '#F59E0B' },
    }
  }
}
```

**Tennivaló (döntés szükséges):**

| Elem | Státusz |
|---|---|
| Logó (SVG) | Nincs — kell vagy placeholder? |
| Primary szín | `#3B5AD8` javasolt, jóváhagyás kell |
| Typography | Jelenleg rendszer font — Inter/Geist ajánlott |
| Dark mode | Nem tervezett egyelőre |

---

## Összefoglalás — mi kész, mi vár döntésre

| # | Kérdés | Státusz | Tennivaló |
|---|---|---|---|
| 1 | OpenAPI / Swashbuckle | ⚠️ Csak Dev módban | Build-time gen döntés |
| 2 | Project aggregate | ❌ Nincs | DDD design: FlowEpic elég? Vagy új aggregate? |
| 3 | Keycloak realm struktúra | ✅ Éles, single realm + tid | — |
| 4 | Role model (PM/Workshop/Reporting) | ⚠️ Alap szerepek élnek | Új szerepkörök tervezése |
| 5 | Deploy target | ✅ VPS + systemd + PM2 | — |
| 6 | Brand / design tokens | ❌ Nincs | Tailwind config + logó döntés |
