---
id: MSG-KERNEL-105
from: root
to: kernel
type: task
priority: critical
status: READ
ref: SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md
created: 2026-04-28
---

# KERNEL-105 — Outbox cross-module HTTP fan-out + module_subscriptions (Manufacturing prereq)

> **Manufacturing Phase 1 BLOCKER** — az inbox subscription + cross-module event dispatch hiányzik.
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Effort:** ~2.5 nap
> **Használhatsz sub-agent-eket**

---

## 1. module_subscriptions tábla

```sql
CREATE TABLE "ModuleSubscriptions" (
    "Id"             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "SubscriberModule" varchar(100) NOT NULL,  -- pl. "Manufacturing"
    "EventType"       varchar(200) NOT NULL,  -- pl. "CuttingPanelCompleted"
    "InboxEndpoint"   varchar(500) NOT NULL,  -- pl. "https://127.0.0.1:5007/internal/inbox/cutting"
    "IsActive"        boolean NOT NULL DEFAULT true,
    "CreatedAt"       timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX "IX_ModuleSubscriptions_Sub_Event" ON "ModuleSubscriptions" ("SubscriberModule", "EventType");
```

EF migration: `dotnet ef migrations add ModuleSubscriptions`

## 2. OutboxBackgroundWorker bővítés

A jelenlegi `ProcessBatchAsync`:
```
outbox message → ISignalROutboxFanOut → IHashChainOutboxSink
```

Bővíteni:
```
outbox message → ISignalROutboxFanOut → IHashChainOutboxSink → ICrossModuleOutboxDispatcher
```

### ICrossModuleOutboxDispatcher

```csharp
public interface ICrossModuleOutboxDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
```

Implementáció:
1. `ModuleSubscriptions` lookup by `EventType`
2. Aktív subscriber-ek → HTTP POST az `InboxEndpoint`-ra
3. mTLS (IHttpClientFactory named "cross-module")
4. HMAC body signature (X-SpaceOS-Internal + X-SpaceOS-Hmac)
5. Retry: 3x exponential (1s/2s/4s)
6. Ha hiba: `OutboxMessage.MarkFailed()` (nem veszít el eventet)

## 3. Seed: CuttingPanelCompleted → Manufacturing

```csharp
// Seed migration vagy application startup:
// ModuleSubscription { SubscriberModule = "Manufacturing", EventType = "CuttingPanelCompleted", InboxEndpoint = "http://127.0.0.1:5007/internal/inbox/cutting" }
```

## Tesztek (+12)

1. ModuleSubscription CRUD
2. CrossModuleOutboxDispatcher: happy path
3. CrossModuleOutboxDispatcher: no subscribers → skip
4. CrossModuleOutboxDispatcher: subscriber HTTP 500 → retry + MarkFailed
5. CrossModuleOutboxDispatcher: mTLS header present
6. CrossModuleOutboxDispatcher: HMAC signature valid
7. Migration: ModuleSubscriptions tábla létezik
8. Integration: outbox event → subscriber endpoint called
9-12. Edge cases: inactive subscriber, duplicate EventType, empty endpoint

## Definition of Done

- [ ] ModuleSubscriptions tábla + migration
- [ ] ICrossModuleOutboxDispatcher + implementáció
- [ ] mTLS + HMAC header
- [ ] OutboxBackgroundWorker bővítve
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 1173 pass
- [ ] Outbox DONE
