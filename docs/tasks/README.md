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
- **NAGY ÁTALAKÍTÁS** ✅ — mappa restructure, frontend reset, Orchestrator = AI gateway

### Backend tesztek: ~3858

### 🔵 Active

Nincs aktív task.

### 🟡 New

Nincs new task.

### Következő lépések (nem kiadva)

| # | Feladat | Leírás |
|---|---|---|
| 1 | Frontend rebuild | `frontend/joinerytech-portal/` — egyetlen React app, joinerytech.hu |
| 2 | nginx API proxy | Direkt route-ok backend service-ekhez (nem Orchestrator-on át) |
| 3 | Orchestrator cleanup | BFF proxy route-ok eltávolítása, csak AI/LLM marad |
| 4 | Brevo API key | Gábor regisztrálja (brevo.com) |
| 5 | Turnstile site key | Gábor regisztrálja (Cloudflare) |
