# SpaceOS.DesignPortal — Tech Debt Log

**Létrehozva:** 2026-03-29
**Projekt:** `spaceos-design-portal`

---

## TD-001 — Hiányzó REVIEW_REPORT.md-k (E18–E27)

**Prioritás:** 🟡 P2
**Státusz:** `OPEN`
**Érintett epikek:** E18, E19, E20, E21, E22, E23, E24, E25, E26, E27

### Leírás

Az E18–E27 epikek a CODE → TEST fázis után közvetlenül `CLOSED_DONE` státuszba kerültek,
a WORKFLOW.md-ben definiált **REVIEW fázis kihagyásával**.

Minden epic `tasks/` mappájából hiányzik a `REVIEW_REPORT.md`.

### Kockázat

A REVIEW fázis feladata lett volna:
- REVIEW_CHECKLIST.md minden pontjának ellenőrzése
- Esetleges violations javítása a forráskódban
- Build + test zöld utólagos megerősítése

A build jelenleg zöld (0 hiba) és 174 teszt átmegy, de a checklist tételei
(pl. direkt axios hívások, `any` típusok, hiányzó test fájlok) **nem lettek szisztematikusan ellenőrizve**.

### Elfogadott kockázat

A REVIEW_REPORT-ok visszamenőleges elkészítése helyett a hiányt dokumentáljuk
és a következő fejlesztési ciklusban (E28+, vagy Layer 2 Driver fejlesztés előtt)
futtatjuk le a retroaktív review-t egy dedikált tech debt sprint keretében.

### Javasolt jövőbeli akció

```
[Tech Debt Sprint — before Layer 2 / before production deploy]
Claude Code: @se-security-reviewer
Task: Run REVIEW_CHECKLIST.md against all src/ files
Output: REVIEW_REPORT.md per epic + fix violations in place
```

---

## TD-002 — Hiányzó Security Review (E18–E27)

**Prioritás:** 🟡 P2
**Státusz:** `OPEN`
**Érintett területek:** Frontend security (OWASP Top 10 — client side)

### Ellenőrzendő pontok (REVIEW_CHECKLIST.md alapján)

| ID | Kockázat | Várható állapot |
|----|----------|-----------------|
| A1 | Direkt axios hívás komponensben | Valószínűleg tiszta (scaffold tiltja) |
| S1 | `useEffect` + fetch anti-pattern | Valószínűleg tiszta (TanStack Query használt) |
| C4 | Uncontrolled form (react-hook-form nélkül) | Ellenőrzendő LoginPage + formok |
| R1 | Minden route `ProtectedRoute`-on belül | Ellenőrzendő router/index.tsx |
| T1 | `any` típus | Ellenőrzendő — TypeScript strict mode be van kapcsolva |
| G5 | Minden Page-hez létezik `*.test.tsx` | Ellenőrzendő — csak FsmBadge.test.tsx scaffold |

### Elfogadott kockázat

A projekt jelenleg fejlesztési fázisban van, nem production-ready deployment.
A security review production deploy előtt kötelező.

---

## TD-003 — Hiányzó Page-szintű tesztek (E19–E27)

**Prioritás:** 🟢 P3
**Státusz:** `OPEN`

### Leírás

A scaffold csak `FsmBadge.test.tsx`-et tartalmaz.
Az E19–E27 által létrehozott Page komponensekhez (`TenantsPage`, `FacilitiesPage`,
`FlowEpicKanban`, `ChatPage`, stb.) nem készültek companion `*.test.tsx` fájlok.

A CLAUDE.md golden rule: *"Every new Page → companion `*.test.tsx`, no exceptions."*

### Elfogadott kockázat

A 174 passing test valószínűleg unit-szintű teszteket tartalmaz.
Integrációs / render-szintű page tesztek hiányoznak.

### Javasolt jövőbeli akció

```
[Tech Debt Sprint]
@javascript-typescript-jest skill
Task: Page-level render tests for E19–E27 feature pages
Priority: ChatPage, FlowEpicKanban, AuditPage (legkomplexebb komponensek)
```

---

## Összefoglaló táblázat

| ID | Leírás | Prioritás | Státusz | Mikor kezelendő |
|----|--------|-----------|---------|-----------------|
| TD-001 | Hiányzó REVIEW_REPORT.md-k | 🟡 P2 | OPEN | Layer 2 előtt |
| TD-002 | Security review nem futott | 🟡 P2 | OPEN | Production deploy előtt |
| TD-003 | Hiányzó Page-szintű tesztek | 🟢 P3 | OPEN | Tech debt sprint |

---

## Hogyan zárd le a tech debt tételeket

```markdown
## TD-001 — Lezárva
**Dátum:** YYYY-MM-DD
**Akció:** REVIEW agent lefutttatva E18–E27-re, violations javítva
**Eredmény:** REVIEW_REPORT.md minden epic tasks/ mappájában
```
