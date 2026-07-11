---
id: IDEA-2026-06-30-002
title: "Real-time Terminal Metrics Dashboard with SSE"
category: feature
priority: high
effort: medium
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló

Live metrics panel a Dashboard oldalon, Server-Sent Events alapú real-time frissítéssel (session cost, task progress, alert-ek).

## Probléma

A Dashboard jelenleg 2 másodpercenként poll-ol, ami késleltetett és inefficiens. Nincs azonnali feedback ha terminál STUCK/CRASHED állapotba kerül vagy költség alert lép fel.

## Megoldás

**Real-time Metrics Panel (Dashboard page):**

```
┌─────────────────────────────────────────────┐
│ Terminal Metrics (Live)                     │
├─────────────────────────────────────────────┤
│ Backend   ● WORKING  Task: MSG-BACKEND-045 │
│   Cost: $2.34  Progress: 3/5 todos          │
│   ⚠️ Alert: Approaching soft limit ($3)     │
├─────────────────────────────────────────────┤
│ Frontend  ● IDLE     Last: 2m ago           │
│   Cost: $0.00  No active task               │
└─────────────────────────────────────────────┘
```

**SSE Endpoint:**
```
GET /api/monitoring/stream
→ data: {"type":"session_cost","terminal":"backend","cost":2.34}
→ data: {"type":"todo_progress","terminal":"backend","completed":3,"total":5}
→ data: {"type":"cost_alert","terminal":"backend","level":"soft"}
```

**UI Updates:**
- SSE connection on page load
- Auto-reconnect on disconnect (exponential backoff)
- Visual pulse animation on live updates
- Alert badge count (unread alerts)
- Color-coded status indicators

## Acceptance Criteria

- [ ] SSE `/api/monitoring/stream` endpoint implementálva
- [ ] Dashboard kapcsolódik SSE-hez page load-on
- [ ] Real-time cost update (<1s latency)
- [ ] Todo progress live frissül
- [ ] Alert badge működik (unread count)
- [ ] Auto-reconnect logic működik
- [ ] Fallback to polling ha SSE nem elérhető
