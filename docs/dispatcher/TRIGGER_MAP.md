# SpaceOS Dispatcher — Trigger Map

> Naprakész dokumentáció arról, hogy melyik terminált mi triggereli.
> Utolsó frissítés: 2026-06-17

---

## Cron Jobs

| Szkript | Gyakoriság | Funkció |
|---------|------------|---------|
| `nightwatch.sh` | */2 perc | Dispatcher orchestrator — hívja a watch-*.sh szkripteket |
| `watch-stuck.sh` | */5 perc | Beakadt session-ök → Enter küld |
| `watch-inbox.sh` | */3 perc | UNREAD inbox → session indítás |
| `watch-done.sh` | */2 perc | DONE outbox → reviewer.sh indítás |
| `plan-scan.sh` | */10 perc | Ötletek scan → planning queue |
| `telegram-bot.sh` | */1 perc | Telegram parancsok fogadása |
| `telegram-datahaven-bot.sh` | */1 perc | Datahaven Telegram bot |

---

## Terminál Triggerek

### ROOT (spaceos-root)

| Trigger | Forrás | Mikor |
|---------|--------|-------|
| **Nincs automatikus trigger** | — | Root manuálisan indul vagy inbox-ra |
| Inbox üzenet | `watch-inbox.sh` | Ha van UNREAD `docs/mailbox/root/inbox/` |

**Megjegyzés:** Root kikerült a `PRIORITY_SESSIONS`-ből és a `watch-stuck.sh`-ból is. Nem kap automatikus triggert.

---

### CONDUCTOR (spaceos-conductor)

| Trigger | Forrás | Mikor |
|---------|--------|-------|
| Auto-indítás | `watch-priority.sh` | Ha session leállt → újraindít |
| Stuck kezelés | `watch-stuck.sh` | Ha beakadt (5 percenként max) |
| Pipeline inbox | `pipeline-docs.sh` | DONE feldolgozás után → következő task |
| Terminál DONE/BLOCKED | Terminálok | Skill-ből `to: conductor` |

**Megjegyzés:** Conductor az egyetlen `PRIORITY_SESSIONS` tag — mindig fut.

---

### TERMINÁLOK (fe, kernel, identity, stb.)

| Trigger | Forrás | Mikor |
|---------|--------|-------|
| Inbox indítás | `watch-inbox.sh` | Ha van UNREAD inbox → session indít |
| Stuck kezelés | `watch-stuck.sh` | Ha beakadt (5 percenként max) |
| Review reject | `reviewer.sh` | Ha DONE visszadobva → új inbox |

**Megjegyzés:** Terminálok csak feladattal indulnak (`TASK_ONLY_TERMINALS`).

---

### NEXUS (spaceos-nexus)

| Trigger | Forrás | Mikor |
|---------|--------|-------|
| Inbox indítás | `watch-inbox.sh` | Ha van UNREAD inbox |
| Stuck kezelés | `watch-stuck.sh` | Ha beakadt |
| Datahaven/Resonance | `pipeline-docs.sh` | Ha DONE assignee: nexus |

---

## Üzenet Routing

### DONE/BLOCKED üzenetek

```
Terminál DONE outbox
    ↓
watch-done.sh (*/2 perc)
    ↓
reviewer.sh (2× Haiku)
    ↓
APPROVE → pipeline.sh → pipeline-docs.sh → Conductor inbox
REJECT  → reviewer.sh → Terminál inbox (javításra)
```

### Planning Pipeline

```
plan-scan.sh (*/10 perc)
    ↓
docs/planning/ideas/
    ↓
plan-select.sh → plan-debate.sh (2× Sonnet)
    ↓
docs/planning/queue/
    ↓
Conductor feldolgozza → Terminál inbox
```

---

## Fájlok

| Fájl | Funkció |
|------|---------|
| `scripts/common.sh` | `PRIORITY_SESSIONS`, `SESSIONS`, `SESSION_WORKDIR` definíciók |
| `scripts/nightwatch.sh` | Dispatcher orchestrator |
| `scripts/watch-priority.sh` | Priority session-ök (Conductor) újraindítása |
| `scripts/watch-stuck.sh` | Beakadt session-ök kezelése |
| `scripts/watch-inbox.sh` | UNREAD inbox → session indítás |
| `scripts/watch-done.sh` | DONE outbox → reviewer indítás |
| `scripts/reviewer.sh` | Dual Haiku review |
| `scripts/pipeline.sh` | DONE feldolgozás orchestrator |
| `scripts/pipeline-docs.sh` | Docs frissítés + következő inbox (→ Conductor) |

---

## Modell Policy

| Feladat típus | Modell | Indoklás |
|---------------|--------|----------|
| Architektúra tervezés | **opus** | Komplex cross-module döntések |
| Spec review (v1-v4) | **sonnet** | Tervezési minőség biztosítása |
| Kód implementáció | **haiku** | Fókuszált feladat, spec kész, dual review utána |
| DONE review | **haiku** | 2× párhuzamos, gyors értékelés |
| Librarian | **haiku** | Egyszerű memória sync |
| Infra feladatok | **haiku** | Konfiguráció, nem komplex logika |

**Biztonsági tartalékok:**
1. v1-v4 pipeline (sonnet) — spec minőség
2. Dual Haiku review — implementáció ellenőrzés
3. Conductor koordináció — feladat routing

---

## Pause/Stop

| Parancs | Hatás |
|---------|-------|
| `./scripts/pause-dispatcher.sh on` | Dispatcher szünetel (cron fut, de nem indít munkát) |
| `./scripts/pause-dispatcher.sh off` | Dispatcher aktív |
| `./scripts/cron-control.sh stop` | Crontab kikapcsolása (backup) |
| `./scripts/cron-control.sh start` | Crontab visszakapcsolása |
| `./scripts/stop-terminals.sh` | Terminálok graceful leállítása |
| `./scripts/cold-shutdown.sh` | Passzív várakozás session-ök leállására |

---

## Changelog

| Dátum | Változás |
|-------|----------|
| 2026-06-17 | Root kikerült PRIORITY_SESSIONS-ből |
| 2026-06-17 | work-nudge.sh → Conductor (majd eltávolítva crontab-ból) |
| 2026-06-17 | pipeline-docs.sh routing → Conductor |
| 2026-06-17 | 12× SKILL.md: to: root → to: conductor |
| 2026-06-17 | pause-dispatcher.sh, cron-control.sh, stop-terminals.sh, cold-shutdown.sh létrehozva |
| 2026-06-17 | Root kikerült watch-stuck.sh-ból |
| 2026-06-17 | Modell policy: kód feladatok sonnet → haiku (pipeline-docs.sh) |
