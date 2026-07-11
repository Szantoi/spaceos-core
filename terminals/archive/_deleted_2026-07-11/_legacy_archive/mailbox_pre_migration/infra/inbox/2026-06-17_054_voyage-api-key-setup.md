---
id: MSG-INFRA-054
from: root
to: infra
type: task
priority: high
status: COMPLETE
model: haiku
ref: MSG-NEXUS-001
created: 2026-06-17
---

# Infra — VPS Setup: Voyage AI API Key

## Összefoglaló

**URGENT:** Nexus Knowledge Service Fázis 1 unblock-hoz szükséges a Voyage AI API key beállítása a VPS-en.

**Ref:** MSG-NEXUS-001 BLOCKED (embedding API key hiányzik)

---

## Lépések (VPS SSH)

**Host:** `109.122.222.198` · **User:** `gabor`

```bash
# 1. Voyage AI key beszerzése (5 min)
# https://dash.voyageai.com/
# Free tier: 25M tokens/month (bőven elég docs/knowledge-hez)

# 2. VPS setup
cd /opt/spaceos/spaceos-nexus/knowledge-service

# 3. .env frissítés
nano .env
# Add/update: VOYAGE_API_KEY=<key from dash.voyageai.com>

# 4. Service restart (Nexus terminál majd "Folytasd a munkát")
# (nem szükséges most, Nexus session-t majd fortsz)
```

---

## Prioritás

**HIGH** — Nexus Fázis 1 (Knowledge Service) nem tud tesztelésre lépni nélküle.

**Timeline:** 30 perc (API key procurement + VPS setup)

---

## DoD

- [ ] Voyage AI account + free tier key
- [ ] VPS: `/opt/spaceos/spaceos-nexus/knowledge-service/.env` → `VOYAGE_API_KEY=<key>`
- [ ] Confirm setup success (report back to Root)

---

**Status:** AWAITING VPS OPERATOR
