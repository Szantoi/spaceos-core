# SpaceOS Dispatcher v2 — Összefoglaló

> Automatizált mailbox dispatch rendszer a SpaceOS multi-terminál workflow-hoz.
> A Root inbox üzenetet ír → dispatcher észleli → beírja a megfelelő Claude Code session-be.

---

## Architektúra

```
VS Code terminál tabjai:

  Tab 1: ssh spaceos → claude -c                    ← ROOT (kézi, te irányítod)
  Tab 2: ssh spaceos → tmux attach -t spaceos-kernel     ← élő nézet
  Tab 3: ssh spaceos → tmux attach -t spaceos-orch       ← élő nézet
  Tab N: ssh spaceos → tmux attach -t spaceos-portal     ← élő nézet

VPS háttérben:

  tmux session-ök:
    spaceos-kernel        claude --dangerously-skip-permissions -c @ spaceos-kernel/
    spaceos-orch          claude --dangerously-skip-permissions -c @ spaceos-orchestrator/
    spaceos-portal        claude --dangerously-skip-permissions -c @ design-portal/
    spaceos-joinery       claude --dangerously-skip-permissions -c @ spaceos-modules-joinery/
    spaceos-abstractions  claude --dangerously-skip-permissions -c @ spaceos-modules-abstractions/

  spaceos-dispatcher (daemon):
    inotifywait → figyeli docs/mailbox/*/inbox/ könyvtárakat
    UNREAD .md fájl megjelenik → tmux send-keys → beírja a session-be
```

---

## Automatizált vs. kézi terminálok

| Terminál | Tmux session | Dispatcher kezeli? |
|---|---|---|
| **Root** | — (közvetlen SSH) | ❌ Te irányítod |
| **Kernel** | spaceos-kernel | ✅ Automatikus |
| **Orchestrator** | spaceos-orch | ✅ Automatikus |
| **Portal** | spaceos-portal | ✅ Automatikus |
| **Joinery** | spaceos-joinery | ✅ Automatikus |
| **Abstractions** | spaceos-abstractions | ✅ Automatikus |
| **E2E** | — | ❌ Kézi |
| **Infra** | — | ❌ Kézi |

---

## Napi indítás

### VPS boot / friss indítás után

```bash
ssh spaceos

# 1. Tmux session-ök indítása (claude elindul mindegyikben)
sd --launch

# 2. Várj ~20 másodpercet (5 session × claude startup)
sleep 20

# 3. Ellenőrzés
sd-ls            # Minden session él?
sd --status      # Részletes állapot

# 4. Dispatcher indítása háttérben
sd --daemon
```

### VS Code terminálok megnyitása

```
Tab 1 (Root):     ssh spaceos → cd /opt/spaceos → claude --dangerously-skip-permissions -c
Tab 2 (Kernel):   ssh spaceos → tmux attach -t spaceos-kernel
Tab 3 (Orch):     ssh spaceos → tmux attach -t spaceos-orch
Tab 4 (Portal):   ssh spaceos → tmux attach -t spaceos-portal
...bármelyik terminálhoz csatlakozhatsz
```

---

## Dispatch flow — hogyan működik

```
1. Te a root session-ben írsz egy inbox üzenetet:
   docs/mailbox/kernel/inbox/2026-04-14_068_fix-something.md
   (status: UNREAD)

2. Dispatcher (háttérben futó daemon) inotifywait-tel észleli az új fájlt

3. Dispatcher tmux send-keys-szel "begépeli" az utasítást
   a spaceos-kernel session-be — mintha te írtad volna

4. A kernel session-ben futó Claude Code:
   - Elolvassa az inbox üzenetet
   - status: UNREAD → READ
   - Végrehajtja a feladatot (CODE → BUILD → TEST)
   - Outbox-ba ír (DONE / BLOCKED)

5. Te bármikor rácsatlakozhatsz és élőben nézed:
   tmux attach -t spaceos-kernel
```

---

## Alias-ok (sd parancsok)

