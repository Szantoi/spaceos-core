---
id: MSG-FREETIER-FE-002
from: root
to: freetier-fe
type: task
priority: high
status: READ
ref: MSG-FREETIER-FE-001-DONE
created: 2026-04-24
---

# FREETIER-FE-002 — Workspace CRUD + Share + Deploy prep + Tesztek (Phase 1, Nap 9–16)

> **Tervdok:** `docs/architecture/SpaceOS_FreeTier_Portal_Architecture_v1.md` — Section 14 ütemterv Nap 9–16
> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Előzmény:** FREETIER-FE-001 ✅ (34 teszt, 13 route, scaffold+landing+kalkulátor+SVG+auth kész)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 9–10 — Workspace CRUD funkciók

A workspace oldalak már léteznek (WorkspaceListPage, WorkspaceDetailPage), de a tényleges API integráció és interaktivitás még hiányzik.

### React Query hooks

```typescript
// src/hooks/useWorkspaces.ts — ha nincs még:
export function useWorkspaces() {
  return useQuery({ queryKey: ['workspaces'], queryFn: workspacesApi.list });
}

export function useWorkspace(id: string) {
  return useQuery({ queryKey: ['workspace', id], queryFn: () => workspacesApi.get(id) });
}

export function useRevisions(workspaceId: string) {
  return useQuery({ queryKey: ['revisions', workspaceId], queryFn: () => workspacesApi.revisions(workspaceId) });
}

export function useSaveWorkspace() {
  return useMutation({ mutationFn: workspacesApi.save, onSuccess: () => queryClient.invalidateQueries(['workspaces']) });
}
```

### Funkciók

1. **WorkspaceListPage:** Grid card nézet, "Új munkatér" gomb, üres állapot
2. **WorkspaceDetailPage:** Revision history lista, aktuális eredmény SVG, share manager, "Tovább szerkesztés" → /kalkulator (betölti a workspace inputját)
3. **"Mentsd el" flow:** Kalkulátor eredmény oldalról → ha auth → workspace save → redirect /munkaterletek/:id
4. **RevisionList component:** dátum + yield% + "Betöltés" gomb

**Tesztek (+8):**
- useWorkspaces hook: lista renderel
- useSaveWorkspace: mutation hívás
- WorkspaceListPage: üres állapot, card renderelés
- WorkspaceDetailPage: revision lista, SVG megjelenítés
- "Mentsd el" flow: auth guard, redirect

---

## Nap 11 — Share flow

1. **ShareManager component:** "Link generálás" gomb → `POST /workspaces/{id}/share` → URL megjelenítés + másolás (clipboard API)
2. **Share link visszavonás:** "Visszavonás" gomb → `DELETE /workspaces/{id}/share/{shareId}`
3. **SharedViewPage finomítás:** SVG + stat cards + "Próbáld ki te is!" CTA → / (landing)

**Tesztek (+4):**
- ShareManager: link generálás, clipboard copy
- Share revoke: gomb klikk, API hívás
- SharedViewPage: anonymous render, CTA megjelenik

---

## Nap 12 — Upgrade form finomítás + responsive pass

1. **UpgradePage:** form validáció (zod), submission → 201, redirect /upgrade/koszonjuk
2. **Responsive audit:** minden oldal 375px-en tesztelve
3. **ErrorBoundary:** global error handler, user-friendly hibaüzenet
4. **Loading states:** skeleton/spinner minden async oldalon

**Tesztek (+3):**
- UpgradePage: form submit, validáció, redirect
- ErrorBoundary: error catch, render fallback
- Responsive: mobile viewport teszt

---

## Nap 13 — Deploy prep

1. **`.env.production` ellenőrzés:**
```
VITE_API_BASE_URL=https://freetier.joinerytech.hu
```

2. **`npm run build`** → production dist

3. **Nginx vhost** (`eszkozok.joinerytech.hu`):
   - A tervdok Section 12.2 tartalmazza a vhost config-ot
   - Root: `/opt/spaceos/spaceos-freetier-portal/dist`
   - SPA fallback: `try_files $uri $uri/ /index.html`
   - Proxy: NEM kell (API cross-origin, CORS konfigurálva)

**Megjegyzés:** A DNS és nginx vhost beállítás INFRA feladat lesz — te csak a dist-et készítsd elő és jelezd a DONE-ban hogy INFRA deploy task szükséges.

---

## Nap 14–16 — Tesztek bővítés

**Cél:** ≥ 60 teszt összesen (34 meglévő + 26 új)

### Unit tesztek (+15)
- Workspace hooks és pages
- Share flow (generate, revoke, copy)
- Upgrade form
- Auth flow (magic link, verify, 401 redirect)
- ErrorBoundary
- API client interceptors

### Integration tesztek (+6, MSW)
- Kalkulátor → result → mentés flow (MSW mock)
- Magic link → verify → session → workspace lista
- Share generate → public view

### E2E smoke (Playwright, +5 teszt, OPTIONAL)
Ha van idő:
1. Landing → kalkulátor → eredmény
2. Magic link → verify → workspace
3. Share link → anonymous view
4. Upgrade form → submit
5. 404 page

**Ha Playwright E2E túl sok, jelezd a DONE-ban — a unit+integration tesztek a prioritás.**

---

## Definition of Done

- [ ] Workspace CRUD: list, detail, save, revision history
- [ ] Share: generate link, clipboard copy, revoke, public view
- [ ] Upgrade form: validáció, submit, thank you page
- [ ] Responsive: 375px audit minden oldalon
- [ ] ErrorBoundary + loading states
- [ ] `.env.production` helyes
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 55 pass (34 meglévő + min 21 új)
- [ ] `npm run lint` 0 error
- [ ] DONE-ban jelezve: INFRA deploy task szükséges (eszkozok.joinerytech.hu nginx)
- [ ] Outbox DONE üzenet küldve
