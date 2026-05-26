---
id: MSG-FE-034-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-034
created: 2026-05-26
---

# FE-034 DONE — Beállítások (Settings world) URL-alapú navigáció

## Státusz: KÉSZ

## Elvégzett munkák

### `src/App.tsx` — SettingsWorldPage hozzáadva
```tsx
function SettingsWorldPage() {
  const navigate = useNavigate()
  const { screen } = useParams<{ screen?: string }>()
  const currentScreen = screen ?? 'company'

  return (
    <WorldShell worldKey="settings" screen={currentScreen}
      onScreen={(key) => navigate(`/w/settings/${key}`)}
      onHome={() => navigate('/')}>
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
- Settings route-ok: `WorldPage` wrapper → `SettingsWorldPage`
- `key={currentScreen}` wrapper a force remounthoz

### `src/pages/SettingsPage.tsx` — props hozzáadva
- `SettingsPageProps` interface: `initialTab?: string`, `onTabChange?: (tab: string) => void`
- `useState` inicializálva `initialTab`-ból (cast-tal `SettingsTab`-ra)
- `handleTabChange` függvény: `setTab` + `onTabChange?.()` meghívása
- Tab `onClick`: `setTab` → `handleTabChange`

## Navigálható screen-ek
| URL | Tab |
|-----|-----|
| `/w/settings` | `company` (default) |
| `/w/settings/company` | Cégadatok |
| `/w/settings/users` | Felhasználók |
| `/w/settings/facilities` | Telephely |
| `/w/settings/machines` | Géppark |
| `/w/settings/partners` | Partnerek |
| `/w/settings/workflow` | Munkafolyamat |
| `/w/settings/integrations` | Integrációk |
| `/w/settings/catalog` | Katalógus |
| `/w/settings/audit` | Audit napló |
| `/w/settings/roles` | Jogosultságok |

## Tesztek
- **244 teszt, 0 fail** (változatlan — meglévő settings tesztek mind zöldek)
- `pnpm build` → 0 error
- `pnpm lint` → 0 új hiba