| Alias | Parancs | Mire való |
|---|---|---|
| `sd --launch` | Session-ök indítása | Boot után egyszer |
| `sd --daemon` | Dispatcher háttérben | Boot után egyszer |
| `sd --status` | Állapot lekérdezés | Mi fut, UNREAD-ek |
| `sd --stop` | Dispatcher leállítás | Daemon kill |
| `sd --dry-run` | Teszt mód | Logol, de nem küld |
| `sd-log` | Log követés | `tail -f` a dispatcher logra |
| `sd-ls` | Session lista | Aktív tmux session-ök |

---

## Tmux kezelés

| Művelet | Parancs |
|---|---|
| Csatlakozás | `tmux attach -t spaceos-kernel` |
| Leválás | **Ctrl+B, aztán D** (session fut tovább!) |
| Session lista | `tmux ls` vagy `sd-ls` |
| Session váltás (tmux-ban) | **Ctrl+B, aztán S** |
| Minden session leállítása | `tmux kill-server` |

### Fontos: leválás vs. kilépés

| Mit csinálsz | Mi történik |
|---|---|
| **Ctrl+B, D** | Leválás — session és claude **fut tovább** |
| `/exit` a claude-ban | Claude kilép — tmux session **meghal** |
| VS Code tab bezárás | SSH megszakad — session **fut tovább** |
| VPS reboot | Minden meghal — `sd --launch` kell újra |

---

## Fájl helyek

```
/opt/spaceos/
├── tools/dispatcher/
│   └── spaceos-dispatcher.sh       ← a dispatcher script
├── logs/dispatcher/
│   └── dispatcher.log              ← dispatcher log
└── docs/mailbox/
    ├── kernel/
    │   ├── inbox/                   ← Root ír IDE
    │   └── outbox/                  ← Kernel válaszol
    ├── orchestrator/
    │   ├── inbox/
    │   └── outbox/
    ├── portal/
    │   ├── inbox/
    │   └── outbox/
    ├── joinery/
    │   ├── inbox/
    │   └── outbox/
    └── abstractions/
        ├── inbox/
        └── outbox/
```

---

## Leállítás és újraindítás

### Mindent leállítani

```bash
sd --stop              # Dispatcher leáll
tmux kill-server       # Összes session leáll
```

### Csak dispatcher újraindítás (session-ök maradnak)

```bash
sd --stop
sd --daemon
```

### Egy session újraindítása

```bash
tmux kill-session -t spaceos-kernel
# Manuálisan:
tmux new-session -d -s spaceos-kernel
tmux send-keys -t spaceos-kernel "cd /opt/spaceos/spaceos-kernel" Enter
sleep 1
tmux send-keys -t spaceos-kernel "claude --dangerously-skip-permissions -c" Enter
```

---

## Biztonsági megjegyzések

- A dispatcher **30 másodperces cooldown**-t tart terminálonként — nem küld dupla üzenetet
- Ha egy session **nem fut**, a dispatcher logol de nem próbál indítani — neked kell `sd --launch`
- **E2E és Infra szándékosan kizárva** — azokat mindig kézzel kezeled
- A `--dangerously-skip-permissions` flag a claude session-ökben van, nem a dispatcherben
- A dispatcher csak **tmux send-keys**-t használ — nem futtat kódot, nem módosít fájlokat

---

## Hibaelhárítás

| Probléma | Megoldás |
|---|---|
| `sd --status` → session NEM FUT | `sd --launch` |
| Permission denied a log-ra | Root-ként: `chown -R gabor:gabor /opt/spaceos/logs/dispatcher/` |
| Dispatcher nem észlel üzenetet | Az üzenet `status: UNREAD`? A fájl `.md` kiterjesztésű? |
| Session rossz könyvtárban indul | Ellenőrizd a TERMINALS map-et a script-ben |
| SSH megszakad, session elvész | Nem vész el! `tmux attach -t spaceos-kernel` |

---

## Terminál repo útvonalak

```
kernel       → /opt/spaceos/spaceos-kernel
orchestrator → /opt/spaceos/spaceos-orchestrator
portal       → /opt/spaceos/design-portal
joinery      → /opt/spaceos/spaceos-modules-joinery
abstractions → /opt/spaceos/spaceos-modules-abstractions
```

> A dispatcher script-ben a `TERMINALS` map-ben módosíthatók:
> `/opt/spaceos/tools/dispatcher/spaceos-dispatcher.sh`
