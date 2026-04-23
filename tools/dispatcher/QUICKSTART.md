# SpaceOS Dispatcher v2 — Gyors indítás

## 1. lépés: Telepítés a VPS-en

```bash
ssh spaceos
cd /opt/spaceos
unzip spaceos-dispatcher-v2.zip -d tools/dispatcher
bash tools/dispatcher/install.sh
source ~/.bashrc
```

## 2. lépés: Tmux session-ök indítása (egyszer)

```bash
ssh spaceos
sd --launch
```

Ez létrehozza:
```
spaceos-kernel        → /opt/spaceos/spaceos-kernel
spaceos-orch          → /opt/spaceos/spaceos-orchestrator
spaceos-portal        → /opt/spaceos/design-portal
spaceos-joinery       → /opt/spaceos/spaceos-modules-joinery
spaceos-abstractions  → /opt/spaceos/spaceos-modules-abstractions
```

Mindegyikben elindul: `claude --dangerously-skip-permissions -c`

## 3. lépés: VS Code-ból csatlakozás

Nyiss VS Code terminált, SSH-zz be, és attach-olj:

```bash
# Tab 1: Root (ez nem tmux — te kezeled közvetlenül)
ssh spaceos
cd /opt/spaceos
claude --dangerously-skip-permissions -c

# Tab 2: Kernel
ssh spaceos
tmux attach -t spaceos-kernel

# Tab 3: Orchestrator
ssh spaceos
tmux attach -t spaceos-orch

# Tab N: bármelyik másik
ssh spaceos
tmux attach -t spaceos-joinery
```

**Ami változik:** `cd ... && claude ...` helyett `tmux attach -t spaceos-XXX`
**Ami NEM változik:** a terminálban minden ugyanúgy néz ki és működik.

## 4. lépés: Dispatcher indítása

Nyiss egy plusz VS Code terminált:

```bash
ssh spaceos
sd --daemon
```

Vagy ha látni akarod a logot élőben:
```bash
sd-start     # Előtérben fut, Ctrl+C-vel állítod le
```

## Napi használat

```bash
# Reggel (VS Code-ban):
ssh spaceos
sd --status              # Mi fut, mi nem

# Ha valami nem fut:
sd --launch              # Újraindítja ami hiányzik

# Terminálokhoz csatlakozás:
tmux attach -t spaceos-kernel

# Élőben nézni a dispatchert:
sd-log                   # tail -f a log fájlra
```

## Tmux gyors referencia

| Parancs | Mit csinál |
|---|---|
| `tmux attach -t spaceos-kernel` | Csatlakozás session-höz |
| `Ctrl+B, D` | Leválás (session fut tovább!) |
| `Ctrl+B, S` | Session lista (navigálás) |
| `tmux ls` | Session-ök listája |
| `sd-ls` | Csak spaceos session-ök |

## Ha megszakad az SSH

Semmi baj — a session fut tovább a VPS-en.
Újracsatlakozás: `ssh spaceos && tmux attach -t spaceos-kernel`

## Hogyan működik a dispatch?

```
Te (root terminálban):
  → Írsz inbox üzenetet: docs/mailbox/kernel/inbox/...

Dispatcher (háttérben):
  → inotifywait észleli az új fájlt
  → tmux send-keys "Új feladat érkezett..." Enter
  → A kernel session-ben megjelenik, mintha te gépeltéd volna

Te (VS Code-ban):
  → tmux attach -t spaceos-kernel
  → Élőben látod ahogy dolgozik
  → Bármikor átveheted a kontrollt
```
