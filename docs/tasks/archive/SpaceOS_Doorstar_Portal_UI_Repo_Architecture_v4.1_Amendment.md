# SpaceOS — Doorstar Portal UI Repo Architecture
## v4.1 Amendment: Tmux Dispatcher Integráció

> **Verzió:** v4.1 — 2026-04-16
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ — v4 + jelen amendment együtt érvényes
> **Kontextus:** A v4 `fe-dev` izolált Linux user modellje elengedve. Helyette a SpaceOS tmux dispatcher workflow-ba integrált `spaceos-fe` session, `gabor` user alatt, CLAUDE.md instrukciós izolációval.
> **Előzmény:** SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4.md + SpaceOS-Munkamodszer-Tmux-Dispatcher.md

---

## 1. Mi változik a v4-hez képest

| v4 szekció | Változás típusa | Tartalom |
|---|---|---|
| §2 D-02 | **FELÜLÍRVA** | `fe-dev` user → `spaceos-fe` tmux session, gabor user, CLAUDE.md izoláció |
| §2 D-07 | **TÖRÖLVE** | Külön `doorstar-portal-dev` KC client nem szükséges — gabor user a `portal-app` clientet használja |
| §3.2 | **FELÜLÍRVA** | Fejlesztői topology: nincs fe-dev user |
| §7.1 | **EGYSZERŰSÖDIK** | Egy Keycloak client elég (portal-app + localhost redirect URI) |
| §7.2 | **EGYSZERŰSÖDIK** | Nincs isDev client switching |
| §8 (teljes) | **FELÜLÍRVA** | VPS fe-dev → tmux dispatcher integráció |
| §9 CLAUDE.md | **BŐVÜL** | Dispatcher-specifikus instrukciók + working directory constraint |
| §11.1 | **FELÜLÍRVA** | SSH tunnel → tmux reattach |
| §14 VPS gates | **FELÜLÍRVA** | fe-dev checklistek → dispatcher checklistek |
| §16 Nap 1 | **FELÜLÍRVA** | fe-dev setup → dispatcher session setup |

| v4 szekció | Változás | Tartalom |
|---|---|---|
| §4 | Változatlan | FE repo struktúra |
| §5 | Változatlan | OpenAPI contract pipeline |
| §6 | Változatlan | BFF API surface |
| §7.3-7.5 | Változatlan | Bootstrap sorrend, token claim-ek, .env |
| §10 | Változatlan | CONTRACT_ISSUES.md protokoll |
| §12 | Változatlan | Nginx konfiguráció |
| §13 | Változatlan | Test strategy placeholder |
| §15 | Változatlan | Roadmap |

### Érintett findingok

| Finding | Státusz | Indoklás |
|---|---|---|
| SEC-UI-01 (iptables chain) | **TÖRÖLVE** — nem releváns | Nincs fe-dev user, nincs iptables |
| SEC-UI-02 (InMemoryWebStorage) | Változatlan — továbbra is érvényes | Token storage az app kérdése, nem a user-é |
| SEC-UI-04 (rendszerfájl olvasás) | **TÖRÖLVE** — gabor user mindent lát | Instrukciós tiltás marad a CLAUDE.md-ben |
| SEC-UI-05 (KC client szétválasztás) | **TÖRÖLVE** | Egy client elég, gabor user |
| SEC-UI-07 (KC 8080 port) | **TÖRÖLVE** | Nincs iptables |
| SEC-UI-10 (SSH exfiltration) | **TÖRÖLVE** | Gabor user, nem releváns |

**Javított finding számok:** v4 17 finding → **v4.1: 11 finding marad érvényes** (0C + 4H + 7M; a 2 CRITICAL iptables-specifikus volt).

---

## 2. D-02 FELÜLÍRT döntés

**Korábbi (v4):** `fe-dev` izolált Linux user — filesystem chmod + iptables REJECT.

**Új (v4.1):** `spaceos-fe` tmux session a dispatcher rendszerben, `gabor` user alatt. Az izoláció **instrukciós** (CLAUDE.md + working directory), nem fizikai. Ez elfogadható mert:
- A Claude Code agent kooperatív — nem adversarial threat model
- A dispatcher workflow az egyetlen standard fejlesztési mód a SpaceOS-ben
- A Contract-First elv (deployed API) és a WD constraint funkcionálisan ekvivalens
- A fizikai izoláció overheadje (cross-user tmux, socket permissions, dual SSH) nem indokolt

