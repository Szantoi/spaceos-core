---
name: D4 — new aggregates missing domain events
description: CODE agent introduces new AggregateRoot subclasses without any AddDomainEvent calls in Create() or mutation methods.
type: feedback
---

Rule D4 violated on every new aggregate in MSG-K020/K021 (NodeManifest, SyncSignal) and MSG-K054 (StageChainTemplate.AddStep/RemoveStep).

In K020/K021: `Create()` and every mutating method (`UpdateHeartbeat`, `MarkSynced`) had zero `AddDomainEvent(...)` calls.

In K054: `StageChainTemplate.AddStep()` and `RemoveStep()` mutate state (`_steps`, `UpdatedAt`) without raising domain events. The `Create()` factory correctly raises `StageChainCreatedEvent`, but subsequent step mutations are silent.

**Why recurring:** CODE agent covers `Create()` factory events (first-time reminder works) but omits events from *subsequent* mutation methods on aggregates, especially collection-mutation methods (`AddStep`, `RemoveStep`, `AddItem`, `RemoveItem`).

**Standard fix:** For every new `AggregateRoot` subclass, immediately verify:
1. `Create()` raises an `<Entity>CreatedEvent`.
2. Every method that modifies a property OR collection raises an `<Entity><Mutation>Event`.
Events are `readonly record struct : IDomainEvent`. Flag as UNFIXABLE — requires developer decision on handler wiring.

**Specific pattern to watch:** Methods named `Add<X>` / `Remove<X>` on aggregates are the most commonly missed.
