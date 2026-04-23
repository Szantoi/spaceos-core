# SpaceOS — Local Dev Testing Workflow
## Playwright MCP a VPS Vite dev server ellen (SSH tunnel)

> **Verzió:** v1.0 — 2026-04-19
> **Státusz:** AKTÍV — napi fejlesztői workflow
> **Cél:** Playwright MCP tesztelés a **lokális gépen** futva, **VPS-en futó dev buildet** célozva — a publikált `joinerytech.hu` prod oldal helyett
> **Kapcsolódó:** `SpaceOS_Exploratory_Testing_Tooling_v1.md` (Playwright MCP setup alapok), `SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4.1_Amendment.md` (dispatcher modell)

---

## 1. Miért ez a setup

Három célpont lehetséges Playwright MCP teszteléshez:

| Célpont | URL | Mikor |
|---|---|---|
| **Prod** | `https://joinerytech.hu` | Soft Launch smoke test, deployed verifikáció |
| **Dev (VPS, SSH tunnel)** | `http://localhost:5173` | **Ez a dokumentum** — napi dev, még nem deployolt kód |
| **Lokális full stack** | `http://localhost:5173` + lokális BFF+Kernel | Offline munka, jövőbeli opció |

Az **Opció 1 (SSH tunnel)** a választott modell, mert:
- FE forráskód a VPS-en marad (dispatcher `spaceos-fe` session érintetlen)
- Nincs kettős repo szinkronban tartás
- Keycloak redirect URI már konfigurált (`http://localhost:5173/*`)
- Prod ↔ dev váltás a Playwright MCP prompt-ban egy URL csere

---

## 2. Architektúra — egy képben

```
┌─────────────────────────────────┐         ┌─────────────────────────────────┐
│  Lokális gép (Windows PC)       │         │  VPS (109.122.222.198)          │
│                                 │         │                                 │
│  Playwright MCP                 │         │  spaceos-fe tmux session        │
│    └─ Chromium (headless)       │         │    └─ pnpm dev                  │
│         └─ http://localhost:5173├─────────┤       └─ Vite @ 127.0.0.1:5173  │
│                                 │   SSH   │                                 │
│  PowerShell #1:                 │  tunnel │  /opt/spaceos/                  │
│    ssh -L 5173:127.0.0.1:5173   │         │    spaceos-doorstar-portal/     │
│                                 │         │                                 │
└─────────────────────────────────┘         └─────────────────────────────────┘

                BFF hívások a dev build-ből:
                http://localhost:5173 → tunnel → VPS Vite → prod BFF (Orchestrator)
                                                         https://joinerytech.hu/bff
```

A Vite dev server **loopback-only** (`127.0.0.1:5173`), nincs publikusan expose-olva. Az SSH tunnel az egyetlen bejárat.

---

## 3. One-time setup

### 3.1 VPS oldal (gabor user, NEM root)

#### Vite config — `vite.config.ts`

```ts
export default defineConfig({
  // ...
  server: {
    host: '127.0.0.1',        // SEC: loopback-only, SSH tunnelen keresztül
    port: 5173,
    strictPort: true,
    watch: {
      ignored: [
        '**/mailbox/**',       // dispatcher IPC, nem source
        '**/docs/**',
        '**/*.md',
      ],
      followSymlinks: false,   // ELOOP hard-stop
    },
  },
});
```

#### `.gitignore` bővítés

```
mailbox/
```

A `mailbox/` a dispatcher symlink target, nem része a forráskódnak.

#### Keycloak client (`portal-app`) — már konfigurálva

A v4.1 Amendment §4 szerint:

```
Valid Redirect URIs:
  http://localhost:5173/*    ← dev
  https://joinerytech.hu/*   ← prod
  https://portal.joinerytech.hu/*
  https://asztalostech.hu/*

Web Origins:
  http://localhost:5173
  https://joinerytech.hu
  https://asztalostech.hu
  https://portal.joinerytech.hu
```

Ha nincs ott a `http://localhost:5173/*` → KC admin console → Clients → portal-app → hozzáadni.

### 3.2 Lokális gép (Windows)

#### SSH config — `C:\Users\szant\.ssh\config`

```
Host spaceos-dev
  HostName 109.122.222.198
  User gabor
  LocalForward 5173 127.0.0.1:5173
```