---

## 3. Tmux dispatcher integráció (§8 felülírása)

### 3.1 A `spaceos-fe` session helye

```
/tmp/spaceos.tmux (közös tmux socket, gabor)
│
├── spaceos-root          ← Root koordinátor
├── spaceos-kernel        ← @ ~/spaceos-kernel/
├── spaceos-orch          ← @ ~/spaceos-orchestrator/
├── spaceos-portal        ← @ ~/spaceos-design-portal/ (befagyasztva)
├── spaceos-joinery       ← @ ~/spaceos-modules-joinery/
├── spaceos-abstractions  ← @ ~/spaceos-modules-abstractions/
├── spaceos-infra         ← @ ~/spaceos-infra/
├── spaceos-e2e           ← @ ~/spaceos-e2e/
└── spaceos-fe            ← ÚJ @ ~/spaceos-doorstar-portal/
```

### 3.2 Session létrehozás (sd --launch-all bővítés)

A dispatcher `spaceos-dispatcher.sh` fájlban új session hozzáadása:

```bash
# Új session a launch-all-ban:
tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-fe \
  -c /home/gabor/spaceos-doorstar-portal
```

### 3.3 Mailbox struktúra

```
~/spaceos-docs/mailbox/
├── kernel/
│   ├── inbox/
│   └── outbox/
├── orch/
│   ├── inbox/
│   └── outbox/
├── ...
└── fe/                    ← ÚJ
    ├── inbox/             ← Root ír ide task-ot
    └── outbox/            ← FE Claude Code DONE-t ír ide
```

### 3.4 Dispatch flow (FE-specifikus)

**Inbox (root → FE):**
```
1. Root session (notify/semi/auto mód):
   docs/mailbox/fe/inbox/2026-04-17_001_scaffold-react-app.md
   (status: UNREAD, priority: high, type: task)

2. Dispatcher inotifywait észleli

3. tmux send-keys spaceos-fe:
   "Új feladat érkezett. Olvasd el: mailbox/fe/inbox/2026-04-17_001_scaffold-react-app.md"

4. FE Claude Code feldolgozza (WORKFLOW.md pipeline: READ → CODE → BUILD → TEST → REVIEW → OUTBOX)
```

**Outbox (FE → root):**
```
1. FE Claude Code outbox-ba ír:
   docs/mailbox/fe/outbox/2026-04-17_001_scaffold-react-app-done.md

2. Dispatcher észleli → mód alapján értesíti root-ot

3. Root (notify módban): "Nézd meg: [fe] DONE: scaffold-react-app"
   Root (semi módban): ellenőriz, archivál
   Root (auto módban): elfogad + következő task kiadás
```

### 3.5 Working directory constraint

A `spaceos-fe` session **mindig** a `~/spaceos-doorstar-portal/` working directory-ben indul. A CLAUDE.md explicit tiltja a WD-n kívüli fájlműveleteket. A dispatcher `--launch-all` ezt garantálja.

**Ellenőrzés (root session-ből):**
```bash
tmux -S /tmp/spaceos.tmux send-keys -t spaceos-fe "pwd" Enter
# → /home/gabor/spaceos-doorstar-portal ✅
```

---

## 4. Keycloak egyszerűsítés (§7.1 felülírása)

Mivel gabor user fut, nincs szükség külön dev Keycloak clientre. A `portal-app` client bővül:

```
Valid Redirect URIs:
  https://joinerytech.hu/*              ← meglévő
  https://asztalostech.hu/*             ← meglévő
  https://portal.joinerytech.hu/*       ← ÚJ: deployed Doorstar portal
  http://localhost:5173/*               ← ÚJ: Vite dev server

Web Origins:
  https://joinerytech.hu
  https://asztalostech.hu
  https://portal.joinerytech.hu         ← ÚJ
  http://localhost:5173                  ← ÚJ
```

A `keycloak.config.ts` egyszerűsödik (nincs isDev branching):

