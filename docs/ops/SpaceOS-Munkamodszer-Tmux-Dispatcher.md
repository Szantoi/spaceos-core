# SpaceOS Dispatcher — Üzemeltetési kézikönyv

**Frissítve:** 2026-04-18  
**Script:** `/opt/spaceos/tools/dispatcher/spaceos-dispatcher.sh` (alias: `sd`)

---

## Koncepció

A VPS-en minden terminál egy-egy **tmux session**, amelyben Claude Code fut. A dispatcher ezeket a session-öket kezeli és automatikusan továbbítja az inbox/outbox üzeneteket.

```
Root (Claude)  →  inbox fájl  →  Dispatcher  →  tmux send-keys  →  Terminál (Claude)
Terminál (Claude)  →  outbox fájl  →  Dispatcher  →  tmux send-keys  →  Root (Claude)
```

**RAM-spórolás (8GB VPS):** a root session mindig fut, a többi on-demand — inbox érkezésekor indul, DONE/BLOCKED után 5 másodperccel leáll.

---

## Session-ök

| Terminál | tmux session | Könyvtár | Típus |
|---|---|---|---|
| root | `spaceos-root` | `/opt/spaceos` | persistent |
| kernel | `spaceos-kernel` | `spaceos-kernel/` | on-demand |
| orch | `spaceos-orch` | `spaceos-orchestrator/` | on-demand |
| portal | `spaceos-portal` | `design-portal/` | on-demand |
| joinery | `spaceos-joinery` | `spaceos-modules-joinery/` | on-demand |
| abstractions | `spaceos-abstractions` | `spaceos-modules-abstractions/` | on-demand |
| infra | `spaceos-infra` | `infra/` | on-demand |
| e2e | `spaceos-e2e` | `e2e/` | on-demand |
| cutting | `spaceos-cutting` | `spaceos-modules-cutting/` | on-demand |
| inventory | `spaceos-inventory` | `spaceos-modules-inventory/` | on-demand |
| procurement | `spaceos-procurement` | `spaceos-modules-procurement/` | on-demand |
| fe | `spaceos-fe` | `spaceos-doorstar-portal/` | on-demand |
| librarian | `spaceos-librarian` | `spaceos-librarian/` | on-demand |
| architect | `spaceos-architect` | `spaceos-architect/` | **persistent** |

**Tmux socket:** `/tmp/spaceos.tmux`

---

## Parancsok

### Session kezelés

```bash
sd --launch              # Root session indítása
sd --start <név>         # Egy terminál indítása (-c: folytatja az előzőt)
sd --refresh <név>       # Egy terminál fresh restart (nincs -c, új kontextus)
sd --refresh-all         # Minden session fresh restart:
                         #   futó → /exit + újraindítás
                         #   leállított → fresh start (RAM gate: 400MB min)
sd --stop-session <név>  # Egy terminál leállítása
sd --stop-all            # Minden projekt terminál leállítása (root megmarad)
sd --kill-all            # Minden session leállítása (root is)
```

### Dispatcher kezelés

```bash
sd                       # Dispatcher előtérben
sd --daemon              # Dispatcher háttérben
sd --stop                # Dispatcher leállítása
sd --status              # Mi fut, RAM, UNREAD-ek
sd --mode notify         # Mód: csak értesít (alapértelmezett)
sd --mode semi           # Mód: feldolgoz, megáll új task előtt
sd --mode auto           # Mód: teljes automata
```

### Tmux navigáció

```bash
tm attach -t spaceos-root    # Csatlakozás a root session-höz
tm ls                        # Session lista
# Ctrl+B, S                  # Interaktív session váltó
# Ctrl+B, D                  # Leválás (session fut tovább)
```

---

## Root módok

A dispatcher az outbox üzeneteket különbözőképpen adja át a root session-nek:

| Mód | Viselkedés |
|---|---|
| `notify` | Csak értesít: "nézd meg ezt az outbox-ot" — Gabor dönt mindenről |
| `semi` | Feldolgozza a DONE-t (ellenőriz, archivál, frissít), de megáll új task kiadása előtt |
| `auto` | Teljes automata: DONE → elfogadás → archive → következő task kiadás |

