# Sub-skill: Senior Frontend Review

Alkalmazd ezt az analitikai keretrendszert a frontend tervdokumentumra.
Minden finding-et `FE-NN` prefixszel dokumentálj.

---

## Review checklist — 28 pont

### Route & Navigation (FE-01..06)

1. **Route flat vs. nested** — a route struktúra flat (`/:world/:screen`) vagy nested? Nested jobb a code splitting-hez, de flat egyszerűbb. Indokold a választást.
2. **404 / unauthorized route** — mi történik ha a user nem-engedélyezett world-be navigál URL-ben? Redirect Home? 403 page?
3. **Deep linking** — a teljes állapot visszaállítható-e URL-ből? (pl. `/production/cutting?plan=CP-184-A`)
4. **Browser history** — a world→screen navigáció `push` vagy `replace`? A Back gomb hova visz?
5. **Route guard** — az `enabledModules` ellenőrzés route-szinten történik (guard), vagy komponens-szinten (conditional render)?
6. **Lazy boundaries** — minden `React.lazy()` hívásnak van `<Suspense fallback>` + `<ErrorBoundary>`?

### Component Architecture (FE-07..14)

7. **Shared vs. app-specific** — minden új komponens indokoltan van elhelyezve (`@spaceos/ui` vs. app-local)?
8. **Prop drilling** — van-e 3+ szintű prop drilling? Ha igen → context vagy Zustand javasolt.
9. **Controlled vs. uncontrolled** — form elemek konzisztensen controlled VAGY uncontrolled?
10. **Key stability** — lista renderelésben stabil key-ek vannak (nem index)?
11. **Memoization** — drága renderelések `React.memo` / `useMemo` / `useCallback`-kal védve?
12. **Component size** — van-e 300+ soros komponens? Ha igen → bontsd szét.
13. **Event handler naming** — konzisztens `onX` / `handleX` konvenció?
14. **Error boundaries** — minden world-nek van saját ErrorBoundary-je?

### State Management (FE-15..19)

15. **Server state vs. client state** — TanStack Query kizárólag server state-re, Zustand kizárólag client state-re?
16. **Query key convention** — konzisztens query key hierarchia (pl. `['cutting', 'plans', planId]`)?
17. **Stale time** — a query `staleTime` megfelelő? Dashboard-ok: 30s, listák: 60s, konfig: 5min.
18. **Optimistic updates** — mutáció-utáni UI frissítés: optimistic VAGY invalidation? Indokold.
19. **Zustand persist** — SEMMILYEN auth token / sensitive data NEM kerül `persist` middleware-be.

### Performance (FE-20..24)

20. **Bundle splitting** — world-enként külön chunk? Mérethatár: < 80KB gzip / world.
21. **Image optimization** — SVG inline vs. sprite? PNG/JPG lazy-loaded?
22. **Font loading** — `font-display: swap`? Preconnect a Google Fonts-ra?
23. **Re-render audit** — a Home screen nem renderelődik újra world navigációnál.
24. **Virtualization** — 100+ soros lista van? Ha igen → `react-window` / `react-virtuoso`.

### Accessibility & Responsive (FE-25..28)

25. **Semantic HTML** — `<nav>`, `<main>`, `<aside>`, `<header>`, `<footer>` megfelelően használva?
26. **Keyboard navigation** — Tab sorrend logikus? Focus trap modal-okban?
27. **ARIA labels** — icon-only gombok `aria-label`-lel?
28. **Touch targets** — minden interaktív elem min 44×44px (WCAG) / 48×48px (Material)?

---

## Finding súlyok — frontend specifikus

| Súly | Mikor |
|------|-------|
| 🔴 CRITICAL | Auth token leak, XSS vulnerability, enabledModules bypass |
| 🟠 HIGH | Prop drilling 4+ szint, missing ErrorBoundary, bundle > 200KB, missing route guard |
| 🟡 MEDIUM | Missing memoization, inconsistent query keys, missing ARIA labels |
| 🟢 LOW | Naming convention, unnecessary re-render, cosmetic |
