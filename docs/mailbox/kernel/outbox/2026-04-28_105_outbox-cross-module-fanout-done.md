---
id: MSG-K105-DONE
from: kernel
to: root
type: done
status: READ
priority: critical
ref: 2026-04-28_105_outbox-cross-module-fanout
---

## Összefoglaló

KERNEL-105: Outbox cross-module HTTP fan-out + `ModuleSubscriptions` tábla implementálva.

### Új fájlok

| Fájl | Leírás |
|---|---|
| `SpaceOS.Kernel.Domain/Outbox/ICrossModuleOutboxDispatcher.cs` | Interface a cross-module HTTP dispatch-hoz |
| `SpaceOS.Kernel.Domain/Entities/ModuleSubscription.cs` | System-level entity (nem tenant-scoped) |
| `SpaceOS.Kernel.Domain/Repositories/IModuleSubscriptionRepository.cs` | Repository interface |
| `SpaceOS.Infrastructure/Data/Configurations/ModuleSubscriptionConfiguration.cs` | EF Core konfiguráció |
| `SpaceOS.Infrastructure/Data/Repositories/ModuleSubscriptionRepository.cs` | EF Core implementáció |
| `SpaceOS.Infrastructure/Outbox/CrossModuleOutboxDispatcher.cs` | HTTP POST, HMAC-SHA256, 3x retry |
| `SpaceOS.Infrastructure/Migrations/20260428152343_ModuleSubscriptions.cs` | PostgreSQL migration |
| `SpaceOS.Kernel.Tests/Entities/ModuleSubscriptionTests.cs` | 4 unit teszt |
| `SpaceOS.Kernel.Tests/Infrastructure/CrossModuleOutboxDispatcherTests.cs` | 8 unit teszt |

### Módosított fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/Data/AppDbContext.cs` | `ModuleSubscriptions` DbSet hozzáadva |
| `SpaceOS.Infrastructure/Outbox/OutboxBackgroundWorker.cs` | `ICrossModuleOutboxDispatcher` beláncazva SignalR + hashchain után |
| `SpaceOS.Infrastructure/DependencyInjection.cs` | `IModuleSubscriptionRepository`, `ICrossModuleOutboxDispatcher`, `AddHttpClient("cross-module")` |
| `SpaceOS.Kernel.Tests/Infrastructure/OutboxBackgroundWorkerTests.cs` | `ICrossModuleOutboxDispatcher` mock hozzáadva `BuildWorker()`-hez |

### Implementáció részletei

- **HMAC:** `HMACSHA256.HashData`, key: `configuration["CrossModule:HmacKey"]`, fejléc: `X-SpaceOS-Hmac`
- **Autentikáció:** `X-SpaceOS-Internal: true` fejléc minden kimenő kérésnél
- **Retry:** 3 kísérlet, exponenciális (1s → 2s → 4s), sikertelen esetén exception propagál → `MarkFailed`
- **UNIQUE index:** `IX_ModuleSubscriptions_Sub_Event` a `(SubscriberModule, EventType)` páron
- **Seed:** `CuttingPanelCompleted` → Manufacturing subscription a migration `Up()` végén

## Tesztek

**1178 / 1178 pass** (963 unit + 108 integration + 107 API · 4 skipped)

DoD: ≥ 1173 — **teljesítve** ✅

Új tesztek: **12** (4 `ModuleSubscriptionTests` + 8 `CrossModuleOutboxDispatcherTests`)

## Security review

- Input validation: `ArgumentException.ThrowIfNullOrWhiteSpace` minden `ModuleSubscription.Create` paraméteren
- HMAC body signing minden kimenő HTTP kérésnél (replay protection)
- `X-SpaceOS-Internal` fejléc — future: internal network policy kikényszeríthető
- Named `HttpClient("cross-module")` — future: mTLS tanúsítvány beállítható a DI regisztrációban
- SQL injection: EF Core paraméteres query, nincs string concat
- Sensitive data: HMAC key csak konfigurációból olvasva, nem logolva

## Kockázatok / kérdések

Nincs blokkolt pont. A migration VPS-en manuálisan alkalmazandó (PostgreSQL típusok kézzel javítva a Designer fájlban — az automatikusan generált SQLite `TEXT` típusok helyett `uuid`, `character varying(N)`, `boolean`, `timestamp with time zone` típusok vannak).

VPS apply parancs:
```bash
sudo -u postgres psql -d spaceos -c "
CREATE TABLE \"ModuleSubscriptions\" (
  \"Id\" uuid NOT NULL,
  \"SubscriberModule\" character varying(100) NOT NULL,
  \"EventType\" character varying(200) NOT NULL,
  \"InboxEndpoint\" character varying(500) NOT NULL,
  \"IsActive\" boolean NOT NULL DEFAULT true,
  \"CreatedAt\" timestamp with time zone NOT NULL,
  CONSTRAINT \"PK_ModuleSubscriptions\" PRIMARY KEY (\"Id\")
);
CREATE UNIQUE INDEX \"IX_ModuleSubscriptions_Sub_Event\" ON \"ModuleSubscriptions\" (\"SubscriberModule\", \"EventType\");
INSERT INTO \"__EFMigrationsHistory\" VALUES ('20260428152343_ModuleSubscriptions', '8.0.11');
"
```