Módváltás bármikor: `sd --mode semi` — azonnal érvényes, a dispatcher fájlból olvassa (`tools/dispatcher/mode`).

---

## Refresh vs Start

| Parancs | Futó session | Nem futó session |
|---|---|---|
| `--start <név>` | — (már fut, kihagyja) | Indítja `-c`-vel (előző session folytatása) |
| `--refresh <név>` | `/exit` + újraindítás | Fresh start (`-c` nélkül) |

**Mikor melyiket:**
- `--start`: ha a terminál leállt és folytatni akarod ahol abbahagyta
- `--refresh`: ha friss kontextust akarsz (új sprint, szerepkör frissítés, reset)

### Automatikus "folytasd a munkát!" ← ÚJ 2026-04-18

Ha a dispatcher `-c` folytatással indít egy session-t, automatikusan elküldi:
```
folytasd a munkát!
```
Ez jelzi a Claude-nak, hogy vegye fel a kontextust és folytassa az előző feladatot. Manuális újraindításnál (`sd --start <név>`) is ez a teendő — a dispatcher most automatikusan elvégzi.

---

## Watchdog

### Outbox watchdog (terminál → root)

Ha egy outbox fájl UNREAD marad (a root nem dolgozza fel):

- 1. próba: 2 perc után
- 2. próba: 4 perc után  
- 3. próba: 8 perc után
- 3 kísérlet után megáll → `[watchdog] SKIP (max retry)` — emberi beavatkozás kell

### Inbox watchdog (root → terminál) ← ÚJ 2026-04-18

Ha egy inbox üzenet **5 percnél tovább UNREAD** marad (a terminál nem kezdte el olvasni), a dispatcher értesíti a root-ot:

```
⚠️ [inbox-watchdog] cutting terminál 312mp óta NEM olvasta: 018_bug004-post-500-deep.md — ellenőrizd a konzolt, kézzel kell Enter?
```

Típikus ok: az üzenet beíródott a terminál konzolába de az Enter nem ment el. Megoldás: `tm attach -t spaceos-<név>` → Enter kézzel.

Max 1 értesítés / fájl (nem spammeli a root-ot).

---

## Ismert quirks és megoldások

### Enter nem regisztrálódik (konzolba írt de nem küldött üzenet)

**Tünet:** A dispatcher beírja az üzenetet a terminál konzolába, de az Enter nem megy el — a terminál nem dolgozza fel.

**Ok:** Race condition: a tmux `send-keys` a szöveg után azonnal küldte az Enter-t, mielőtt a Claude Code readline készen lett volna.

**Fix (2026-04-18):** `sleep 0.5` a szöveg és az Enter között minden dispatch hívásban. Ha manuálisan tapasztalod: `tm attach -t spaceos-<név>` → Enter kézzel.

---

## E2E rate limit

Két E2E suite futtatás között legalább **60 másodperc** szükséges. A Kernel 100 req/min fixed window limitje kimerül ha egymás után futtatod.

```bash
npm test          # 1. futtatás
# várj 60+ másodpercet
npm test          # 2. futtatás — helyes eredmény
```

---

## Fájl helyek

```
/opt/spaceos/tools/dispatcher/
├── spaceos-dispatcher.sh    # Dispatcher script (sd alias)
└── mode                     # Aktuális mód: notify | semi | auto

/opt/spaceos/logs/dispatcher/
└── dispatcher.log           # Élő log: sd-log alias

/tmp/spaceos.tmux            # Tmux socket (SSH-t túléli)
/etc/sudoers.d/gabor-spaceos # Infra sudo jogok (jelszó nélkül)
```

---

## Napi rutin

```bash
# SSH után
sd-ls                          # Élnek a session-ök? (tmux túléli az SSH-t)
sd --launch                    # Ha root nem fut → indítás
sd --daemon                    # Dispatcher háttérben
sd --status                    # Állapot ellenőrzés

# Munka közben
tm attach -t spaceos-root      # Root koordinátor
sd --mode semi                 # Félautomata mód

# Nap végén
sd --stop                      # Dispatcher leállítása
# A tmux session-öket NE állítsd le — futnak tovább
```
