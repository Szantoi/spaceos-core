# SpaceOS — Feladatok (Root nézet)

## Státuszok

| Mappa | Jelentés |
|---|---|
| `new/` | Beérkezett, root még nem döntött / nem adta ki |
| `active/` | Ki van adva terminálnak vagy operátornak, vár |
| `archive/` | DONE + elfogadott, lezárt |

---

## Jelenlegi állapot (2026-04-30 — nagy átalakítás után)

### Architektúra

```
/opt/spaceos/
  backend/         ← 7 futó service + 3 NuGet lib + Orchestrator
  frontend/        ← joinerytech-portal (újraépítés alatt)
  docs/            ← dokumentáció
  keycloak/        ← IdP
  tools/           ← dispatcher
```

### Lezárt milestoneok

- Soft Launch (Doorstar) ✅ 2026-04-20
- Bugfix Sprint ✅ (E2E 266/266)
- Growth Strategy v1 Sessions 1-5 ✅
- FreeTier API ✅ (176 teszt)
- Cabinet 0.1–0.3 ✅ (719 teszt, 10 NuGet)
- Cutting Phase 3–6 ✅ (931 teszt)
- PartnerTier MVP ✅ (232 teszt)
- Manufacturing Phase 1 ✅ (250 teszt)
- Portal World (5 world) + FE-026 mock data ✅ (242 teszt)
- **FE-027 Portal Scaffold** ✅ — Vite + React 18 + TS + Tailwind v4 (179 teszt, commit 3b95802)
- **FE-028 Auth + API** ✅ — Keycloak OIDC PKCE + API hook + route guard (189 teszt, commit 7e4c58b)
- **FE-029 Landing page** ✅ — publikus bemutatkozó + route átszervezés (195 teszt, commit 3d4b8cc)
- **FE referencia oldalak** ✅ — 13+ oldal teljes redesign, 229/229 teszt, 0 build hiba, commit `720e106`
- **Cabinet 0.3** ✅ — Track A+B+C lezárva, v0.3.0-alpha.1 (755 teszt)
- **NAGY ÁTALAKÍTÁS** ✅ — mappa restructure, frontend reset, Orchestrator = AI gateway
- **Site live** ✅ — https://joinerytech.hu elérhető (2026-04-30)

### Backend tesztek: ~3894

### 🔵 Active

Nincs aktív task.

### 🟡 New

Nincs new task.

### Következő lépések (nem kiadva)

| # | Feladat | Leírás |
|---|---|---|
| 1 | nginx API proxy | Direkt route-ok backend service-ekhez (nem Orchestrator-on át) |
| 2 | Orchestrator cleanup | BFF proxy route-ok eltávolítása, csak AI/LLM marad |
| 3 | Backend API integráció (FE) | Valós API hívások mock helyett |
| 4 | Brevo API key | Gábor regisztrálja (brevo.com) |
| 5 | Turnstile site key | Gábor regisztrálja (Cloudflare) |
