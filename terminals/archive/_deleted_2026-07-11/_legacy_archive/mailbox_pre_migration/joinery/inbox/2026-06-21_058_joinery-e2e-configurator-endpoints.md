---
id: MSG-JOINERY-058
from: root
to: joinery
type: task
priority: critical
status: READ
model: sonnet
created: 2026-06-21
---

# Joinery E2E Flow — Phase 1: Backend Konfigurátor Endpoints + Database Schema

## Összefoglaló

Implementálj 3 új backend endpoint a Joinery modulban (.NET 8, C#) a product konfiguráció és work order generálás támogatásához.

## Implementációs követelmények

### 1. POST /joinery/api/products/configure
- **Input**: productType, dimensions, materials, fittings
- **Logic**: Product configuration calculations + BOM generation
- **Output**: `{ configId, previewUrl, estimatedPrice, bomPreview[] }`
- **Database**: `joinery_configurations` cache table

### 2. POST /joinery/api/work-orders
- **Input**: configId, quantity, deliveryDate, customerRef, notes
- **Logic**: Work order generation + scheduling
- **Output**: `{ workOrderId, pdfUrl, bomItems[], totalCost, scheduledStart }`
- **Database**: `work_orders` table

### 3. GET /joinery/api/work-orders/:id/sheet.pdf
- **Output**: PDF binary (work order sheet)
- **Content-Type**: application/pdf

## Database Schema

Új táblák a `spaceos_joinery` schemában:

1. **joinery_configurations**
   - id (UUID, PK)
   - config_data (JSONB) — product configuration JSON
   - created_at (TIMESTAMP)

2. **work_orders**
   - id (UUID, PK)
   - config_id (UUID, FK → joinery_configurations)
   - quantity (INT)
   - delivery_date (DATE)
   - customer_ref (VARCHAR)
   - status (VARCHAR) — 'pending', 'scheduled', 'in_progress', 'completed'
   - created_at (TIMESTAMP)

3. **product_templates**
   - id (UUID, PK)
   - template_type (VARCHAR) — 'standard_door', 'double_door', 'sliding_door', 'window_door', 'custom'
   - rules (JSONB) — configuration rules and defaults

## Technikai követelmények

- .NET 8 + C# latest
- Entity Framework Core + migrations
- PostgreSQL JSONB a konfigurációs adatokhoz
- Unit + Integration tesztek (xUnit)
- **Configuration-driven logic** — NO hardcoded calculations (JSONB templates)
- Service layer: `ProductConfiguratorService`
- 5 product template seed: standard_door, double_door, sliding_door, window_door, custom

## Security & Authorization

- Minden endpoint: `[Authorize(Policy = "ManufacturerOnly")]`
- RLS FORCE a work_orders táblán (TenantId alapján)
- Input validation: max quantity = 1000

## Kötelező munkafolyamat

1. ✅ Inbox READ (status: UNREAD → READ)
2. ✅ 3 database schema + EF Core migrations
3. ✅ 3 endpoint Controller action
4. ✅ Service layer: ProductConfiguratorService
5. ✅ Seed: 5 product template (standard_door, double_door, sliding_door, window_door, custom)
6. ✅ Unit tests: ProductConfiguratorService
7. ✅ Integration tests: 3 endpoints
8. ✅ Build: `dotnet build` → 0 errors
9. ✅ Test: `dotnet test` → all passing
10. ✅ PM2 restart check
11. ✅ Curl test a 3 endpoint-on
12. ✅ Outbox DONE

## Frontend dependency

MSG-FE-087 (párhuzamos, mock-kal dolgozik majd)

## Referencia

- ADR-039: Joinery nem hív más modult közvetlenül
- ADR-024: IParametricProduct implementor
- Joinery CLAUDE.md — TenantGucKey = "app.tenant_id"

## Elvárások

- Configuration-driven approach: a product templates JSONB szabályok alapján működnek
- Clean Architecture + DDD: Entity factory methods, private setters
- Result<T> return types minden service method-on
- ConfigureAwait(false) minden async callban
- AsNoTracking() read-only lekérdezéseknél

## Becsült időigény

8-10 óra (Sonnet model)
