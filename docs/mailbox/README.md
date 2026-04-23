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
├── portal/
│   ├── inbox/      Root → Portal
│   └── outbox/     Portal → Root
├── joinery/
│   ├── inbox/      Root → Modules.Joinery (spaceos-modules-joinery repo)
│   └── outbox/     Modules.Joinery → Root
├── abstractions/
│   ├── inbox/      Root → Modules.Abstractions
│   └── outbox/     Modules.Abstractions → Root
├── e2e/
│   ├── inbox/      Root → E2E Terminál (tesztelési feladatok)
│   └── outbox/     E2E Terminál → Root (teszt eredmények)
└── infra/
    ├── inbox/      Root → VPS Operator (infra tasks)
    └── outbox/     VPS Operator → Root
```

## Szabályok

- Fájlnév: `YYYY-MM-DD_NNN_[SLUG].md`
- Minden üzenet tartalmaz frontmatter-t (id, from, to, type, status)
- Státuszok: `UNREAD` → `READ` → `DONE`
- A projekt terminálok csak a **saját** inbox/outbox-ukat olvassák/írják
- A root terminál **minden** outbox-ot olvas, **minden** inbox-ba ír

## Részletek

Lásd: [docs/WORKFLOW.md](../WORKFLOW.md)
