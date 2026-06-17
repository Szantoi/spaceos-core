# ROOT Memory

Utolsó frissítés: 2026-06-17 07:25

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
