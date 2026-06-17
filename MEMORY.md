# ROOT Memory

Utolsó frissítés: 2026-06-17 07:25

## Aktuális állapot
- Memory rendszer KÉSZ: minden terminálnak saját MEMORY.md a munkakönyvtárában
- `stop-idle.sh` szkript KÉSZ: leállítja a feladat nélküli terminálokat
- Commit: `2b84be7` — memory file location refactor

## Fontos kontextus

### Reviewer rendszer (refaktorálva)
- **Hideg indítás:** `claude -p` (nem interaktív, friss processz)
- **Konfiguráció:** `scripts/reviewer-config.yaml` (minden paraméter)
- **Prompt:** `scripts/reviewer-prompt.md` (template placeholderekkel)
- **Kontextus:** `scripts/reviewer-context.md` (projekt tudás)

| Placeholder | Tartalom |
|---|---|
| `{{CONTEXT}}` | reviewer-context.md tartalma |
| `{{INBOX_PATH}}` | Eredeti feladat fájl útvonala |
| `{{INBOX_CONTENT}}` | Eredeti feladat tartalma |
| `{{DONE_PATH}}` | DONE üzenet fájl útvonala |
| `{{DONE_CONTENT}}` | DONE üzenet tartalma |

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
