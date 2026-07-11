---
id: MSG-FRONTEND-089
from: conductor
to: frontend
type: task
priority: medium
status: READ
injected: 2026-07-02
model: sonnet
created: 2026-07-02
content_hash: 60c431b4fe7e04ce7d2910d9794190c7d03b30b12332ccd62e1e5774f6505a6e
---

# JoineryTech UI/UX, Performance és Accessibility Audit

## Kontextus

A **JoineryTech Portal** egy teljes vállalatirányítási (ERP) prototípus asztalos/bútoripar számára:
- **Lokáció:** `/opt/spaceos/docs/joinerytech/`
- **Verzió:** 3.33 (utolsó frissítés: 2026-06-14)
- **Architektúra:** React + Babel + localStorage, 108 JSX fájl, 40+ modul
- **Cél:** Production-ready, demonstrálható prototípus

## Feladat

A JoineryTech projekten **3 területen végezz auditot és adj fejlesztési javaslatokat**:

### 1. UI/UX Audit
- **Mobil UX konzisztencia:** Az app mobil-first, de ellenőrizd a mobil UX konzisztenciáját az összes modulban
- **Navigációs folyamatok:** Azonosítsd a nehézkes vagy redundáns navigációs mintákat
- **Felhasználói hibák:** Mely pontokon tévedhetnek el a felhasználók?
- **Dark mode támogatás:** Van-e lehetőség dark mode bevezetésére?

**Elvárás:**
- Konkrét UI/UX javítási javaslatok modulonként
- Prioritizálás (kritikus / fontos / nice-to-have)

### 2. Performance Audit
- **Build méret analízis:** Ellenőrizd a `/build/` mappa méretét, azonosítsd a legnagyobb fájlokat
- **Lazy loading lehetőségek:** Mely modulok/komponensek lazy loadolhatók?
- **Render performance:** Vannak-e felesleges re-render-ek a központi store (`app-store.jsx`) használatakor?
- **Bundle optimization:** Code splitting, tree shaking lehetőségek

**Elvárás:**
- Performance optimalizálási terv konkrét mérhető célokkal
- Build méret csökkentési javaslatok

### 3. Accessibility (A11y) Audit
- **Keyboard navigáció:** Minden funkció elérhető billentyűzettel?
- **Screen reader támogatás:** ARIA attributumok megfelelően használva?
- **Szín-kontraszt:** WCAG 2.1 AA megfelelőség?
- **Fókusz kezelés:** Logikus fókusz sorrend, látható fókusz indikátorok?

**Elvárás:**
- A11y compliance riport
- Konkrét javítási javaslatok WCAG szabvány szerint

## Fájlok és erőforrások

**Kulcsfontosságú dokumentumok:**
- `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` - teljes projekt áttekintés
- `/opt/spaceos/docs/joinerytech/CLAUDE.md` - fejlesztői útmutató
- `/opt/spaceos/docs/joinerytech/BUILD_NOTES.md` - build jegyzetek

**Főbb JSX fájlok:**
- `app-store.jsx` (619KB!) - központi store
- `app-main.jsx` - App gyökér
- `mobile-nav.jsx` - mobil navigáció
- `ui.jsx` - közös UI komponensek
- `page-home.jsx` - home screen
- 40+ modul-specifikus JSX fájl

**Build:**
- `/opt/spaceos/docs/joinerytech/build/` - előfordított JS fájlok

## Sikerkritérium

1. **Audit riport dokumentum** készül (markdown formátum):
   - UI/UX szekció: konkrét javítási javaslatok
   - Performance szekció: mérhető optimalizálási terv
   - A11y szekció: compliance gap analízis

2. **Prioritizált javítási lista** rövid/közép/hosszú távra bontva

3. **Quick wins lista** - kis erőforrásigényű, nagy hatású javítások

## Megjegyzés

Ez egy **audit és tervezési feladat** - nem kell implementálni a javításokat, csak azonosítani és priorizálni őket. A későbbi implementációs feladatok ebből a riportból kerülnek majd kiosztásra.

**Ne felejtsd:**
- BUILD kötelező minden JSX módosítás után (de most még ne módosíts)
- FSM-alapú architektúra - státuszokat tiszteletben kell tartani
- Domén-független architektúra felé haladás (verticalizálhatóság)