```typescript
// src/auth/keycloak.config.ts — v4.1 egyszerűsített
import { UserManager, InMemoryWebStorage } from 'oidc-client-ts';

export const userManager = new UserManager({
  authority: import.meta.env.VITE_KEYCLOAK_URL
    + '/realms/' + import.meta.env.VITE_KEYCLOAK_REALM,
  client_id: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,  // 'portal-app'
  redirect_uri: window.location.origin + '/callback',
  post_logout_redirect_uri: window.location.origin,
  response_type: 'code',
  scope: 'openid profile spaceos-tenant-scope',
  automaticSilentRenew: true,
  silentRequestTimeoutInSeconds: 10,
  // SEC-UI-02: EXPLICIT in-memory — sessionStorage TILOS
  userStore: new InMemoryWebStorage(),
  stateStore: new InMemoryWebStorage(),
});
```

---

## 5. CLAUDE.md bővítés (§9 kiegészítése)

A v4 CLAUDE.md teljes tartalma **érvényes marad**, plusz az alábbi dispatcher-specifikus blokk:

```markdown
## Dispatcher Integration

This session runs inside the SpaceOS tmux dispatcher system as `spaceos-fe`.
You receive tasks via mailbox/fe/inbox/ and report results to mailbox/fe/outbox/.

### Working Directory Constraint

Your working directory is ~/spaceos-doorstar-portal/. You MUST NOT:
- cd to or read files from ~/spaceos-kernel/, ~/spaceos-orchestrator/,
  ~/spaceos-modules-*/, ~/spaceos-infra/, ~/spaceos-e2e/, or any other
  SpaceOS backend repository
- Run psql, systemctl, journalctl, or any system administration command
- Access /opt/spaceos/ (production deploy directory)
- Access /etc/spaceos/ (production secrets)

These directories are technically readable (you run as gabor user), but accessing
them violates the Contract-First architecture. If you need backend information,
file a CONTRACT_ISSUES.md entry.

### Mailbox Protocol

Follow the standard WORKFLOW.md pipeline:
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

When you complete a task:
1. Run all checks: pnpm build && pnpm test && pnpm lint && pnpm typecheck
2. Write DONE message to mailbox/fe/outbox/ with results summary
3. Wait for next inbox message — do NOT start unsolicited work

### Cross-Project Communication

If you need something from the backend (new endpoint, bug fix, contract clarification):
1. Write it in CONTRACT_ISSUES.md in the FE repo
2. Mention it in your outbox DONE message: "CONTRACT_ISSUE: CI-NNN filed"
3. The root coordinator will bridge it to the appropriate backend terminal
Do NOT write directly to other project mailboxes (kernel/inbox/, orch/inbox/, etc.)
```

---

## 6. Dev loop felülírás (§11.1)

### Fejlesztés indítása (dispatcher modell)

```bash
# Reggel — Gabor SSH-zik VPS-re:
ssh spaceos
sd-ls                              # Session-ök élnek?
sd --launch-all                    # Ha nem → újraindítás (9 session, beleértve spaceos-fe)
sd --daemon                        # Dispatcher indítás

# FE fejlesztés:
tm attach -t spaceos-fe            # Csatlakozás a FE session-höz
# Ctrl+B, S                        # Váltás más session-re (root, kernel, stb.)

# Vite dev szerver:
# A spaceos-fe session-ben a Claude Code futtatja: pnpm dev
# Gábor laptop böngészőben:
ssh -L 5173:localhost:5173 gabor@109.122.222.198
# → http://localhost:5173
```

### "Bárhonnan" — tmux előnye

| Helyzet | Parancs |
|---|---|
| SSH megszakadt, újracsatlakozás | `ssh spaceos && tm attach -t spaceos-fe` — minden ott van ahol hagytad |
| Root → FE → Kernel váltás | `Ctrl+B, S` → session selector (9 session egy helyen) |
| Bemutatás ügyfélnek | `portal.joinerytech.hu` — deployolt build |
| FE task kiadás root-ból | Inbox fájl → dispatcher automatikusan kézbesíti |

---

## 7. Definition of Done felülírás (§14 VPS gates)

### VPS gates (v4.1 — dispatcher modell)

