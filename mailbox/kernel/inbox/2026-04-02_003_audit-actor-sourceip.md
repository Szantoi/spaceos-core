---
id: MSG-K013
from: root
to: kernel
type: task-assign
priority: P2
status: UNREAD
created: 2026-04-02T12:00:00
---

## Tárgy

Audit Log — Actor (ki) és SourceIp (honnan) mezők hozzáadása

## Probléma

A jelenlegi `AuditEvent` entitás nem rögzíti, hogy **ki** és **honnan** hajtotta végre a műveletet. A SpaceOS Vision Trust & Immutability elvéhez ez alapvetően szükséges.

## Feladat

### 1. Domain — `AuditEvent.cs`

Két új mező:

| Mező | Típus | Leírás |
|---|---|---|
| `ActorId` | `string?` | A JWT `sub` claimje (username). Null ha system/internal event. |
| `SourceIp` | `string?` | A HTTP kérés IP-je (`X-Forwarded-For` vagy `RemoteIpAddress`). Null ha system event. |

A `Create()` factory metódus kap két új opcionális paramétert:
```csharp
public static AuditEvent Create(
    Guid tenantId, string eventType, Guid aggregateId,
    string payload, string stateHash,
    string? actorId = null, string? sourceIp = null)
```

### 2. `IAuditEventDispatcher` + `AuditEventDispatcher`

Az `AuditEventDispatcher` kap egy `IHttpContextAccessor` függőséget, és automatikusan kinyeri:
- `actorId` → `httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value`
- `sourceIp` → `httpContext.Connection.RemoteIpAddress?.ToString()` (figyelembe véve `X-Forwarded-For` headert ha van)

Így a domain event handlerekben nem kell manuálisan átadni — az infrastruktúra kezeli.

### 3. `AuditEventDto`

Két új mező a DTO-ban:
```csharp
public sealed record AuditEventDto(
    Guid Id,
    string EventType,
    Guid AggregateId,
    string StateHash,
    DateTimeOffset OccurredAt,
    string? ActorId,
    string? SourceIp);
```

### 4. EF Core migration

Új migration: `AddActorIdAndSourceIpToAuditEvents`
- `ActorId` — `TEXT`, nullable, max 200 karakter
- `SourceIp` — `TEXT`, nullable, max 45 karakter (IPv6 max hossz)

### 5. Audit UI frissítés (Portal mailboxon)

Erről külön üzenetet küldök a Portal terminálnak.

## Pipeline

CODE → TEST → Outbox status-update.
