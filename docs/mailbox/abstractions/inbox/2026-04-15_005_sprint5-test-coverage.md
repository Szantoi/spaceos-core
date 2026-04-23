---
id: MSG-ABSTRACTIONS-005
from: root
to: abstractions
type: task
priority: high
status: READ
ref: R-18
created: 2026-04-15
---

# MSG-ABSTRACTIONS-005 — Sprint 5: Test Coverage — Graph Engine ciklus detekció

## Háttér

Devils-advocate audit (2026-04-15) egy kritikus algoritmikus gap-et azonosított (R-18):

**Kérdés:** Az `AddEdge()` futtat-e DFS ciklus-ellenőrzést az INSERT **előtt**, vagy csak `DeriveManufacturing()` traversal-kor derül ki a ciklus?

Ha csak traversal-time:
- Érvénytelen gráf kerülhet a DB-be (audit immutability miatt ki sem javítható UPDATE-tel)
- Diamond dependency: A→B, A→C, B→D, C→D → D kétszer deriválódik → helytelen gyártási mennyiség
- A hiba minden downstream modult (Joinery, Cutting) és a NuGet Contracts 1.0.0 fogyasztóit érinti

## Feladat

### 1. Diagnózis

Nézd meg az `AddEdge()` implementációt:
- Van-e DFS pre-condition check az INSERT előtt?
- Dokumentáld a választ az outbox-ban

### 2. Ha csak traversal-time — fix szükséges

Adj hozzá write-time ciklus-ellenőrzést: DFS a célcsúcstól a forráscsúcs felé, INSERT előtt.

### 3. Tesztek (mindenképpen, fix-től függetlenül)

```csharp
// 1. Önreferenciális ciklus
AddEdge_WhenCreatesSelfLoop_RejectsAtWriteTime()

// 2. Write-time ciklus
AddEdge_WhenCreatesCycle_RejectsAtWriteTime()

// 3. Diamond dependency — nem dupla számítás
DeriveManufacturing_WithDiamondDependency_DerivesOnce()

// 4. Nagy gráf — nincs stack overflow
DeriveManufacturing_With100Nodes_CompletesWithinTimeout()
```

### 4. RLS negatív teszt

```csharp
// Tenant A gráfja nem elérhető Tenant B session-ből
GraphEngine_CrossTenant_ReturnsEmpty()
```

## DoD

- [ ] `AddEdge()` ciklus-detekció stratégiája dokumentálva (write-time vagy traversal-time)
- [ ] Ha szükséges: write-time DFS check implementálva
- [ ] 4 új teszteset zöld
- [ ] RLS cross-tenant negatív teszt zöld
- [ ] Tesztszám ≥ 61
- [ ] DONE outbox: diagnózis eredménye + új tesztszám

