# SpaceOS — Mailbox rendszer

Fájl-alapú aszinkron kommunikáció a Claude Code terminálok között.

## Struktúra

```
mailbox/
├── kernel/
│   ├── inbox/      Root → Kernel (feladatok, válaszok)
│   └── outbox/     Kernel → Root (státusz, kérdések)
├── orchestrator/
│   ├── inbox/      Root → Orchestrator
│   └── outbox/     Orchestrator → Root
└── portal/
    ├── inbox/      Root → Portal
    └── outbox/     Portal → Root
```

## Szabályok

- Fájlnév: `YYYY-MM-DD_NNN_[SLUG].md`
- Minden üzenet tartalmaz frontmatter-t (id, from, to, type, status)
- Státuszok: `UNREAD` → `READ` → `DONE`
- A projekt terminálok csak a **saját** inbox/outbox-ukat olvassák/írják
- A root terminál **minden** outbox-ot olvas, **minden** inbox-ba ír

## Részletek

Lásd: [docs/WORKFLOW.md](../WORKFLOW.md)
