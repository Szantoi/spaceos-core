---
id: MSG-ABSTRACTIONS-006-DONE
from: abstractions
to: root
type: response
priority: high
status: READ
ref: MSG-ABSTRACTIONS-006
created: 2026-04-15
---

# MSG-ABSTRACTIONS-006 — Security Review (Q2 Pre-launch) — DONE

**Kritikus találat: NINCS.** Kódmódosítás nem szükséges (feladat feltétele szerint).

---

## 1. Authentication & Authorization

| Ellenőrzés | Státusz | Megjegyzés |
|---|---|---|
| Minden endpoint ManufacturerOnly policy-val | ✅ | Route group-on `RequireAuthorization("ManufacturerOnly")` |
| `/health` anonym elérhető | ✅ | `.AllowAnonymous()` explicit |
| JWT `tenant_id` claim-ből, nem request body-ból | ✅ | `GetTenantId(ctx)` a JWT-ből veszi |
| `ValidateAudience` | ⚠️ MEDIUM | `ValidateAudience = false` — lásd lent |
| `RequireHttpsMetadata` | ⚠️ LOW | `false` — belső 127.0.0.1 service, elfogadható |

### ⚠️ MEDIUM-01: `ValidateAudience = false` (Program.cs:26)

```csharp
opts.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = false,  // ← PROBLÉMA
};
```

Az `appsettings.Development.json`-ban `"Audience": "kernel-api"` be van állítva, de figyelmen kívül van hagyva. Más audience-re kiadott érvényes token (pl. portal, orchestrator) is elfogadásra kerül.

**Javaslat:**
```csharp
ValidateAudience = true,
ValidAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
```

---

## 2. Row-Level Security (RLS)

| Ellenőrzés | Státusz | Megjegyzés |
|---|---|---|
| `app.tenant_id` SET íráskor | ✅ | `TenantSessionInterceptor` — `SavingChangesAsync` |
| `app.tenant_id` SET olvasáskor | ⚠️ MEDIUM | Interceptor csak `SaveChangesInterceptor` — olvasáskor NEM fut |
| Handler-szintű tenant check | ✅ | Minden handler: `if (template.TenantId != request.TenantId) → Forbidden()` |
| Repository tenant filter | ⚠️ MEDIUM | `GetTemplateAsync` / `GetTemplateWithAllAsync` csak ID-ra szűr |

### ⚠️ MEDIUM-02: `TenantSessionInterceptor` csak write-path-en fut

`TenantSessionInterceptor extends SaveChangesInterceptor` — read query-k NEM állítják be az `app.tenant_id` session változót. Ha az RLS FORCE policy `current_setting('app.tenant_id', true)` alapján szűr, olvasáskor a DB-szintű védelem nem aktív.

**Megjegyzés:** A handler-szintű check (`template.TenantId != request.TenantId`) az alkalmazás szintjén véd. Az RLS defense-in-depth gyengébb az olvasási útvonalon.

**Javaslat:** `DbCommandInterceptor` használata olvasásra is (vagy `IDbContextFactory` + session init).

### ⚠️ MEDIUM-03: Repository nem szűr TenantId-ra olvasáskor

```csharp
// AbstractionsRepository.cs:17 — nincs TenantId szűrő!
public async Task<ProductTemplate?> GetTemplateAsync(Guid id, CancellationToken ct = default) =>
    await _db.ProductTemplates.AsNoTracking()
             .FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
```

`GetTemplateAsync` és `GetTemplateWithAllAsync` csak `id` alapján tölt. Cross-tenant IDOR kizárólag a handler-szintű ellenőrzésen és a DB-szintű RLS-en múlik.

**Javaslat:** `GetTemplateAsync(Guid id, Guid tenantId, CancellationToken ct)` — szűrő a query-ban is.

---

## 3. Graph Engine — DoS elemzés

| Ellenőrzés | Státusz | Megjegyzés |
|---|---|---|
| Write-time BFS nem rekurzív | ✅ | BE-02 compliant iteratív BFS |
| HasPath() O(E) per call, max O(E²) összesen | ⚠️ LOW | 500 connection limittel max O(250K) — elfogadható |
| Slot limit (200) + Connection limit (500) | ✅ | DoS attack surface korlátozva |
| GUID validáció Application rétegen | ✅ | `AddSlotConnectionValidator`: `NotEmpty()` minden GUID-ra |
| Körkörös referencia a cycle detection kódjában | ✅ | `HasPath()` `visited` HashSet-tel van megvédve |

---

## 4. Input Validation

| Ellenőrzés | Státusz | Megjegyzés |
|---|---|---|
| FluentValidation minden Command-on | ✅ | Ellenőrzött: AddSlotConnection, CreateTemplate, AddSlot, SetParameter |
| SQL injection | ✅ | EF Core paraméteres query mindenhol |
| `SET app.tenant_id = '{tenantId}'` interpoláció | ⚠️ LOW | Guid típus csak `[0-9a-f-]` karaktereket tartalmaz — gyakorlati kockázat: 0 |
| Empty GUID guard | ✅ | Validator `NotEmpty()` + C# Guid típus |

---

## 5. Sensitive Data

| Ellenőrzés | Státusz | Megjegyzés |
|---|---|---|
| Stack trace response-ban | ✅ | Nincs `UseDeveloperExceptionPage()` explicit production-ban |
| PII logban | ✅ | Nem azonosítható személyes adat a logolt mezőkben |
| Cache-Control: no-store | ✅ | `/cutting-list`, `/cnc-plan`, `/process-plan` endpontokon beállítva |

---

## 6. OWASP Top 10 Rapid Check

| # | Kategória | Státusz | Megjegyzés |
|---|---|---|---|
| A1 | Broken Access Control | ⚠️ MEDIUM | Cross-tenant: handler check ✅, DB query nincs tenant-filterrel (MEDIUM-03) |
| A3 | Injection | ✅ | EF Core paraméteres; SET interpoláció Guid-dal — biztonságos |
| A4 | Insecure Design | ✅ | 200 slot + 500 connection limit = implicit depth/DoS korlát |
| A5 | Security Misconfiguration | ⚠️ MEDIUM | `ValidateAudience = false` (MEDIUM-01) |

---

## Összesítés

| Szint | Darab | Azonosítók |
|---|---|---|
| KRITIKUS | 0 | — |
| MEDIUM | 3 | MEDIUM-01, MEDIUM-02, MEDIUM-03 |
| LOW | 3 | RequireHttpsMetadata, SET interpoláció, HasPath O(E²) |

**Kód módosítás nem készült** (nincs kritikus találat). A három MEDIUM finding root döntést igényel a javítás prioritásáról (Q2 launch előtt ajánlott legalább MEDIUM-01 és MEDIUM-03 javítása).
