# Island Setup Skill

Új sziget (island) létrehozása a 4-sziget architektúrában. Egységes mappa struktúra, CLAUDE.md, knowledge-service és terminálok beállítása.

## Trigger

- "hozz létre új szigetet"
- "új island setup"
- "sziget létrehozás"
- "create new island"

## Input Paraméterek

| Paraméter | Kötelező | Leírás |
|-----------|----------|--------|
| `island_name` | igen | Sziget neve (lowercase, pl: "nexus", "joinerytech") |
| `island_role` | igen | Szerep (infra, platform, customer, orchestration) |
| `port_range` | igen | Knowledge service port (pl: 3456) |
| `host_location` | igen | Hosting helyszín (vps, local, cloud) |
| `description` | nem | Rövid leírás |

## Hosting és Biztonság

### Sziget Típusok Hosting Szerint

| Típus | Helyszín | Token | Példa |
|-------|----------|-------|-------|
| **VPS Internal** | VPS szerver | Belső MCP token | Nexus, JoineryTech, Doorstar, SpaceOS |
| **Local External** | Lokális gép | Federation bridge token | Cabinet |
| **Cloud External** | Cloud provider | API key + OAuth | (jövőbeli) |

### Token Védelem

Minden szigetnek **saját MCP_AUTH_TOKEN**-je van:

```bash
# Sziget-specifikus token generálás
openssl rand -base64 32 > /opt/$ISLAND/config/.mcp-token

# Token beállítás
export MCP_AUTH_TOKEN=$(cat /opt/$ISLAND/config/.mcp-token)
```

**Szabályok:**
1. **Tokenek NEM megoszthatók** szigetek között
2. **Federation üzenetek** SHA-256 hash-elve
3. **Külső partnerek** (Cabinet) csak federation protokollon
4. **Localhost binding** - knowledge service csak 127.0.0.1-en (VPS)
5. **Nginx reverse proxy** - HTTPS + rate limiting külső eléréshez

### Sziget Megkülönböztetés

Minden knowledge-service válaszában szerepel az `island_id`:

```json
{
  "status": "ok",
  "island_id": "nexus",
  "host": "vps",
  "port": 3456,
  "documents": 4508
}
```

### Hosting Konfiguráció

```yaml
# /opt/<island>/config/hosting.yaml
island:
  id: "<island_name>"
  host_type: "vps"           # vps | local | cloud
  host_address: "127.0.0.1"  # Belső: localhost, Külső: IP/domain
  public_access: false       # true = nginx proxy szükséges

security:
  mcp_token_file: ".mcp-token"
  federation_only: false     # true = csak federation, nincs MCP
  allowed_origins:
    - "https://datahaven.joinerytech.hu"
  rate_limit:
    requests_per_minute: 100
```

## Egységes Mappa Struktúra

```
/opt/<island_name>/
├── CLAUDE.md           # Sziget identity és szabályok
├── config/             # Konfiguráció
├── docs/               # Dokumentáció
│   └── knowledge/      # Tudásbázis
├── logs/               # Naplók
├── scripts/            # Szkriptek
├── src/                # FEJLESZTÉSI KÓD (minden kód itt!)
│   └── <island>-nexus/ # Knowledge-service (symlink vagy copy)
│       └── knowledge-service/
└── terminals/          # Terminál mailbox-ok
    ├── root/
    │   ├── inbox/
    │   ├── outbox/
    │   └── archive/
    ├── conductor/
    ├── backend/
    ├── frontend/
    ├── designer/
    ├── architect/
    ├── librarian/
    ├── explorer/
    └── federation/
        ├── inbox/
        └── outbox/
```

## Lépések

### 1. Mappa struktúra létrehozása

```bash
ISLAND="<island_name>"
mkdir -p /opt/$ISLAND/{config,docs/knowledge,logs,scripts,src,terminals}

# Terminálok
for t in root conductor backend frontend designer architect librarian explorer; do
  mkdir -p /opt/$ISLAND/terminals/$t/{inbox,outbox,archive}
done

# Federation
mkdir -p /opt/$ISLAND/terminals/federation/{inbox,outbox}
```

