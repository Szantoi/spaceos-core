---
id: IDEA-2026-06-30-005
title: "Cost Budget Tracker Widget with Daily/Weekly Alerts"
category: feature
priority: high
effort: small
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló

Real-time költségfigyelő widget a Dashboard-on, napi/heti budget threshold alert-ekkel és terminál-szintű cost breakdown-nal.

## Probléma

Jelenleg nincs központi költség monitoring a Dashboard-on. A terminálok költsége csak a raw log-okból vagy MCP API-ból elérhető. Nincs proaktív alert ha budget threshold-ot megközelítünk.

## Megoldás

**Cost Tracker Widget (Dashboard page, top-right corner):**

```
┌─────────────────────────────────────┐
│ 💰 Today's Cost Budget              │
├─────────────────────────────────────┤
│ $12.34 / $50.00 (24%)              │
│ ████████░░░░░░░░░░░░░░░░░░░░░░      │
│                                     │
│ ⚠️ 2 soft alerts, 0 hard alerts    │
│                                     │
│ Breakdown:                          │
│ • Backend:  $7.20 (58%)             │
│ • Architect: $3.15 (26%)            │
│ • Frontend: $1.99 (16%)             │
│                                     │
│ [View Details] [Configure Alerts]   │
└─────────────────────────────────────┘
```

**Features:**
1. **Daily/Weekly budget tracking:**
   - Configurable daily limit (default: $50)
   - Weekly rolling average
   - Progress bar (green < 60%, yellow 60-80%, red > 80%)

2. **Alert levels:**
   - Soft limit: 60% ($30) → Warning toast
   - Hard limit: 80% ($40) → Alert badge + Telegram notification
   - Critical: 100% ($50) → Auto-pause workers + Root escalation

3. **Breakdown by terminal:**
   - Pie chart vagy bar chart
   - Click terminal → cost history chart (7 days)

4. **API integration:**
```
GET /api/monitoring/cost/today
GET /api/monitoring/cost/terminal/:terminal
PUT /api/monitoring/cost/config
```

**Alert notifications:**
- Toast notification (Dashboard)
- Telegram message (@root, @conductor)
- Email (optional, config)

## Acceptance Criteria

- [ ] Cost widget renderelve Dashboard page-en
- [ ] Daily cost + budget progress bar működik
- [ ] Breakdown by terminal működik (3+ terminál)
- [ ] Soft/Hard/Critical alert logic működik
- [ ] Toast notification trigger működik
- [ ] Telegram alert integrálva
- [ ] Configure Alerts modal működik (budget limit)
- [ ] View Details modal működik (7 day history chart)
- [ ] Auto-pause workers Critical threshold-nál
- [ ] Real-time update SSE-vel (ha SSE endpoint kész)
