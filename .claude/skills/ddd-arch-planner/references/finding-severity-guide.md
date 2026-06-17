# Finding Severity Guide

## Mikor CRITICAL 🔴

Implementáció blokkoló — a rendszer **hamis biztonságérzetet nyújt**, vagy **adatvesztés / data breach** közvetlen kockázatával jár.

| Kategória | Példa |
|-----------|-------|
| Trust boundary szivárgás | Hash chain és adat ugyanabban a DB-ben — újraszámítható |
| Cross-tenant adathozzáférés | RLS hiány vagy FORCE RLS hiány |
| Credential plaintext tárolás | Token/kulcs az app DB-ben, nem Key Vault-ban |
| DI container anti-pattern | `BuildServiceProvider()` — memory leak, Singleton/Scoped conflict |
| Cartesian product SQL | `FULL OUTER JOIN ON true` — O(n²), hibás COUNT |
| Race condition | `GetLastHash` + parallel write = chain fork |
| Auth bypass | TenantId HTTP headerből (nem JWT claim-ből) |

**Következmény ha nem javítják:** DoD blocker — nem mehet production-ra.

---

## Mikor HIGH 🟠

Strukturális hiba — nem azonnali exploit, de **architektúrális adósságot** képez, vagy **security control hatékonyságát rontja**.

| Kategória | Példa |
|-----------|-------|
| Unobserved Task | `_ = task` — audit event csendesen elvész |
| Partial error handling | Csak 401 kezelve, 429/503/timeout → raw 500 |
| Missing input validation | Kernel perimeter határán nincs schema guard |
| Aggregate boundary szivárgás | `_navigation` property = lazy load trap |
| Missing endpoint spec | CQRS vertikum fele hiányzik a tervből |
| Timing attack | Token comparison nem `FixedTimeEquals` |
| SSE resource leak | Client disconnect → generator fut tovább (billing) |

**Következmény ha nem javítják:** Tech debt — következő fázisban kötelező lezárni.

---

## Mikor MEDIUM 🟡

Jó practice hiány — **nem blokkoló**, de karbantarthatóságot, megfigyelhetőséget, vagy teljesítményt ront.

| Kategória | Példa |
|-----------|-------|
| Dokumentálatlan korlát | In-memory RL backing store — multi-instance esetén nem működik |
| Missing pagináció | Unbounded list query |
| Non-standard tooling | `dotnet script` helyett standalone console project |
| Missing ConfigureAwait | Async method production kódban |
| Missing partial index | Szűrt query-re nincs partial index → Seq Scan |
| Hardcoded config | `ValidateOnStart()` hiány — runtime fail helyett startup fail |

**Következmény ha nem javítják:** Akkumulálódó tech debt — sprint review-n jelezni kell.

---

## Effort heurisztika

| Finding súly | Tipikus javítási effort |
|-------------|------------------------|
| 🔴 CRITICAL | 0.5 – 2 nap |
| 🟠 HIGH | 0.5 – 1 nap |
| 🟡 MEDIUM | 0.25 – 0.5 nap |

Ha egy fázisban >3 CRITICAL finding van → +1 nap buffer az összesített effort becslésen.
