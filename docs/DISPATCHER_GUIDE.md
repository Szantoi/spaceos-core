# SpaceOS Dispatcher & Session Management Guide

> Hogyan kell kezelni a tmux session-öket és a dispatcher automatizációt.

---

## Gyors referencia

```bash
# Tmux session-ök listázása
tmux -S /tmp/spaceos.tmux list-sessions

# Csatlakozás session-höz
tmux -S /tmp/spaceos.tmux attach -t spaceos-conductor

# Session-ből kilépés (session fut tovább)
# Ctrl+B, majd D

# Dispatcher állapot
bash scripts/pause-dispatcher.sh status

# Összes session leállítása
bash scripts/cold-shutdown.sh
```

---

## 1. Tmux alapok

A SpaceOS egy **dedikált tmux socket**-et használ: `/tmp/spaceos.tmux`

Minden tmux parancsnak tartalmaznia kell: `-S /tmp/spaceos.tmux`

### Session-ök listázása

```bash
tmux -S /tmp/spaceos.tmux list-sessions
```

Példa output:
```
spaceos-conductor: 1 windows (created Wed Jun 17 17:30:54 2026)
spaceos-fe: 1 windows (created Thu Jun 18 06:03:12 2026)
spaceos-kernel: 1 windows (created Thu Jun 18 05:06:05 2026)
spaceos-root: 1 windows (created Tue Jun 16 20:22:42 2026) (attached)
```

### Csatlakozás session-höz

```bash
# Conductor session
tmux -S /tmp/spaceos.tmux attach -t spaceos-conductor

# Kernel session
tmux -S /tmp/spaceos.tmux attach -t spaceos-kernel

# Root session
tmux -S /tmp/spaceos.tmux attach -t spaceos-root
```

### Kilépés session-ből (session fut tovább)

```
Ctrl+B, majd D
```

### Session kilövése

```bash
tmux -S /tmp/spaceos.tmux kill-session -t spaceos-fe
```

---

## 2. Session-ök manuális indítása

### Új session indítása

```bash
# Új session létrehozása és Claude indítása
tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-kernel -c /opt/spaceos/SpaceOS.Kernel
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-kernel "claude --model sonnet" Enter
```

### Modell választás

A `--model` flag lehetséges értékei:
- `haiku` — gyors, kis feladatok
- `sonnet` — alapértelmezett, kód, elemzés
- `opus` — komplex architektúra, tervezés

### Session workdir-ek

| Session | Munkamappa |
|---------|------------|
| `spaceos-root` | `/opt/spaceos` |
| `spaceos-conductor` | `/opt/spaceos` |
| `spaceos-kernel` | `/opt/spaceos/SpaceOS.Kernel` |
| `spaceos-identity` | `/opt/spaceos/SpaceOS.Kernel` |
| `spaceos-orchestrator` | `/opt/spaceos/spaceos-orchestrator` |
| `spaceos-joinery` | `/opt/spaceos/spaceos-modules-joinery` |
| `spaceos-cutting` | `/opt/spaceos/spaceos-modules-cutting` |
| `spaceos-fe` | `/opt/spaceos/spaceos-doorstar-portal` |
| `spaceos-infra` | `/opt/spaceos/infra` |
| `spaceos-e2e` | `/opt/spaceos/e2e` |
| `spaceos-librarian` | `/opt/spaceos` |
| `spaceos-nexus` | `/opt/spaceos/spaceos-nexus` |
| `spaceos-architect` | `/opt/spaceos` |

---

## 3. Dispatcher automatizáció

A dispatcher a `nightwatch.sh` szkript, ami **2 percenként** fut cron-ból.

### Cron bejegyzések

```
*/2 * * * * /opt/spaceos/scripts/nightwatch.sh
*/5 * * * * /opt/spaceos/scripts/task-dispatcher.sh
*/10 * * * * /opt/spaceos/scripts/plan-scan.sh
* * * * * /opt/spaceos/scripts/telegram-bot.sh
```

### Nightwatch komponensek

| Szkript | Funkció |
|---------|---------|
| `watch-priority.sh` | Conductor session mindig fut |
| `watch-done.sh` | DONE outbox → reviewer indítás |
| `watch-stuck.sh` | Beakadt session → Enter küldés |
| `watch-inbox.sh` | UNREAD inbox → session auto-indítás |

### Dispatcher szüneteltetése

