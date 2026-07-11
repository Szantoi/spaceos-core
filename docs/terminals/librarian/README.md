# LIBRARIAN Terminál

> Tudásbázis gondozás — knowledge dokumentumok szintézise és karbantartása

## Gyors Info

| | |
|---|---|
| **Terminál** | librarian |
| **Port** | - |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/spaceos-librarian/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/librarian/` |
| **Memory** | `/opt/spaceos/docs/memory/librarian.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/librarian.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/librarian/inbox/

# 3. Feldolgozási napló
tail -30 /opt/spaceos/docs/mailbox/librarian/PROCESSED_LOG.md
```

## Fő Feladatok

1. **Knowledge szintézis** — DONE outboxokból tudás kinyerése
2. **INDEX.md karbantartás** — minden doc összefoglalója
3. **Pattern dokumentálás** — visszatérő megoldások
4. **Context fájlok frissítése** — terminál kontextusok

## Knowledge Struktúra

```
docs/knowledge/
├── INDEX.md               ← ELSŐ olvasnivaló
├── security/              ← JWT, RBAC, RLS minták
├── deployment/            ← VPS runbook, gotchas
├── patterns/              ← Dev difficulties, DB patterns
├── architecture/          ← ADR, API contracts, module boundaries
└── context/               ← Terminál kontextusok
```

## Feldolgozási Szabályok

1. **Csak >5 órás READ üzeneteket archivál** — aktív session-t nem bántja
2. **PROCESSED_LOG.md naprakész** — minden feldolgozott üzenet itt
3. **Nem ír kódot** — csak dokumentál

## DONE Outbox Sablon

```yaml
---
id: MSG-LIBRARIAN-NNN-DONE
from: librarian
to: conductor
type: done
priority: medium
status: UNREAD
ref: MSG-LIBRARIAN-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit szintetizáltam, mely doc-ok frissültek.

## Frissített fájlok
- docs/knowledge/...

## INDEX.md változás
Igen/Nem, ha igen mit.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/spaceos-librarian/CLAUDE.md`
- Knowledge INDEX: `/opt/spaceos/docs/knowledge/INDEX.md`
- Processed log: `/opt/spaceos/docs/mailbox/librarian/PROCESSED_LOG.md`
