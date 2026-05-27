# SpaceOS — Feladatok (Root nézet)

## Státuszok

| Mappa | Jelentés |
|---|---|
| `new/` | Beérkezett, root még nem döntött / nem adta ki |
| `active/` | Ki van adva terminálnak vagy operátornak, vár |
| `archive/` | DONE + elfogadott, lezárt |

---

## Jelenlegi állapot (2026-05-27)

### Architektúra

```
/opt/spaceos/
  backend/         ← 7 futó service + 3 NuGet lib + Orchestrator (AI gateway)
  frontend/        ← joinerytech-portal (247 teszt, dist deployed)
  docs/            ← dokumentáció
  keycloak/        ← IdP (Doorstar userek konfigurálva)
  tools/           ← dispatcher
```

### Lezárt milestoneok

- Soft Launch (Doorstar) ✅ 2026-04-20
- Bugfix Sprint ✅ (E2E 266/266)
- Growth Strategy v1 Sessions 1-5 ✅
- FreeTier API ✅ (176 teszt)
- Cabinet 0.1–0.3 ✅ (755 teszt, 10 NuGet)
- Cutting Phase 3–6 ✅ (931 teszt)
- PartnerTier MVP ✅ (232 teszt)
- Manufacturing Phase 1 ✅ (250 teszt)
- Portal World (5 world) + FE-026 mock data ✅ (242 teszt)
- **FE-027–035** ✅ — Portal újraépítés, auth, API integrációk, 247/247 teszt
- **Cabinet 0.3** ✅ — Track A+B+C lezárva, v0.3.0-alpha.1 (755 teszt)
- **NAGY ÁTALAKÍTÁS** ✅ — mappa restructure, Orchestrator = AI gateway only (121 teszt)
- **Site live** ✅ — https://joinerytech.hu elérhető (2026-04-30)
- **Doorstar seed** ✅ — tenant, facility, 5 workstation, 5 template, 9 KC user (2026-05-27)
- **Keycloak SMTP** ✅ — Brevo, noreply@joinerytech.hu, domain hitelesítve, reset password on (2026-05-27)
- **Orchestrator → systemd** ✅ — PM2 root helyett spaceos user, AI gateway only (2026-05-27)

### Backend tesztek: ~3761

### 🔵 Active

| Task | Terminál | Státusz |
|---|---|---|
| `IDENTITY-V1_modules-identity.md` | IDENTITY | Track A ✅ elfogadva (21 teszt) · Track B+C+D kiadva párhuzamosan |

### 🟡 New

Nincs new task.

### Következő lépések (nem kiadva)

| # | Feladat | Leírás | Előfeltétel |
|---|---|---|---|
| Identity Track B | IDENTITY | Application layer — elfogadva, 41 teszt | ✅ DONE |
| Identity Track C | IDENTITY | Infrastructure/Persistence — kiadva MSG-IDENTITY-003 | ✅ kiadva |
| Identity Track D | IDENTITY | KC client + Workers + Redis — elfogadva, 54 teszt | ✅ DONE |
| Identity Track E | IDENTITY | API controllers + Program.cs | Track B+C DONE |
| Identity Deploy | INFRA | systemd + nginx `/identity/*` → 5008 | P0-1 (JWT RS256) lezárva |
| Turnstile | — | Later — csak ha publikus regisztrációs form lesz | — |
