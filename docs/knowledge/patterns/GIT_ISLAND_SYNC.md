# Git-alapú Sziget Szinkronizáció

> **Dátum:** 2026-07-11
> **Státusz:** APPROVED
> **Döntés:** Multi-repo + Session-based sync

---

## Architektúra

### GitHub Repo Struktúra

```
github.com/spaceos/
├── spaceos-core           ← SpaceOS sziget (root, conductor)
├── nexus-core             ← Nexus sziget (infra, monitoring)
├── joinerytech-platform   ← JoineryTech sziget (platform dev)
└── doorstar-instance      ← Doorstar sziget (ügyfél instance)
```

### VPS Mapping

| Sziget | VPS Path | GitHub Repo |
|--------|----------|-------------|
| SpaceOS | `/opt/spaceos/` | `spaceos/spaceos-core` |
| Nexus | `/opt/nexus/` | `spaceos/nexus-core` |
| JoineryTech | `/opt/joinerytech/` | `spaceos/joinerytech-platform` |
| Doorstar | `/opt/doorstar/` | `spaceos/doorstar-instance` |

---

## Session-based Sync Workflow

### 1. Session Start → Git Pull

```bash
# Terminál session indításkor
cd /opt/<sziget>/
git fetch origin
git pull --rebase origin main
```

**Mikor:** Minden terminál session indításakor
**Cél:** Friss kód, mások változásai bekerülnek

### 2. Session End (DONE) → Git Push

```bash
# DONE outbox írás után
cd /opt/<sziget>/
git add -A
git commit -m "feat(<terminal>): <task summary>

MSG-<TERMINAL>-<NNN> DONE

Co-Authored-By: Claude <noreply@anthropic.com>"
git push origin main
```

**Mikor:** Terminál DONE outbox ír
**Cél:** Változások megosztása más terminálokkal/szigetekkel

---

## Implementáció

### Session Starter Hook (Pull)

```typescript
// knowledge-service/src/sessionStarter.ts

async function startSession(terminal: string) {
  const islandPath = getIslandPath(terminal);

  // Git pull before session
  await exec(`cd ${islandPath} && git pull --rebase origin main`);

  // Start Claude session
  await startClaudeSession(terminal);
}
```

### DONE Handler Hook (Push)

```typescript
// knowledge-service/src/pipeline/watchDone.ts

async function handleDone(message: Message) {
  // Process DONE...

  // Git commit + push
  const islandPath = getIslandPath(message.from);
  await exec(`cd ${islandPath} && git add -A`);
  await exec(`cd ${islandPath} && git commit -m "feat(${message.from}): ${message.subject}"`);
  await exec(`cd ${islandPath} && git push origin main`);
}
```

---

## Konfliktus Kezelés

### Automatikus Rebase

```bash
git pull --rebase origin main
```

Ha konfliktus van:
1. Session starter **BLOCKED** státuszt ad
2. Root/Conductor értesítést kap
3. Manuális resolve szükséges

### Konfliktus Megelőzés

- Terminálok **különböző fájlokon** dolgoznak (inbox/outbox szeparáció)
- Ugyanazon fájlon ritkán dolgozik 2 terminál egyszerre
- Federation üzenetek **nem Git-en** mennek (file-based marad)

---

## Mi Megy Git-re, Mi Nem

### GIT-RE MEGY ✅

- Forráskód (`src/`, `scripts/`)
- Dokumentáció (`docs/`)
- Konfigurációk (`config/`)
- CLAUDE.md fájlok
- Tervek, ADR-ek

### NEM MEGY GIT-RE ❌

- `terminals/*/inbox/` — Federation üzenetek (file-based)
- `terminals/*/outbox/` — Federation üzenetek
- `logs/` — Naplók
- `node_modules/`, `dist/` — Build artifacts
- `.env` — Secrets

### .gitignore

```gitignore
# Federation messages (file-based, not Git)
terminals/*/inbox/
terminals/*/outbox/
terminals/*/archive/

# Build & runtime
node_modules/
dist/
logs/
*.log

# Secrets
.env
.env.*
*.pem
*.key
```

---

## Szigetek Közötti Kommunikáció

### Kód Megosztás → Git

```
JoineryTech push → GitHub → Doorstar pull
```

### Üzenet Küldés → Federation (marad file-based)

```
SpaceOS outbox → federation-router → Nexus inbox
```

**A kettő különválik:**
- **Git:** Kód, dokumentáció, konfiguráció
- **Federation:** Task üzenetek, DONE/BLOCKED, koordináció

---

## Lépések a Bevezetéshez

1. **GitHub repók létrehozása** (4 db)
2. **Meglévő kód push** minden szigetről
3. **.gitignore** beállítás (inbox/outbox kizárás)
4. **Session hooks** implementálás (pull/push)
5. **Tesztelés** egy terminállal

---

_SpaceOS Root — Git Island Sync Architecture — 2026-07-11_
