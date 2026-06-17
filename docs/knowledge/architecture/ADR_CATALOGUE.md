# SpaceOS — Architekturális döntések katalógusa (ADR)

> Kritikus arch döntések, indoklásuk és alkalmazásuk a SpaceOS fejlesztéshez.

---

## ADR-001: Security Sprint — JWT RS256 + Azure Key Vault

**Döntés:** 2026-04-03 APPROVED

| Item | Decision |
|---|---|
| JWT algoritmus | **RS256** (asymmetric) |
| Private key tár | **Azure Key Vault** (prod); PEM file dev-ben (`.gitignore`-d) |
| Hash chain serializáció | **PostgreSQL advisory lock** (`pg_try_advisory_xact_lock` per TenantId) |
| Audit sink | **Azure Immutable Blob Storage** (WORM, prod); file-based dev |

**Miért:**
- Production Escrow & security audit findings blokkolta a deployment-et
- RS256 aszimmetrikus kulcs → nincs shared secret key management kockázat
- Azure Key Vault → hardware security module + rotation policy
- Advisory lock → distributed transaction-like capability PostgreSQL-ben

**Alkalmazás:**
1. Sprint 0 (P0-3 → P0-1 → P0-4) előfeltétele az 1. deploynak
2. Sprint 1 előfeltétele: `Azure.Security.KeyVault.Keys`, `Azure.Identity` NuGet packages
3. Audit sink: `Azure.Storage.Blobs` (Sprint 1)
4. Dev setup: `.env` fájl PEM key path, dev Azure storage account (vagy local Azurite)

---

## ADR-002: Modular Monolith — Kernel IParametricProduct interface

**Döntés:** APPROVED (alaparch)

Kernel **nem tudja**, hogy mi az asztalos (Joinery), lapszabász (Cutting), stb.
Kernel csak az `IParametricProduct` interface-t ismeri.

**Interfész:**
```csharp
public interface IParametricProduct
{
    Guid ProductId { get; }
    Guid TenantId { get; }
    Dictionary<string, object> Parameters { get; }
    
    // Kernel callback-ek
    Task<GeometryResult> GenerateGeometry(IGeometryEngine engine);
    Task<ValidationResult> ValidateParameters();
}
```

**Miért:**
- Driver modulok (Joinery, Cutting) implementálják
- Kernel: escrow, audit, FSM, auth — nincs business logic
- Domain logic: Driver layer-ben (SpaceOS.Modules.Joinery, stb.)

**Alkalmazás:**
- Új driver hozzáadásakor: `IParametricProduct` implementálás + DI registration
- Kernel-ben: nincs `if (product is Joinery)` — mindig `product.GenerateGeometry()`

---

## ADR-003: Immutability & Audit Trail — SHA-256 hashed events

**Döntés:** APPROVED (L1 Kernel)

Audit table minden DML event-hez:
```sql
CREATE TABLE "AuditEvents" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EntityType" text NOT NULL,
    "EntityId" uuid NOT NULL,
    "Operation" text NOT NULL, -- INSERT, UPDATE, DELETE
    "ChangedBy" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "ChangedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "BeforeData" jsonb,
    "AfterData" jsonb,
    "DataHash" text NOT NULL, -- SHA256(BeforeData||AfterData)
    CONSTRAINT fk_tenant FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id")
);
```

CAD adat (geometry, parameters) **sosem** UPDATE-elhető — csak INSERT + archive.

**Miért:**
- Regulatory compliance (audit trail)
- Dispute resolution (ki/mikor módosított)
- No UPDATE CAD = no implicit versioning bugs

**Alkalmazás:**
- Kernel interceptor: EVERY DML → AuditEvents
- Driver modules: custom audit fields (pl. Joinery: "material_change_reason")

---

## ADR-004: Role-Based Access Control (RBAC) — Need-to-Know

**Döntés:** APPROVED

Keycloak roles:
```
tenant_admin     → everything
designer         → csak saját Order designs
factory_worker   → assigned Orders + Materials
sales_person     → assigned Quotes + Orders
supplier         → csak saját Inventory items (view)
```

**RLS Policy:** Minden SELECT / UPDATE / DELETE

```sql
CREATE POLICY "tenant_isolation" ON "Orders"
  USING ("TenantId" = current_setting('app.current_tenant')::uuid);

CREATE POLICY "designer_only_own_orders" ON "Orders"
  USING (
    current_setting('app.user_role') = 'tenant_admin'
    OR "DesignedBy" = current_setting('app.user_id')::uuid
  );
```

**Miért:**
- Szállító nem látja a gyártó anyaglistáját
- Tervező nem módosíthat már approved Quote-ot
- GDPR compliance

**Alkalmazás:**
- Login után: Keycloak role → `app.user_role` setting
- Minden query: implicit RLS filter
- Orchestrator BFF: role ellenőrzés (double-defense)

---

## ADR-005: Walking Skeleton First — E2E pipeline előbb, matematika utóbb

**Döntés:** APPROVED (Development strategy)

Prioritás sorrend:
1. **L1 + L3 + L4 (Kernel + Orch + Portal):** Auth, data, UI — **WORKING END-TO-END**
2. **L2 (Joinery driver):** CAD geometry — **SIMPLISTIC first** (rectangle), iterálva komplexebb
3. Cutting, Inventory, stb. — **párhuzamos**, önálló domain

**Miért:**
- Korai feedback: user tesz le Joinery rendelést, amit lát is a Portalon
- Arch bugs hamar látszanak (RLS, audit, workflow FSM)
- Matematika iterálódik (hozza az product owner újabb igényt)

**Alkalmazás:**
- Joinery v1: sima méretes asztalos (width/height/depth) → PDF nyomtatás
- v2: panel layout optimization (ONNX model később)
- v3: edge banding, hardware cost calc
- Test-driven: E2E happy path, majd edge cases

---

## ADR-006: Data → Rules → Geometry

**Döntés:** APPROVED (Design principle)

Responsibility separation:

| Layer | Mi csinál |
|---|---|
| **Frontend (Portal)** | Adatelemek renderelés (sliders, form inputs), UI state |
| **C# Driver** | Adatutalansági szabályok (pl. max width = 2500mm), számítások |
| **LLM (Orch)** | Paraméterek javaslatása (Tool Calling), nem geometry |
| **CAD Engine** | Geometry generálás (3D koordináták, SVG/STEP) |

❌ **Tilos:** Frontend direkt CAD számítást végez, LLM szabályokat írva ad vissza.

**Miért:**
- Konzisztens szerkesztés (backend validáció)
- Kliensünk = más kliensek (nem HTML/JS szab-e meg a physics-t)

**Alkalmazás:**
- Joinery Order POST: `{ Width: 1200, Height: 800 }` → Kernel drivernek
- Driver: `if (Width > MaxWidth) throw ValidationException`
- Frontend: error megjelenítése, retry

---

## Referencia

- Teljes vision: `docs/vision/SpaceOS_Vision_Master.md`
- Security sprint: `docs/mailbox/kernel/outbox/` 2026-04-03 DONE messages
- Module interfaces: `docs/knowledge/architecture/ECOSYSTEM_MODULE_ARCHITECTURE.md`