```bash
# Szünet BE — új munka nem indul
bash scripts/pause-dispatcher.sh on

# Szünet KI — normál működés
bash scripts/pause-dispatcher.sh off

# Állapot lekérdezés
bash scripts/pause-dispatcher.sh status
```

A szüneteltetés ideje alatt:
- Cron job-ok futnak, de nem indítanak új munkát
- Meglévő session-ök folytatják a munkát
- DONE feldolgozás megáll

---

## 4. Session típusok

### Priority session-ök (mindig futnak)

- `spaceos-conductor` — feladatkiosztás, pipeline

### Task-only session-ök (csak feladattal indulnak)

Ezek automatikusan indulnak ha van UNREAD inbox üzenet:

- `spaceos-kernel`, `spaceos-identity`
- `spaceos-orchestrator`, `spaceos-joinery`, `spaceos-cutting`
- `spaceos-fe`, `spaceos-fe-b`
- `spaceos-infra`, `spaceos-e2e`
- `spaceos-librarian`, `spaceos-nexus`
- `spaceos-architect`

### Speciális: Root session

- `spaceos-root` — manuálisan kezelendő, stratégiai döntésekhez

---

## 5. Inbox alapú indítás

A dispatcher automatikusan olvassa az inbox `model:` mezőjét:

```yaml
---
id: MSG-KERNEL-001
from: conductor
to: kernel
model: sonnet  # ← Ez határozza meg a modellt
status: UNREAD
---
```

Ha az inbox 2+ perce UNREAD és a session nem fut → auto-indítás.

---

## 6. Stuck session kezelés

A `watch-stuck.sh` detektálja:

- "Press up to edit queued messages" prompt
- Model-választó dialog
- Üres `❯` prompt

Ilyenkor 5 percenként Enter-t küld.

---

## 7. Logok

```bash
# Dispatcher log
tail -f /opt/spaceos/logs/dispatcher/nightwatch.log

# Task dispatcher log
tail -f /opt/spaceos/logs/dispatcher/task-dispatcher.log

# Plan scan log
tail -f /opt/spaceos/logs/dispatcher/plan-scan.log

# Telegram bot log
tail -f /opt/spaceos/logs/dispatcher/telegram.log
```

---

## 8. Teljes leállítás

### Hideg leállítás (session-ök maguktól fejeződnek be)

```bash
bash scripts/cold-shutdown.sh
```

### Összes session kilövése

```bash
tmux -S /tmp/spaceos.tmux kill-server
```

---

## 9. Hibaelhárítás

### Session nem indul

1. Ellenőrizd a dispatcher állapotát:
   ```bash
   bash scripts/pause-dispatcher.sh status
   ```

2. Ellenőrizd az inbox-ot:
   ```bash
   ls docs/mailbox/<terminál>/inbox/
   grep "status:" docs/mailbox/<terminál>/inbox/*.md
   ```

3. Manuálisan indítsd a session-t (lásd 2. fejezet)

### Session beakadt

1. Csatlakozz és nézd meg mi van:
   ```bash
   tmux -S /tmp/spaceos.tmux attach -t <session>
   ```

2. Küldj Enter-t:
   ```bash
   tmux -S /tmp/spaceos.tmux send-keys -t <session> Enter
   ```

### Dispatcher nem fut

1. Ellenőrizd a cron-t:
   ```bash
   crontab -l | grep nightwatch
   ```

2. Futtasd manuálisan:
   ```bash
   bash scripts/nightwatch.sh
   ```

---

## 10. Példa workflow

### Új feladat kiadása terminálnak

1. Írj inbox üzenetet:
   ```bash
   vim docs/mailbox/kernel/inbox/2026-06-18_001_my-task.md
   ```

2. Frontmatter:
   ```yaml
   ---
   id: MSG-KERNEL-001
   from: conductor
   to: kernel
   type: task
   priority: high
   status: UNREAD
   model: sonnet
   created: 2026-06-18
   ---

   # Feladat leírása

   ...
   ```

3. A dispatcher 2 percen belül elindítja a session-t.

### Session manuális triggerelése

```bash
# Session-höz csatlakozás
tmux -S /tmp/spaceos.tmux attach -t spaceos-kernel

# Üzenet beírása
# "Te a KERNEL terminál vagy. Olvasd be: MEMORY.md — Folytasd a munkát."
```

---

*Utolsó frissítés: 2026-06-18*
