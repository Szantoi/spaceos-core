# ROOT Memory

Utolsó frissítés: 2026-06-23 14:30

## LEGUTÓBBI FEJLESZTÉS (2026-06-23)

### Dispatch Control & Token Budget System — KÉSZ

**Lokáció:** `spaceos-nexus/knowledge-service/src/dispatch-control/`
**Dokumentáció:** `docs/agent-infrastructure/DISPATCH_CONTROL_IMPLEMENTATION.md`
**Tesztek:** 57 passing (24 + 14 + 19)

| Phase | Modul | Funkció |
|-------|-------|---------|
| **Phase 2** | `tokenBudget.ts` | Token használat tracking, napi limitek, budget státusz |
| **Phase 3** | (integrált) | Budget alerts (80%/90%/100% threshold) Telegram értesítés |
| **Phase 4** | `dispatchProposal.ts` | Conductor→Root javaslat workflow, approve/reject |
| **Phase 5** | `scheduledWindows.ts` | Időablak-alapú dispatch, terminál engedélyek |

**API végpontok:**
```
/api/control/mode              — dispatch mód (auto/manual/scheduled)
/api/control/budget            — budget státusz
/api/control/budget/:terminal  — terminál limit
/api/control/can-dispatch      — dispatch ellenőrzés
/api/control/queue             — dispatch várakozási sor
/api/control/proposals         — Conductor javaslatok
/api/control/windows           — scheduled windows
```

**Használat:**
```bash
# Budget ellenőrzés
curl localhost:3456/api/control/budget

# Dispatch engedélyezés
curl "localhost:3456/api/control/can-dispatch?terminal=backend&estimatedTokens=5000"

# Scheduled windows betöltése
curl -X POST localhost:3456/api/control/windows/load-defaults
```

---

## KRITIKUS TANULSÁG (2026-06-23)

### Autonóm rendszer leállítása fél napba telt!

A probléma: 6 különböző perzisztencia réteg automatikusan újraindította egymást.

**Perzisztencia rétegek:**
| # | Típus | Lokáció |
|---|---|---|
| 1 | System systemd | `/etc/systemd/system/spaceos-knowledge.service` |
| 2 | System systemd | `/etc/systemd/system/claude-code.service` |
| 3 | System systemd | `/etc/systemd/system/marveen.service` |
| 4 | **User systemd** | `~/.config/systemd/user/spaceos-dashboard.service` |
| 5 | **User systemd** | `~/.config/systemd/user/spaceos-channels.service` |
| 6 | **User systemd** | `~/.config/systemd/user/spaceos-morning.timer` |
| 7 | Háttér process | Régi `node dist/server.js` napokkal korábbiról |

**Megoldás - 3 új parancs:**
```bash
nexus kill      # 🛑 MINDEN leállítása (6 réteg)
nexus disable   # 🔒 Auto-start kikapcsolása
nexus enable    # 🔓 Auto-start visszakapcsolása
```

**Részletes dokumentáció:** `docs/knowledge/debugging/AUTONOMOUS_SHUTDOWN_BUG_2026-06-23.md`

---

## Aktuális állapot
- Memory rendszer KÉSZ: minden terminálnak saját MEMORY.md a munkakönyvtárában
- `stop-idle.sh` szkript KÉSZ: leállítja a feladat nélküli terminálokat
- Commit: `2b84be7` — memory file location refactor

## Fontos kontextus

### Pipeline rendszer (refaktorálva)

Minden pipeline komponens:
- **Hideg indítás:** `claude -p` (friss processz, timeout)
- **Konfiguráció:** YAML fájlból (modellek, timing, útvonalak)
- **Promptok:** külön .md fájlok `{{placeholder}}` szintaxissal

| Komponens | Config | Promptok |
|---|---|---|
| **Reviewer** | `reviewer-config.yaml` | `reviewer-prompt.md` |
| **Planning** | `plan-config.yaml` | `prompts/plan-*.md` |

**Prompt placeholder szintaxis:** `{{VARIABLE_NAME}}`

```
scripts/
├── yaml-parser.sh           # Közös YAML parser
├── reviewer-config.yaml     # Reviewer konfig
├── reviewer-prompt.md       # Reviewer prompt
├── plan-config.yaml         # Planning konfig
└── prompts/
    ├── plan-scan-prompt.md
    ├── plan-select-prompt.md
    ├── plan-debate-prompt.md
    ├── plan-review-prompt.md
    └── plan-consensus-prompt.md
```

### Terminál architektúra
- **DONE routing:** terminálok → conductor (nem root)
- **Model policy:** haiku (kód), sonnet (spec), opus (architektúra)
- **Root triggering:** NINCS automatikus trigger (watch-stuck, work-nudge kikapcsolva)
- **Priority sessions:** csak `spaceos-conductor`

### Dispatcher szkriptek
| Szkript | Funkció |
|---|---|
| `nightwatch.sh` | Fő dispatcher (*/2 cron) |
| `watch-inbox.sh` | UNREAD inbox → session indítás/nudge |
| `watch-stuck.sh` | Beakadt session → Enter nudge |
| `stop-idle.sh` | **ÚJ** — Feladat nélküli terminálok leállítása |
| `stop-terminals.sh` | Minden terminál leállítása |
| `pause-dispatcher.sh` | Flag-alapú szüneteltetés |

### Hideg indítás protokoll
1. `./scripts/stop-idle.sh` — feladat nélküli terminálok leállítása
2. `cat MEMORY.md` — kontextus visszaolvasás
3. Inbox ellenőrzés → feladat folytatás

## Következő lépések
- Conductor inbox ellenőrzése (4 UNREAD üzenet)
- Orch inbox ellenőrzése (1 UNREAD üzenet)

## Megoldott problémák
- **Probléma:** DONE üzenetek Root-nak mentek Conductor helyett
  **Megoldás:** SKILL.md és CLAUDE.md fájlok frissítése `to: conductor`-ra

- **Probléma:** Terminálok nem tudták az identitásukat
  **Megoldás:** Trigger üzenetekbe identitás: "Te a ${TERMINAL^^} terminál vagy."

- **Probléma:** Memory fájlok központi mappában voltak
  **Megoldás:** MEMORY.md áthelyezve minden terminál saját könyvtárába

- **Probléma:** Feladat nélküli terminálok futottak feleslegesen
  **Megoldás:** `stop-idle.sh` szkript — leállítja akinek nincs UNREAD inbox

## Session tapasztalatok
- Lokális MEMORY.md egyszerűbb mint abszolút útvonal
- Hideg indítás: stop-idle.sh + MEMORY.md kombináció működik
