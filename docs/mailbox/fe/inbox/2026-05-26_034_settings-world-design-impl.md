---
id: MSG-FE-034
from: root
to: fe
type: task
priority: high
status: UNREAD
created: 2026-05-26
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# FE-034 — Beállítások (Settings world) URL-alapú navigáció

## Kontextus

A `settings` world a `worlds.ts`-ben 10 screen-nel rendelkezik (company, users, facilities,
machines, partners, workflow, integrations, catalog, audit, roles). A WorldShell sidebar
megjeleníti ezeket a screen-linkeket, de a jelenlegi `SettingsPage.tsx` belső tab-állapotot
(`useState`) használ és nem olvassa az URL `:screen` paramétert.

Ennek következménye: a sidebar-ban kattintva a megfelelő linkre az URL megváltozik
(`/w/settings/users`), de a tartalom nem reagál rá — a `SettingsPage` az utoljára
kézzel kattintott tabon marad.

Megoldás: a többi world-höz hasonlóan `SettingsWorldPage`-t kell létrehozni.

---

## 1. `App.tsx` — `SettingsWorldPage` hozzáadása

### Jelenlegi (HIBÁS — WorldPage wrapper):
```tsx
<Route path="/w/settings" element={
  <RequireAuth>
    <WorldPage worldKey="settings"><SettingsPage /></WorldPage>
  </RequireAuth>
} />
<Route path="/w/settings/:screen" element={
  <RequireAuth>
    <WorldPage worldKey="settings"><SettingsPage /></WorldPage>
  </RequireAuth>
} />
```

### Elvárás (SettingsWorldPage — mint a többi world):
```tsx
function SettingsWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = (screen ?? 'company') as SettingsTab

  return (
    <WorldShell
      worldKey="settings"
      screen={currentScreen}
      onScreen={(key) => navigate(`/w/settings/${key}`)}
      onHome={() => navigate('/')}
    >
      <div key={currentScreen} className="contents">
        <SettingsPage
          initialTab={currentScreen}
          onTabChange={(tab) => navigate(`/w/settings/${tab}`)}
        />
      </div>
    </WorldShell>
  )
}
```

Route-ok:
```tsx
<Route path="/w/settings" element={<RequireAuth><SettingsWorldPage /></RequireAuth>} />
<Route path="/w/settings/:screen" element={<RequireAuth><SettingsWorldPage /></RequireAuth>} />
```

**Importok szükségesek:** `SettingsTab` típus az `App.tsx`-ben, vagy `SettingsPage`-ből re-export.
Egyszerűbb megoldás: `string` típusú `initialTab` és `onTabChange` prop.

---

## 2. `SettingsPage.tsx` — props hozzáadása

### Jelenlegi:
```tsx
export function SettingsPage() {
  const t = I18N.hu
  const [tab, setTab] = useState<SettingsTab>('company')
```

### Elvárás:
```tsx
interface SettingsPageProps {
  initialTab?: SettingsTab
  onTabChange?: (tab: SettingsTab) => void
}

export function SettingsPage({ initialTab = 'company', onTabChange }: SettingsPageProps = {}) {
  const t = I18N.hu
  const [tab, setTab] = useState<SettingsTab>(initialTab)

  function handleTabChange(newTab: SettingsTab) {
    setTab(newTab)
    onTabChange?.(newTab)
  }
```

### Tab gomb onClick frissítése:
```tsx
// VOLT:
onClick={() => setTab(tb.key)}

// LEGYEN:
onClick={() => handleTabChange(tb.key)}
```

---

## 3. Navigálható screen-ek (worlds.ts szerint)

| URL | Tab |
|-----|-----|
| `/w/settings` | `company` (default) |
| `/w/settings/company` | Cégadatok |
| `/w/settings/users` | Felhasználók |
| `/w/settings/facilities` | Részlegek |
| `/w/settings/machines` | Géppark |
| `/w/settings/partners` | Partnerek |
| `/w/settings/workflow` | Munkafolyamat |
| `/w/settings/integrations` | Integrációk |
| `/w/settings/catalog` | Katalógus |
| `/w/settings/audit` | Napló |
| `/w/settings/roles` | Jogosultságok |

---

## Definition of Done

- [ ] `SettingsWorldPage` létrehozva App.tsx-ben
- [ ] `WorldPage` wrapper törlve a settings route-okból
- [ ] `SettingsPage` fogad `initialTab` és `onTabChange` props-t
- [ ] Tab gomb kattintásra URL is változik (`/w/settings/:screen`)
- [ ] Sidebar tab-linkek (WorldShell) és belső tab-ok szinkronban vannak
- [ ] `key={currentScreen}` wrapper a force remounthoz
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind pass
- [ ] `pnpm lint` → 0 új hiba

## Fájlok érintve

- `src/App.tsx` — SettingsWorldPage + route frissítés
- `src/pages/SettingsPage.tsx` — initialTab + onTabChange props