#### SSH kulcs (jelszó-mentes belépéshez, opcionális de ajánlott)

```powershell
# Ha még nincs kulcs:
ssh-keygen -t ed25519

# Publikus kulcs feltöltése:
type $env:USERPROFILE\.ssh\id_ed25519.pub | ssh gabor@109.122.222.198 "cat >> ~/.ssh/authorized_keys"
```

#### Playwright MCP — regisztrált

`SpaceOS_Exploratory_Testing_Tooling_v1.md §3` alapján:

```bash
claude mcp add playwright -s user -- npx @playwright/mcp@latest --browser chromium --headless
```

---

## 4. Napi workflow — 3 ablak

### 4.1 Ablak A — VPS spaceos-fe session

```bash
ssh spaceos
tm attach -t spaceos-fe
# (vagy sd-ls ha nem él, majd sd --launch-all)
cd /opt/spaceos/spaceos-doorstar-portal
pnpm dev
```

Várt output:
```
VITE v8.0.8  ready in 327 ms
➜  Local:   http://127.0.0.1:5173/
```

**Ezt az ablakot hagyd futni egész nap.**

### 4.2 Ablak B — Lokális SSH tunnel

```powershell
ssh -N spaceos-dev
```

Jelszó / kulcs után: **némán áll**. Ez a jó. `-N` = no command, csak a tunnel.

**Ezt az ablakot is hagyd futni** amíg tesztelsz. `Ctrl+C` zárja.

### 4.3 Ablak C — Playwright MCP / böngésző

Böngésző smoke-teszt:
```
http://localhost:5173
```
Keycloak login screen → OK.

Playwright MCP prompt:
```
Use playwright mcp --browser chromium --headless to open http://localhost:5173
(Vite dev server via SSH tunnel — NOT the prod joinerytech.hu site).
Perform Keycloak login with test-admin.
After successful login, save storage state to /tmp/auth-state-local.json.
```

---

## 5. Prod vs. dev célpont — váltási mátrix

| Cél | Base URL | Storage state | Parancs előtag |
|---|---|---|---|
| Prod smoke | `https://joinerytech.hu` | `/opt/spaceos/tester/auth-state.json` (VPS) | `Use playwright mcp to open https://joinerytech.hu...` |
| Dev (tunnel) | `http://localhost:5173` | `/tmp/auth-state-local.json` (lokális) | `Use playwright mcp to open http://localhost:5173...` |

A storage state fájlok **nem cserélhetők** — origin-specifikus cookie-k és tokenek.

---

## 6. Hibaelhárítás

| Tünet | Valószínű ok | Fix |
|---|---|---|
| `ERR_PNPM_NO_IMPORTER_MANIFEST_FOUND` | Rossz WD | `cd /opt/spaceos/spaceos-doorstar-portal` |
| `ELOOP: too many symbolic links` | Mailbox symlink loop | `vite.config.ts watch.ignored + followSymlinks: false` (már beépítve) |
| SSH tunnel: `channel 3: open failed: Connection refused` | Vite nem fut a VPS-en | Ablak A-ban `pnpm dev` él? Output mutasson `127.0.0.1:5173`-at |
| SSH: `bind [127.0.0.1]:5173: Address already in use` | Másik tunnel/process foglalja | Windows: `netstat -ano \| findstr :5173` → taskkill |
| Böngésző: `ERR_CONNECTION_REFUSED` | Tunnel nem él vagy Vite `0.0.0.0`-n | VPS: `ss -tlnp \| grep 5173` mutasson `127.0.0.1:5173`-at |
| Keycloak: `Invalid redirect_uri` | `http://localhost:5173/*` hiányzik a portal-app client-ből | KC admin console → Clients → portal-app → Valid Redirect URIs |
| Playwright MCP prod-ot nyit nem dev-et | Prompt-ban hardcode URL | Explicit: `open http://localhost:5173 (NOT joinerytech.hu)` |

### Sanity commands

```bash
# VPS — Vite a jó interfészen hallgat?
ss -tlnp | grep 5173
# Várt: LISTEN  0  511  127.0.0.1:5173  *:*

# VPS — symlink loop nincs?
find /opt/spaceos/spaceos-doorstar-portal/mailbox -maxdepth 5 -type l -printf '%p -> %l\n'

# Lokális — tunnel él?
# Ablak B-ben: némán áll ← OK
# Ablak C-ben: curl http://localhost:5173 → HTML válasz
```

