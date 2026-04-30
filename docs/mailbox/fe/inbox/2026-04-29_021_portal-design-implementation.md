---
id: MSG-FE-021
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-29
---

# FE-021 — JoineryTech Portal Design Implementation (pixel-perfect)

> **Design fájlok:** `design-reference/` mappa a repo-ban
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **FONTOS:** Olvasd el ELŐSZÖR a `design-reference/README.md`-t, AZTÁN a `design-reference/chats/chat1.md`-t (user intent), VÉGÜL a `design-reference/project/JoineryTech Portal.html`-t és az összes importált fájlt!
> **Használhatsz sub-agent-eket** ha szükséges

---

## Feladat

A `design-reference/` mappában egy Claude Design handoff bundle van. Ez HTML/CSS/JS prototípus — a te feladatod **pixel-perfect** React implementáció a meglévő Doorstar Portal-ban.

### Lépések

1. **Olvasd el a chat transcript-et** (`design-reference/chats/chat1.md`) — ez tartalmazza a user intent-et
2. **Olvasd el a fő HTML-t** (`design-reference/project/JoineryTech Portal.html`) — ez az entry point
3. **Olvasd el az összes page-*.jsx és ui.jsx fájlt** — ezek a komponensek
4. **Olvasd el a data*.js fájlokat** — mock adatok
5. **Implementáld a design-t** a meglévő Portal World struktúrába (WorldShell, world routes, Zustand store-ok)

### Elvárások

- **Pixel-perfect** — szín, tipográfia, spacing, layout a prototípus szerint
- **Tailwind CSS** — a meglévő Tailwind config-ot bővítsd ha kell
- **Magyar nyelvű UI** — ahogy a prototípusban van
- **Responsive** — mobile-first
- **Meglévő komponensek reuse** — ne duplikálj ami már van (WorldShell, WorldGuard, stb.)

### NE csinálj

- NE rendereld böngészőben a prototípust — olvasd el a forráskódot
- NE másold a prototípus belső struktúráját — csak a vizuális outputot kövesd
- NE törj el meglévő teszteket

---

## Definition of Done

- [ ] Design pixel-perfect implementálva React-ben
- [ ] Magyar nyelvű UI
- [ ] Responsive (375px+)
- [ ] Meglévő tesztek nem törtek el
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 251 pass (meglévő)
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE — részletezze mit implementált
