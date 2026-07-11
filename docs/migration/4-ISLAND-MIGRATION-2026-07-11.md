# 4-Sziget Migráció — 2026-07-11

## Összefoglaló

A SpaceOS monolitikus struktúráját 4 független szigetre bontottuk szét a jobb fókusz és keveredés elkerülése érdekében.

---

## Új Architektúra

```
/opt/nexus/           ← Agent Infrastructure Development (port 3456-3457)
/opt/joinerytech/     ← Faipar SaaS Platform (port 3458-3459)
/opt/doorstar/        ← Doorstar Ügyfél Implementation (port 3460-3461)
/opt/spaceos/         ← Orchestration & Research (port 3462-3463)
```

---

## Szigetek Szerepei

| Sziget | Szerep | Terminálok |
|--------|--------|------------|
| **Nexus** | Knowledge-service, MCP tools, pipeline fejlesztés | root, backend, qa |
| **JoineryTech** | 7 modul platform (CRM, HR, EHS, stb.) | root, conductor, backend, frontend, designer |
| **Doorstar** | 6-STAGE production, Cabinet-VPS bridge | root, conductor, backend, frontend |
| **SpaceOS** | Stratégiai tervezés, tudásbázis, koordináció | root, conductor, architect, librarian, explorer |

---

## Port Allokáció

| Sziget | Knowledge Service | Datahaven | ChromaDB |
|--------|------------------|-----------|----------|
| Nexus | 3456 | 3457 | 8001 |
| JoineryTech | 3458 | 3459 | 8002 |
| Doorstar | 3460 | 3461 | 8003 |
| SpaceOS | 3462 | 3463 | 8000 |

---

## Federation Protokoll

Szigetek közötti kommunikáció:
- **Inbox:** `terminals/federation/inbox/`
- **Outbox:** `terminals/federation/outbox/`
- **Watcher:** `/opt/spaceos/scripts/federation-watcher.sh`

Cabinet-VPS kommunikáció a Doorstar szigeten keresztül.

---

## Létrehozott Fájlok

### Minden szigeten
- `CLAUDE.md` — Sziget identity
- `terminals/*/CLAUDE.md` — Terminál identityk
- `config/federation.yaml` — Federation konfig
- `.env` — Knowledge-service beállítások

### Dokumentáció
- `/opt/spaceos/docs/FEDERATION_PROTOCOL.md`
- `/opt/spaceos/docs/migration/4-ISLAND-MIGRATION-2026-07-11.md` (ez a fájl)

### Szkriptek
- `/opt/spaceos/scripts/federation-watcher.sh`

---

## Szolgáltatás Indítás

### Nexus (port 3456)
```bash
cd /opt/nexus/nexus-core/knowledge-service
TERMINALS_PATH=/opt/nexus/terminals \
KNOWLEDGE_BASE_PATH=/opt/nexus/docs/knowledge \
PORT=3456 \
node dist/server.js
```

### JoineryTech (port 3458)
```bash
cd /opt/joinerytech/joinerytech-nexus/knowledge-service
TERMINALS_PATH=/opt/joinerytech/terminals \
KNOWLEDGE_BASE_PATH=/opt/joinerytech/docs/knowledge \
PORT=3458 \
node dist/server.js
```

### Doorstar (port 3460)
```bash
cd /opt/doorstar/doorstar-nexus/knowledge-service
TERMINALS_PATH=/opt/doorstar/terminals \
KNOWLEDGE_BASE_PATH=/opt/doorstar/docs/knowledge \
PORT=3460 \
node dist/server.js
```

---

## Migrált Tartalom

### Doorstar
- Federation inbox (22 üzenet Cabinet-től)
- Federation outbox (3 válasz)

### JoineryTech
- 7 modul dokumentáció (`docs/joinerytech/`)

---

## Következő Lépések

1. ✅ Nexus knowledge-service fut (3456)
2. ✅ JoineryTech knowledge-service fut (3458)
3. ✅ Doorstar knowledge-service fut (3460)
4. ✅ SpaceOS knowledge-service fut (3462)
5. ⏳ Federation watcher cron beállítás
6. ⏳ Systemd service fájlok minden szigethez
7. ⏳ Tudástár kialakítás (Librarian + Explorer)

## Aktív Inbox Üzenetek

- MSG-LIBRARIAN-029: 4-sziget tudástár kialakítás
- MSG-EXPLORER-019: Tudástár felfedezés

---

## Régi Struktúra Backup

Az eredeti SpaceOS struktúra megmaradt `/opt/spaceos/`-ban.
A `CLAUDE.md.backup` tartalmazza a régi konfigurációt.

---

_Migráció: 2026-07-11 08:46-09:10 UTC_
_Végrehajtó: Root Terminal_
