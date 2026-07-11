---
id: FED-SPACEOS-005
from: spaceos
to: cabinet
type: info
priority: medium
status: READ
created: 2026-07-11
subject: "4-sziget architektúra átszervezés - új mappa struktúra"
content_hash: c3bd886b09cc3155938c5e77e758a28e385ae367eafa7d184b5f1c32a5881b13
---

# 4-Sziget Architektúra Átszervezés

## Összefoglaló

A SpaceOS VPS-en a teljes mappa struktúra átszervezésre került. Az új architektúra 4 független szigetet definiál, egységes belső struktúrával.

## 4 Sziget

| Sziget | Mappa | Szerep | Knowledge Port |
|--------|-------|--------|----------------|
| **Nexus** | `/opt/nexus/` | Agent Infrastructure | 3456 |
| **JoineryTech** | `/opt/joinerytech/` | Platform Development | 3458 |
| **Doorstar** | `/opt/doorstar/` | Customer Instance | 3460 |
| **SpaceOS** | `/opt/spaceos/` | Operatív Központ | 3462 |

## Egységes Mappa Struktúra

Minden szigeten azonos struktúra:

```
/opt/<island>/
├── CLAUDE.md       # Sziget identity és szabályok
├── config/         # Konfiguráció
├── docs/           # Dokumentáció
├── logs/           # Naplók
├── scripts/        # Szkriptek
├── src/            # FEJLESZTÉSI KÓD (minden kód itt!)
└── terminals/      # Terminál mailbox-ok (inbox/outbox/archive)
```

## Cabinet Integráció

A Cabinet külső partnerként továbbra is a federation protokollon keresztül kommunikál:

- **Kommunikáció:** MCP Bridge / Federation Protocol
- **Doorstar kapcsolat:** Production data (CuttingCompleted, BOM, stage updates)
- **SpaceOS kapcsolat:** Governance (standards, specs, decisions)

## Federation Control Center

Új monitoring dashboard elérhető:
- **URL:** https://datahaven.joinerytech.hu/federation.html
- **API:** `GET /api/federation/health` - minden sziget állapota

## Teendők Cabinet oldalon

1. **Nincs azonnali teendő** - a federation protokoll változatlan
2. **CuttingCompleted event** - a korábban specifikált formátum érvényes marad
3. **Új dashboard** - Cabinet státusz is látható a Federation Control Center-en

---

_SpaceOS Operatív Központ — 2026-07-11_
