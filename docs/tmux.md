# tmux Usage Guide - SpaceOS Terminals

> **Minden terminal tmux session-ben fut** `/tmp/spaceos.tmux` socket-en keresztul.

---

## Alapveto Architektura

```
MCP API (/api/session/start)
    |
    v
sessionManager.ts
    |
    v
tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-<terminal>
    |
    v
claude --model sonnet --dangerously-skip-permissions -c "prompt"
    |
    v
Terminal dolgozik, outbox-ba ir
    |
    v
Idle state (>> bypass permissions on)
```

**Kulcs:** A SpaceOS **sajat tmux socket**-et hasznal, NEM az alapertelmezettet!

---

## Session-ok listazasa

```bash
# ROSSZ - alapertelmezett socket (/tmp/tmux-1000/default)
tmux ls

# JO - SpaceOS socket
tmux -S /tmp/spaceos.tmux ls
```

**Pelda kimenet:**
```
spaceos-conductor: 1 windows (created Fri Jul  3 10:13:12 2026)
spaceos-backend: 1 windows (created Fri Jul  3 09:45:23 2026)
spaceos-root: 1 windows (created Fri Jul  3 10:12:56 2026)
```

---

## Session megtekintese (attach)

```bash
# Conductor session attach (read-only mode)
tmux -S /tmp/spaceos.tmux attach -t spaceos-conductor -r

# Detach: Ctrl+b -> d
```

**Figyelem:** `-r` (read-only) ajanlott, hogy ne zavard meg a futo munkat!

---

## Prompt kuldese IDLE terminalnak

### ROSSZ MODSZER (csak sortorestet ad)

```bash
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-conductor "Folytasd" Enter
# Ez NEM fog mukodni Claude Code idle state-nel!
```

### HELYES MODSZER (Marveen-style injection)

```bash
# 1. Prompt kuldese literal mode-ban (-l flag)
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-conductor -l "Folytasd a munkat"

# 2. Hex carriage return (0d) az Enter helyett
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-conductor -H 0d
```

**Vagy egysorosban:**
```bash
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-conductor -l "Dolgozd fel az inbox-ot" && \
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-conductor -H 0d
```

**Miert?**
- `-l` = literal mode (nem ertelmez escape sequence-eket, zarojel-paste problemat elkeruli)
- `-H 0d` = hex carriage return (megbizhatobb mint az "Enter" kulcsszo)

---

## MCP API hasznalata (ajanlott)

**Prompt injection MCP API-n keresztul:**

```bash
curl -X POST http://localhost:3456/api/session/inject \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "conductor",
    "prompt": "Folytasd a munkat",
    "fromTerminal": "root"
  }'
```

**Session inditas prompttal:**

```bash
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "model": "sonnet",
    "prompt": "Olvasd el az inbox-ot",
    "fromTerminal": "root"
  }'
```

**Wake-up (auto-start + inbox):**

```bash
curl -X POST http://localhost:3456/api/session/wake \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "fromTerminal": "conductor"
  }'
```

**Session status:**

```bash
curl -s http://localhost:3456/api/session/backend | jq
```

---

## Gyakori muveletek

### 1. Terminal kimenet ellenorzese (tail)

```bash
# Utolso 15 sor
tmux -S /tmp/spaceos.tmux capture-pane -t spaceos-conductor -p | tail -15

# Kereses hibara
tmux -S /tmp/spaceos.tmux capture-pane -t spaceos-backend -p | grep -i error
```

### 2. Session ujrainditas

```bash
# Terminal leallitasa (C-c kuldese)
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-backend C-c

# Vagy kill session
tmux -S /tmp/spaceos.tmux kill-session -t spaceos-backend

# Ujrainditas MCP API-n keresztul
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","model":"sonnet","fromTerminal":"root"}'
```

### 3. Osszes session status

```bash
curl -s http://localhost:3456/api/sessions/all | jq '.sessions[] | {terminal, sessionExists, claudeRunning}'
```

---

## Jogosultsag matrix (fromTerminal)

| Kezdemenyezo | Iranyithat |
|---|---|
| **root** | MINDENKIT (8 terminal) |
| **conductor** | architect, librarian, explorer, backend, frontend, designer |
| **tobbi** | csak sajat magat |

**Pelda:**

```bash
# Root indithat Conductor-t
curl -X POST localhost:3456/api/session/inject \
  -d '{"terminal":"conductor","prompt":"Folytasd","fromTerminal":"root"}'

# Backend NEM indithatja Conductor-t (permission denied)
curl -X POST localhost:3456/api/session/inject \
  -d '{"terminal":"conductor","prompt":"Folytasd","fromTerminal":"backend"}'
```

---

## Hibakereses

### Session nem indul

```bash
# Ellenorizd a socket-et
ls -la /tmp/spaceos.tmux

# Nezd a session manager log-ot
tail -50 /opt/spaceos/spaceos-nexus/knowledge-service/logs/service.log | grep SessionManager

# Audit trail
cat /opt/spaceos/logs/sessions/$(date +%Y-%m-%d).jsonl | jq 'select(.terminal == "backend")'
```

### Prompt nem megy keresztul

```bash
# Ellenorizd hogy Claude fut-e
tmux -S /tmp/spaceos.tmux capture-pane -t spaceos-conductor -p | tail -5 | grep ">>"

# Ha fut, hasznald az -l es -H 0d flag-eket!
```

### Session "stuck"

```bash
# Nightwatch figyeli es Enter-t kuld automatikusan
tail -20 /opt/spaceos/logs/dispatcher/nightwatch.log | grep stuck

# Manualis nudge
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-backend "" Enter
```

---

## Nightwatch automatikus kezeles

A `nightwatch.ts` (600s ciklus) automatikusan:
- Figyeli az UNREAD inbox-okat -> MCP wake-up
- Figyeli a DONE outbox-okat -> reviewer.ts + pipeline.ts
- Stuck session detektal -> Enter nudge
- Idle terminalokat heartbeat-tel tartja eletben

**Manualis nightwatch ciklus triggereles (fejleszteshez):**
```bash
# Uj ciklus inditasa a knowledge-service-en keresztul
curl -X POST http://localhost:3456/api/internal/nightwatch/trigger
```

---

## Best Practices

1. **Mindig az MCP API-t hasznald** prompt injectionhoz (audit trail!)
2. **Read-only attach** (`-r`) ha nezni akarod a session-t
3. **Ne hasznalj `Enter` kulcsszot** - helyette `-H 0d`
4. **Ellenorizd a jogosultsagokat** (`fromTerminal` mezo)
5. **Session log-okat nezd** (`/opt/spaceos/logs/sessions/`)

---

## Referenciak

- **sessionManager.ts**: `/opt/spaceos/spaceos-nexus/knowledge-service/src/sessionManager.ts`
- **MCP API routes**: `/opt/spaceos/spaceos-nexus/knowledge-service/src/routes/sessionRoutes.ts`
- **Nightwatch**: `/opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline/nightwatch.ts`
- **Audit log**: `/opt/spaceos/logs/sessions/YYYY-MM-DD.jsonl`