Törölve:
- ~~fe-dev user létrehozva~~
- ~~chmod 750~~
- ~~iptables FE_DEV_RESTRICT chain~~
- ~~iptables-persistent~~
- ~~nvm/pnpm fe-dev home~~
- ~~GitHub SSH deploy key (fe-dev)~~
- ~~Ellenőrző mátrix (§8.7)~~

Helyette:

- [ ] `spaceos-fe` tmux session létezik a dispatcher-ben
- [ ] `sd --launch-all` tartalmazza a `spaceos-fe` session-t, WD: `~/spaceos-doorstar-portal/`
- [ ] `~/spaceos-docs/mailbox/fe/inbox/` és `outbox/` könyvtárak létrehozva
- [ ] Dispatcher inotifywait a `fe/inbox/` és `fe/outbox/` mappákon
- [ ] CLAUDE.md tartalmazza a dispatcher + WD constraint blokkot (Section 5)
- [ ] Teszt: root inbox-ból küldött üzenet megjelenik a spaceos-fe session-ben
- [ ] Teszt: spaceos-fe outbox DONE üzenet megjelenik a root session-ben

### Auth gates egyszerűsítés

Törölve:
- ~~`doorstar-portal-dev` Keycloak client~~

Helyette:
- [ ] `portal-app` client: `http://localhost:5173/*` redirect URI hozzáadva
- [ ] `portal-app` client: `http://localhost:5173` Web Origins hozzáadva

---

## 8. Implementációs csomag felülírás (§16 Nap 1)

| Nap | Feladat | Track | Felelős |
|-----|---------|-------|---------|
| 1 | GitHub: `spaceos-doorstar-portal` repo létrehozás | Infra | Gábor |
| 1 | `spaceos-dispatcher.sh`: spaceos-fe session hozzáadás | Infra | Gábor |
| 1 | `~/spaceos-docs/mailbox/fe/{inbox,outbox}/` könyvtárak | Infra | Gábor |
| 1 | Keycloak: portal-app client redirect URI + Web Origins bővítés | Infra | Gábor |
| 1 | `sd --launch-all && sd --daemon` → spaceos-fe session él | Infra | Gábor |
| 2-3 | FE scaffold (v4 §16 agent utasítás — változatlan) | FE | FE Claude Code (spaceos-fe) |
| 4 | Nginx + Let's Encrypt + első deploy (v4 §16 — változatlan) | Infra | Gábor |

### Agent utasítás módosítás

A v4 §16 agent utasítás **érvényes marad**, de a bevezető mondat változik:

> ~~"SSH-zz be `fe-dev@vps`-ként."~~
>
> "Ez a spaceos-fe tmux session. A working directory: `~/spaceos-doorstar-portal/`.
> Olvasd el a CLAUDE.md fájlt — az összes constraint ott van.
> A mailbox protokollt kövesd: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX."

---

## 9. Összefoglaló — v4 + v4.1 együtt érvényes

A v4 dokumentum **teljes egészében érvényes** az alábbi kivételekkel:

| v4 tartalom | v4.1 hatás |
|---|---|
| D-02 (fe-dev user) | → D-02 (tmux session, CLAUDE.md izoláció) |
| D-07 (KC dev client) | → TÖRÖLVE |
| §3.2 fejlesztői topology | → Section 3 itt |
| §7.1 KC client szétválasztás | → Section 4 itt (egy client) |
| §7.2 oidc-client-ts isDev branch | → Section 4 itt (nincs branch) |
| §8 teljes (VPS fe-dev) | → Section 3+5 itt (dispatcher + CLAUDE.md) |
| §9 CLAUDE.md | → v4 tartalom + Section 5 itt (dispatcher blokk) |
| §11.1 dev loop | → Section 6 itt (tmux reattach) |
| §14 VPS gates | → Section 7 itt |
| §16 Nap 1 | → Section 8 itt |
| SEC-UI-01, 04, 05, 07, 10 | → TÖRÖLVE (fe-dev specifikusak voltak) |
| Minden más szekció | → **VÁLTOZATLAN** (§4, §5, §6, §7.3-7.5, §10, §12, §13, §15) |

---

*SpaceOS — Doorstar Portal UI Repo Architecture v4.1 Amendment · 2026. április 16.*
*Tmux dispatcher integráció · fe-dev user elengedve · CLAUDE.md instrukciós izoláció*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — v4 + v4.1 együtt érvényes*
