# Kód Költöztetési Terv — 4-Sziget Architektúra

**Státusz:** PLANNING
**Létrehozva:** 2026-07-11

---

## Jelenlegi Kód Struktúra (`/opt/spaceos/`)

```
/opt/spaceos/
├── backend/                    ← .NET backend (Kernel)
├── frontend/                   ← React frontend
├── datahaven-web/              ← Dashboard
├── datahaven-telegram/         ← Telegram bot
├── spaceos-nexus/              ← Knowledge-service, MCP
├── spaceos-conductor/          ← Conductor logika
├── spaceos-architect/          ← Architect docs
├── spaceos-librarian/          ← Librarian docs
├── spaceos-modules-*/          ← JoineryTech modulok
│   ├── spaceos-modules-crm/
│   ├── spaceos-modules-hr/
│   ├── spaceos-modules-ehs/
│   ├── spaceos-modules-qa/
│   ├── spaceos-modules-dms/
│   └── spaceos-modules-maintenance/
├── infra/                      ← Infra konfigok
├── scripts/                    ← Bash szkriptek
└── docs/                       ← Dokumentáció
```

---

## Költöztetési Mátrix

| Forrás Mappa | Cél Sziget | Cél Útvonal | Megjegyzés |
|--------------|------------|-------------|------------|
| `spaceos-nexus/` | **Nexus** | `/opt/nexus/nexus-core/` | ✅ Már másolva |
| `spaceos-modules-*` | **JoineryTech** | `/opt/joinerytech/modules/` | 7 modul |
| `backend/` (Kernel) | **JoineryTech** | `/opt/joinerytech/backend/` | Core .NET |
| `frontend/` | **JoineryTech** | `/opt/joinerytech/frontend/` | React app |
| `datahaven-web/` | **Minden** | Másolat mindenhova | Dashboard |
| `datahaven-telegram/` | **SpaceOS** | Marad | Orchestration |
| `scripts/` | **SpaceOS** | Marad + sziget-specifikus | Pipeline |
| `infra/` | **SpaceOS** | Marad | Központi infra |

---

## Részletes Költöztetés

### 1. Nexus Sziget

**Már kész:**
- ✅ `nexus-core/knowledge-service/`
- ✅ `nexus-core/mcp-server/`
- ✅ `nexus-core/mcp-connector/`
- ✅ `nexus-core/marvin/`

**Még szükséges:**
- `scripts/` pipeline szkriptek (nightwatch, reviewer, stb.)

---

### 2. JoineryTech Sziget

**Másolandó:**
```bash
# JoineryTech modulok
cp -r /opt/spaceos/spaceos-modules-* /opt/joinerytech/modules/

# Backend core
cp -r /opt/spaceos/backend /opt/joinerytech/

# Frontend
cp -r /opt/spaceos/frontend /opt/joinerytech/
```

**Struktúra:**
```
/opt/joinerytech/
├── backend/                    ← .NET Kernel + Joinery
├── frontend/                   ← React portal
├── modules/
│   ├── crm/
│   ├── hr/
│   ├── ehs/
│   ├── qa/
│   ├── dms/
│   ├── maintenance/
│   └── kontrolling/
├── joinerytech-nexus/          ← Frozen knowledge-service
└── datahaven-web/              ← Dashboard
```

---

### 3. Doorstar Sziget

**Másolandó:**
- Nincs dedikált kód még
- JoineryTech-ből származtatott

**Struktúra:**
```
/opt/doorstar/
├── backend/                    ← Doorstar-specifikus API
├── frontend/                   ← Doorstar UI
├── doorstar-nexus/             ← Frozen knowledge-service
└── datahaven-web/              ← Dashboard
```

---

### 4. SpaceOS Sziget

**Marad:**
- `scripts/` — központi pipeline
- `infra/` — infrastruktúra konfigok
- `datahaven-telegram/` — bot
- `docs/` — központi dokumentáció
- `spaceos-conductor/` — conductor logika
- `spaceos-architect/` — architect docs
- `spaceos-librarian/` — librarian docs

---

## Végrehajtási Sorrend

### Phase 1: JoineryTech (ma)
```bash
# 1. Modulok másolása
mkdir -p /opt/joinerytech/modules
cp -r /opt/spaceos/spaceos-modules-crm /opt/joinerytech/modules/crm
cp -r /opt/spaceos/spaceos-modules-hr /opt/joinerytech/modules/hr
cp -r /opt/spaceos/spaceos-modules-ehs /opt/joinerytech/modules/ehs
cp -r /opt/spaceos/spaceos-modules-qa /opt/joinerytech/modules/qa
cp -r /opt/spaceos/spaceos-modules-dms /opt/joinerytech/modules/dms
cp -r /opt/spaceos/spaceos-modules-maintenance /opt/joinerytech/modules/maintenance

# 2. Backend core
cp -r /opt/spaceos/backend /opt/joinerytech/

# 3. Frontend
cp -r /opt/spaceos/frontend /opt/joinerytech/
```

### Phase 2: Nexus scripts (ma)
```bash
# Pipeline szkriptek
cp /opt/spaceos/scripts/nightwatch.sh /opt/nexus/scripts/
cp /opt/spaceos/scripts/reviewer.sh /opt/nexus/scripts/
cp /opt/spaceos/scripts/pipeline.sh /opt/nexus/scripts/
```

### Phase 3: Doorstar (később)
- Amikor Doorstar-specifikus fejlesztés indul
- JoineryTech-ből fork-olva

---

## Git Kezelés

### Opció A: Külön repók (ajánlott)
- Minden sziget saját git repo
- Könnyebb verziókezelés
- Tisztább history

### Opció B: Monorepo subfolders
- Egy repo, 4 mappa
- Egyszerűbb management
- Bonyolultabb CI/CD

**Döntés:** Librarian + Architect egyeztet

---

## Validation Checklist

| Sziget | Kód | Build | Test | Knowledge |
|--------|-----|-------|------|-----------|
| Nexus | ✅ | ✅ | ⏳ | ⏳ |
| JoineryTech | ⏳ | ⏳ | ⏳ | ⏳ |
| Doorstar | ⏳ | ⏳ | ⏳ | ⏳ |
| SpaceOS | ✅ | N/A | N/A | ⏳ |

---

## Felelősök

| Feladat | Ki |
|---------|-----|
| Kód másolás | Root |
| Build validáció | Backend |
| Tudástár | Librarian + Explorer |
| Architektúra review | Architect |
| Dokumentáció | Librarian |

---

_Kód Költöztetési Terv v1.0 — 2026-07-11_
