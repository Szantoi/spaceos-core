# tmux Session Kezelés — Gyorsútmutató

## Alapok

A SpaceOS és Datahaven összes session egy közös tmux socketen fut:
```
Socket: /tmp/spaceos.tmux
```

---

## Sessionök listázása

```bash
tmux -S /tmp/spaceos.tmux list-sessions
```

Várható kimenet:
```
spaceos-architect: 1 windows (created ...)
spaceos-fe-b:      1 windows (created ...)
spaceos-joinery:   1 windows (created ...)
spaceos-kernel:    1 windows (created ...)
spaceos-librarian: 1 windows (created ...)
spaceos-nexus:     1 windows (created ...)
spaceos-orch:      1 windows (created ...)
spaceos-root:      1 windows (created ...)
```

Vagy egyszerűen:
```bash
bash /opt/spaceos/scripts/status.sh
```

---

## Csatlakozás egy sessionhöz

```bash
tmux -S /tmp/spaceos.tmux attach -t <session-neve>
```

### Példák

| Mit akarsz látni | Parancs |
|---|---|
| Frontend fejlesztés | `tmux -S /tmp/spaceos.tmux attach -t spaceos-fe-b` |
| Kernel backend | `tmux -S /tmp/spaceos.tmux attach -t spaceos-kernel` |
| Architect tervez | `tmux -S /tmp/spaceos.tmux attach -t spaceos-architect` |
| Datahaven / Nexus | `tmux -S /tmp/spaceos.tmux attach -t spaceos-nexus` |
| Joinery modul | `tmux -S /tmp/spaceos.tmux attach -t spaceos-joinery` |
| Librarian | `tmux -S /tmp/spaceos.tmux attach -t spaceos-librarian` |
| Orchestrator | `tmux -S /tmp/spaceos.tmux attach -t spaceos-orch` |
| Root / Sárkány | `tmux -S /tmp/spaceos.tmux attach -t spaceos-root` |

---

## Kilépés a sessionből (session tovább fut!)

```
Ctrl + B  →  D
```

Ez **lecsatlakozik** (detach) — a session és a Claude tovább fut háttérben.  
**Soha ne használd a `Ctrl+C`-t vagy `exit`-et** — az leállítja a Claude sessiont.

---

## Új session indítása (ha egy daemon leállt)

```bash
tmux -S /tmp/spaceos.tmux new-session -d -s <session-neve> -c <munkamappa>
tmux -S /tmp/spaceos.tmux send-keys -t <session-neve> "claude --model <modell>" Enter
```

### Modellek session-ként

| Session | Modell |
|---|---|
| spaceos-fe, spaceos-fe-b | `sonnet` |
| spaceos-kernel | `sonnet` |
| spaceos-joinery | `sonnet` |
| spaceos-orch | `sonnet` |
| spaceos-architect | `opus` |
| spaceos-librarian | `haiku` |
| spaceos-nexus | `sonnet` |

### Példa — Nexus újraindítása

```bash
tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-nexus -c /opt/spaceos/spaceos-nexus
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-nexus "claude --model sonnet" Enter
```

---

## Hasznos tmux parancsok (sessionön belül)

| Parancs | Mit csinál |
|---|---|
| `Ctrl+B D` | Lecsatlakozás (daemon fut tovább) |
| `Ctrl+B [` | Scroll mód (nyilakkal görget, `Q` kilép) |
| `Ctrl+B C` | Új ablak ugyanabban a sessionban |
| `Ctrl+B N` | Következő ablak |
| `Ctrl+B P` | Előző ablak |

---

## Gyors aliasok (opcionális — tedd a ~/.bashrc-be)

```bash
# SpaceOS session aliasok
alias s-status='bash /opt/spaceos/scripts/status.sh'
alias s-fe='tmux -S /tmp/spaceos.tmux attach -t spaceos-fe-b'
alias s-kernel='tmux -S /tmp/spaceos.tmux attach -t spaceos-kernel'
alias s-arch='tmux -S /tmp/spaceos.tmux attach -t spaceos-architect'
alias s-nexus='tmux -S /tmp/spaceos.tmux attach -t spaceos-nexus'
alias s-root='tmux -S /tmp/spaceos.tmux attach -t spaceos-root'
alias s-ls='tmux -S /tmp/spaceos.tmux list-sessions'
```

Aktiválás:
```bash
source ~/.bashrc
```

Utána elég:
```bash
s-status    # teljes áttekintés
s-nexus     # Datahaven session
s-arch      # Architect session
```

---

## Mi történik ha a VPS újraindul?

A tmux sessionök elvesznek újraindításkor. A nightwatch.sh automatikusan újraindítja a sessionöket ha inbox üzenet van — de ha kézzel akarod:

```bash
bash /opt/spaceos/scripts/watch-inbox.sh
```

Ez minden terminálhoz létrehozza a sessiont ha UNREAD inbox van.