### 2. CLAUDE.md létrehozása

```bash
cat > /opt/$ISLAND/CLAUDE.md << 'EOF'
# CLAUDE.md — <Island_Name> Sziget (<Role>)

> <Description>

---

## SZIGET IDENTITY

**Név:** <Island_Name>
**Szerep:** <Role>
**Port range:** <port>-<port+1>
**tmux prefix:** <island>-

---

## FELELŐSSÉGI KÖR

| Feladat | Leírás |
|---------|--------|
| ... | ... |

---

## TERMINÁLOK

| Terminál | Szerep |
|----------|--------|
| **root** | Stratégia, prioritizálás |
| **conductor** | Feladatkiosztás, koordináció |
| **backend** | Backend fejlesztés |
| **frontend** | Frontend fejlesztés |
| **designer** | UI/UX |
| **architect** | Architektúra konzultáció |
| **librarian** | Tudásbázis gondozás |
| **explorer** | Kutatás, onboarding |

---

## KAPCSOLAT MÁS SZIGETEKKEL

```
[Dependency diagram]
```

**Federation inbox:** `terminals/federation/inbox/`
**Federation outbox:** `terminals/federation/outbox/`

---

## SERVICES

| Service | Port | Leírás |
|---------|------|--------|
| Knowledge Service | <port> | MCP API |
| Datahaven Web | <port+1> | Dashboard |

---

_<Island_Name> Sziget — <Role>_
EOF
```

### 3. Knowledge-service beállítás

```bash
# Symlink vagy copy a nexus-core-ból
ln -s /opt/nexus/src/nexus-core/knowledge-service /opt/$ISLAND/src/$ISLAND-nexus/knowledge-service

# Vagy teljes másolat (ha független kell)
cp -r /opt/nexus/src/nexus-core/knowledge-service /opt/$ISLAND/src/$ISLAND-nexus/
```

### 4. Knowledge-service indítás

```bash
cd /opt/$ISLAND/src/$ISLAND-nexus/knowledge-service
TERMINALS_PATH=/opt/$ISLAND/terminals \
KNOWLEDGE_BASE_PATH=/opt/$ISLAND/docs/knowledge \
PORT=<port> \
ISLAND_ID=$ISLAND \
node dist/server.js > /opt/$ISLAND/logs/knowledge-service.log 2>&1 &
```

### 5. Federation konfiguráció frissítése

Frissítsd a `/opt/spaceos/config/federation.yaml` fájlt az új szigettel.

### 6. Federation üzenet küldése

Értesítsd a többi szigetet az új sziget létrehozásáról.

## Ellenőrzés

```bash
# Health check
curl -s http://localhost:<port>/health

# Mappa struktúra
ls /opt/$ISLAND/

# Terminálok
ls /opt/$ISLAND/terminals/
```

## Meglévő Szigetek (2026-07-11)

| Sziget | Mappa | Port | Szerep | Host | Token Típus |
|--------|-------|------|--------|------|-------------|
| Nexus | `/opt/nexus/` | 3456 | Agent Infrastructure | VPS | MCP Internal |
| JoineryTech | `/opt/joinerytech/` | 3458 | Platform Development | VPS | MCP Internal |
| Doorstar | `/opt/doorstar/` | 3460 | Customer Instance | VPS | MCP Internal |
| SpaceOS | `/opt/spaceos/` | 3462 | Operatív Központ | VPS | MCP Internal |

## Külső Partnerek

| Partner | Helyszín | Kommunikáció | Token Típus |
|---------|----------|--------------|-------------|
| **Cabinet** | Lokális gép (Doorstar Kft.) | Federation Protocol | Bridge Token |

**Cabinet különbségek:**
- **NEM VPS-en fut** - lokális CAD rendszer
- **Nincs knowledge-service** - csak federation inbox/outbox
- **Bridge token** - külön autentikáció a federation üzenetekhez
- **SHA-256 hash** - minden üzenet integritás ellenőrzött
- **Polling alapú** - Cabinet poll-ozza a VPS-t (30 sec interval)

---

_Island Setup Skill — SpaceOS 2026-07-11_
