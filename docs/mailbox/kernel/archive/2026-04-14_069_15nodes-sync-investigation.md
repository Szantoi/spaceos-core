---
id: MSG-KERNEL-069
from: root
to: kernel
type: task
priority: medium
status: READ
ref: MSG-E2E-018-DONE
created: 2026-04-14
---

# MSG-KERNEL-069 — 15-nodes-sync: POST /bff/nodes/register 500 vizsgálat

## Háttér

Az E2E `15-nodes-sync` tesztfájl Batch 0 óta fail-el:
- `POST /bff/nodes/register` → **500**

Ez tenant/JWT probléma volt a korábbi fix-sorozatban, de a KERNEL-067 (RLS fix) + INFRA-075 (tid claim) után is fennáll.

## Kért diagnózis

```bash
# Kernel journal a /nodes/register POST-ra
sudo journalctl -u spaceos-kernel --since "5 min ago" | grep -i "nodes\|register\|500\|Exception" | tail -20
```

Vagy E2E terminál futtatja a 15-ös fájlt verbose-ban, te nézd a Kernel logot párhuzamosan:

```bash
# VPS-en folyamatos log figyelés
sudo journalctl -u spaceos-kernel -f --no-pager | grep -E "ERROR|Exception|nodes"
```

**Valószínű okok:**

1. **Unique constraint violation** — a `POST /nodes/register` idempotens-e? Ha a 15-ös teszt több alkalommal regisztrálja ugyanazt a node-ot, constraint-be ütközhet.
2. **Missing required field** — az E2E body esetleg hiányos mezőt küld (az endpoint schema-ja változott a Batch 0 óta).
3. **RLS policy** — nodes tábla `TenantId` filtering? Hasonló mint a /close volt.

## DoD

Diagnózis + outbox:
- Ha Kernel fix kell: implementálj + build (0 error) + teszt (minden zöld) → DONE
- Ha E2E fix kell (hibás body): jelezd DONE outbox-ban, E2E terminál fix-eli
- Ha idempotens hiány: implementálj upsert vagy unique-error-to-200 konverziót