---

## 7. Mi lett megjavítva ebben a session-ben (2026-04-19)

| Probléma | Fix | Státusz |
|---|---|---|
| Symlink loop: `portal/mailbox/fe/fe → docs/mailbox/fe/fe → önmaga` | `rm portal/mailbox/fe/fe` + docs oldalon tiszta | ✅ |
| Vite FSWatcher ELOOP a mailbox symlinkeken | `watch.ignored + followSymlinks: false` | ✅ |
| Mailbox a git working tree-ben | `.gitignore: mailbox/` | ✅ |
| Vite dev server default host | Explicit `127.0.0.1`, nem `0.0.0.0` | ✅ |

---

## 8. Arch-drift — flag későbbi rendezésre

**Nem blokkoló, de Doorstar Soft Launch előtt rendezendő.** A v4.1 Amendment §3.5 szerint más a kanonikus útvonal:

| Jelenlegi | Amendment szerint | Következmény |
|---|---|---|
| `/opt/spaceos/spaceos-doorstar-portal/` (source) | `~/spaceos-doorstar-portal/` | Prod `/opt/spaceos/` source + dist keveredés; `git pull` a deploy dir mellett kockázatos |
| `/opt/spaceos/docs/mailbox/` | `~/spaceos-docs/mailbox/` | Dispatcher WD-constraint és CLAUDE.md path inkonzisztens |
| Mailbox symlink a FE repo-n BELÜL | Mailbox repo-n KÍVÜL, dispatcher külső path-ra figyel | Vite watch folyamatosan veszélyeztetett (ezért kellett most a fix) |

**Javasolt clean arch megoldás:**
- Mailbox külön filesystem path-on (`~/spaceos-docs/mailbox/` v. `/opt/spaceos/docs/mailbox/` — döntés)
- FE repo-ban nincs `mailbox/` (akár symlinkként sem)
- CLAUDE.md abszolút útvonallal hivatkozik az inbox/outbox-ra
- `.gitignore mailbox/` + `vite watch ignored` marad mint safety net

**Külön session-ben rendezni, nem most.**

---

## 9. Command cheat sheet

```bash
# === VPS — spaceos-fe session ===
ssh spaceos
tm attach -t spaceos-fe
cd /opt/spaceos/spaceos-doorstar-portal
pnpm dev
# Detach: Ctrl+B, D

# === Lokális — SSH tunnel (PowerShell) ===
ssh -N spaceos-dev                                   # ha van ~/.ssh/config alias
ssh -L 5173:127.0.0.1:5173 -N gabor@109.122.222.198  # explicit változat
# Bezár: Ctrl+C

# === Lokális — smoke test ===
start http://localhost:5173                          # Windows
curl http://localhost:5173                           # parancssor

# === Playwright MCP — dev target ===
# Claude Code promptban:
# Use playwright mcp to open http://localhost:5173, login as test-admin...

# === Playwright MCP — prod target (régi workflow) ===
# Use playwright mcp to open https://joinerytech.hu, login as test-admin...
```

---

## 10. Változtatási összefoglaló

| File / Target | Változás | Ok |
|---|---|---|
| `vite.config.ts` | `server.host='127.0.0.1'` + `watch.ignored` + `followSymlinks: false` | SEC (loopback), ELOOP prevent |
| `.gitignore` | `mailbox/` | Dispatcher IPC ne menjen git alá |
| `portal/mailbox/fe/fe` | Törölve | ELOOP oka |
| `C:\Users\szant\.ssh\config` | `Host spaceos-dev` + `LocalForward 5173` | Jelszó-mentes tunnel alias |
| Keycloak `portal-app` client | `http://localhost:5173/*` redirect URI (már létezett) | Dev origin auth |
| `/tmp/auth-state-local.json` | Új, első dev login után | Origin-specifikus storage state |
| Playwright MCP prompt template | Dev variant hozzáadva | Prod ↔ dev váltás explicit |

---

*SpaceOS — Local Dev Testing Workflow v1.0 · 2026-04-19*
*Státusz: AKTÍV · Architektúra-drift: §8 flagelve, Soft Launch előtt rendezendő*
*Kapcsolódó: SpaceOS_Exploratory_Testing_Tooling_v1.md · SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4.1_Amendment.md*
