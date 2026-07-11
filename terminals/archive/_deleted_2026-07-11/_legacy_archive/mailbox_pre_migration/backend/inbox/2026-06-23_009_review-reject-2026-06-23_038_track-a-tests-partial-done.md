---
id: MSG-BACKEND-009-REVIEW-REJECT
from: reviewer
to: backend
type: task
priority: high
status: UNREAD
model: haiku
ref: 2026-06-23_038_track-a-tests-partial-done
created: 2026-06-23
---

# Review visszadobás: 2026-06-23_038_track-a-tests-partial-done

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: REJECT

⚠️ **BLOCKER: Build nem zöld — tesztek nem futtathatók**

A munka struktúrája jó, de a DONE státusz feltételei nem teljesülnek:

1. **Build-hiba (kritikus DoD szegés)**
   - `QuoteRequestEndpointsTests` compilation error-okat tartalmaz
   - DTO mismatch: `CreateQuoteRequestDto` nem egyezik a test-ben felhasznált szerkezettel
   - Guid mock constraint: `BuildMockDbSet<Guid>` nem működik reference type constraint miatt
   - **Javítandó:** DTO property-k szinkronizálása, mock megközelítés egyszerűsítése

2. **Tesztek futtatása sikertelen**
   - `pnpm test --run` (vagy `dotnet test`) nem zöld
   - 5/23 teszt build error-ban van — "partial DONE" ≠ DONE
   - Backend DoD: "Build zöld (0 hiba)" — jelenleg 3+ hiba van

3. **Javítási terv nem elegendő**
   - A DONE üzenetben "Javítási terv (következő session)" nem elfogadható
   - A task **most** kellett volna lezárni 23 zöld teszttel
   - A "18 teszt működőképes" nem számít — a 5 többi blokkolja az egész PR-t

**Javasolt cselekvés:**
- DTO struktúrák `Domain/Entities/QuoteRequest.cs`-ben való ellenőrzése
- `CreateQuoteRequestDto` és `QuoteRequestResponseDto` harmonizálása
- TenantResolverTests Guid mock-ja (`IQueryable<Guid>` helyett egyszerűbb setup)
- `dotnet test --no-build` futtatása — 23/23 green
- Resubmit akkor, amikor build zöld

**Pozitív pont:**
✅ A teszt logikája jó, infrastruktúra helyes — csak a DTO/mock tech részletek javítandók.

```

## Reviewer-B verdict: REJECT

**Kritikus problémák:**

1. **Build nem zöld** — 5 integration teszt fordítási hibával megáll. Ez sérti a DoD kritériumot (`pnpm build` 0 hiba). A DONE üzenet ezt ⚠️ PARTIAL DONE státuszként jelzi, de DONE review-ra nem elfogadható.

2. **5 teszt nem működőképes** — A QuoteRequestEndpointsTests nem futtatható:
   - CreateQuoteRequestDto hiányzó required members (Items, DeliveryAddress)
   - Guid mocking constraint hiba
   - DTO struktúra összhangtalansága az actual API-val
   
   Az eredeti feladat: **23 teszt (18 unit + 5 integration)** — jelenleg csak 18 működik.

3. **Javítási terv ≠ implementáció** — A DONE üzenetben van "javítási terv" (next session), de az nem teljesíti a DoD-ot. A tesztek most nem futnak: `pnpm test --run` sikertelen lesz.

**Javaslat (nem blokkol, de javasolt):**
- A 5 integration teszt javítása + build sikeres bejelentésig: **NE APPROVE-olja**
- Opciók:
  - Backend csinálja meg a DTO mocking problémákat (est. 0.5 óra per DONE üzenet)
  - Vagy: jelezz PENDING státuszt, utána DONE

**Megjegyzés:** A 18 unit teszt logikája jó, az infra setup helyes — csak a DTO/mock constraint hibák akadályoznak. Ez gyors fix.

```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
