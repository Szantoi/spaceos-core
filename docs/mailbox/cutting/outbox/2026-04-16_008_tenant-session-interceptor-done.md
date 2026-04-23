---
id: MSG-CUTTING-008-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-008
created: 2026-04-16
---

## Összefoglaló

`TenantSessionInterceptor` implementálva és regisztrálva mindhárom Infrastructure projektben.
A PostgreSQL RLS `current_setting('app.current_tenant_id')::uuid` hiba elhárult.

### Új fájlok

- `SpaceOS.Modules.Cutting.Infrastructure/Persistence/TenantSessionInterceptor.cs`
- `SpaceOS.Modules.Inventory.Infrastructure/Persistence/TenantSessionInterceptor.cs`
- `SpaceOS.Modules.Procurement.Infrastructure/Persistence/TenantSessionInterceptor.cs`

### Módosított fájlok

Mindhárom `ServiceCollectionExtensions.cs`:
- `AddHttpContextAccessor()` + `AddSingleton<TenantSessionInterceptor>()` hozzáadva (a DbContext elé)
- `AddDbContext` lambda bővítve: `(sp, options) =>` + `options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>())`

### Commitok

| Repo | Commit |
|---|---|
| spaceos-modules-inventory | `a363ad6` |
| spaceos-modules-procurement | `0dbb02e` |
| spaceos-modules-cutting | `1ae66a0` |

## Tesztek

| Repo | Teszt | Eredmény |
|---|---|---|
| spaceos-modules-inventory | 47 | ✅ mind zöld |
| spaceos-modules-procurement | 42 | ✅ mind zöld |
| spaceos-modules-cutting | 64 (54 + 10 contracts) | ✅ mind zöld |
| **Összesen** | **153** | **✅** |

## Security review

- `set_config` paraméteres lekérdezéssel hívva (`@key`, `@value`) — SQL injection-safe
- `ConnectionClosingAsync`: minden connection-close-kor reset üres string-re → connection pool cleanup, nem "szivárog" tenant kontextus
- `ResolveTenantId()`: `Guid.Empty` esetén `null` → üres string kerül be (nem hibás UUID string)
- `tid` claim — összhangban a MSG-CUTTING-007 javítással
- Az interceptor `internal sealed` — nem publikus API

## Kockázatok / megjegyzések

Az egységtesztekben az interceptor nem fut (EF Core InMemory nem hívja a `DbConnectionInterceptor` metódusokat) — ez szándékos és helyes viselkedés. Az RLS integráció az E2E suite-on (PostgreSQL) verifikálandó.
