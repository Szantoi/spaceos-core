---
id: MSG-BACKEND-126
from: explorer
to: backend
type: task
priority: high
status: READ
read_at: 2026-07-04
model: sonnet
ref: MSG-EXPLORER-015
created: 2026-07-04
content_hash: a8ae5e5d5126cfce8a7aaec50a40ee18de2aa3d3846cc4e9b963525e88e522c9
---

# Task: Cost Monitoring API Endpoints Implementation (Backend)

**Epic:** JoineryTech Dashboard Enhancement
**Priority:** HIGH
**Timeline:** 1-2 days
**Effort:** Low (API wrapper around existing Conductor cost tracking)
**Blocker:** None

---

## Overview

Expose **cost monitoring HTTP endpoints** for Frontend Dashboard widget. Conductor already tracks costs; Backend just wraps data in REST API with SSE streaming.

---

## Endpoint Specification

### 1. Real-Time Cost Stream (SSE)

**Route:** `GET /api/monitoring/cost/stream`

**Type:** Server-Sent Events (SSE)
**Protocol:** HTTP/1.1 with `Content-Type: text/event-stream`
**Auth:** Bearer JWT (existing)
**Latency Target:** <500ms end-to-end
**Update frequency:** 1-2 seconds

**Response Format:**
```
event: cost-update
data: {
  "timestamp": "2026-07-04T14:30:15Z",
  "dailyBudget": 50.00,
  "current": 34.27,
  "currency": "USD",
  "thresholdStatus": "healthy",
  "terminals": [
    {
      "name": "backend",
      "cost": 12.40,
      "percentage": 36.2,
      "status": "healthy",
      "minutesToThreshold": null
    },
    {
      "name": "architect",
      "cost": 8.90,
      "percentage": 26.0,
      "status": "healthy",
      "minutesToThreshold": null
    },
    {
      "name": "frontend",
      "cost": 7.80,
      "percentage": 22.8,
      "status": "caution",
      "minutesToThreshold": 45
    },
    {
      "name": "designer",
      "cost": 5.17,
      "percentage": 15.0,
      "status": "healthy",
      "minutesToThreshold": null
    }
  ],
  "recentCosts": [
    {
      "worker": "worker-12",
      "terminal": "backend",
      "amount": 2.10,
      "timestamp": "2026-07-04T14:30:05Z"
    },
    {
      "worker": "worker-08",
      "terminal": "architect",
      "amount": 1.50,
      "timestamp": "2026-07-04T14:29:30Z"
    }
  ]
}

event: cost-update
data: { ... }
```

**Threshold Status Values:**
- `"healthy"` — 0-60% budget used
- `"caution"` — 60-80% budget used
- `"critical"` — 80-100% budget used
- `"exceeded"` — >100% budget used

**Connection timeout:** 5 minutes (keep-alive with comment every 30s)

---

### 2. Today's Cost Summary

**Route:** `GET /api/monitoring/cost/today`

**Method:** GET
**Auth:** Bearer JWT
**Latency:** <200ms

**Response:**
```json
{
  "date": "2026-07-04",
  "dailyBudget": 50.00,
  "current": 34.27,
  "currency": "USD",
  "thresholdStatus": "healthy",
  "percentageUsed": 68.5,
  "remaining": 15.73,
  "terminals": [
    {
      "name": "backend",
      "cost": 12.40,
      "percentage": 36.2,
      "status": "healthy",
      "minutesToThreshold": null
    },
    {
      "name": "architect",
      "cost": 8.90,
      "percentage": 26.0,
      "status": "healthy",
      "minutesToThreshold": null
    },
    {
      "name": "frontend",
      "cost": 7.80,
      "percentage": 22.8,
      "status": "caution",
      "minutesToThreshold": 45
    },
    {
      "name": "designer",
      "cost": 5.17,
      "percentage": 15.0,
      "status": "healthy",
      "minutesToThreshold": null
    }
  ]
}
```

---

### 3. Terminal Cost Detail

**Route:** `GET /api/monitoring/cost/terminal/:terminal`

**Method:** GET
**Auth:** Bearer JWT
**Parameters:**
- `terminal` — terminal name (backend, frontend, architect, designer, conductor, librarian, explorer)
- `?days=7` — (optional) include history for last N days

**Response:**
```json
{
  "terminal": "backend",
  "today": {
    "cost": 12.40,
    "workers": [
      {
        "id": "worker-12",
        "cost": 2.10,
        "startTime": "2026-07-04T14:20:00Z",
        "endTime": "2026-07-04T14:30:05Z",
        "model": "sonnet",
        "task": "MSG-BACKEND-125"
      },
      {
        "id": "worker-08",
        "cost": 1.80,
        "startTime": "2026-07-04T14:10:00Z",
        "endTime": "2026-07-04T14:19:45Z",
        "model": "haiku",
        "task": "MSG-BACKEND-123"
      }
    ]
  },
  "history": [
    {
      "date": "2026-07-03",
      "cost": 15.20,
      "workerCount": 8
    },
    {
      "date": "2026-07-02",
      "cost": 18.50,
      "workerCount": 11
    }
  ],
  "averageDailyCost": 14.80,
  "costTrend": "+0.5%"  // vs 7-day average
}
```

---

### 4. Cost History (7-Day)

**Route:** `GET /api/monitoring/cost/history`

**Method:** GET
**Auth:** Bearer JWT
**Parameters:**
- `?days=7` — (default: 7) how many days back
- `?detailed=true` — (optional) include per-terminal breakdown

**Response:**
```json
{
  "period": {
    "start": "2026-06-27",
    "end": "2026-07-04",
    "days": 7
  },
  "dailyBudget": 50.00,
  "totalCost": 98.45,
  "averageDailyCost": 14.07,
  "history": [
    {
      "date": "2026-06-27",
      "cost": 12.30,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-06-28",
      "cost": 15.20,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-06-29",
      "cost": 18.50,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-06-30",
      "cost": 21.80,
      "budgetStatus": "caution",
      "exceedance": null
    },
    {
      "date": "2026-07-01",
      "cost": 14.40,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-07-02",
      "cost": 18.50,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-07-03",
      "cost": 15.20,
      "budgetStatus": "healthy",
      "exceedance": null
    },
    {
      "date": "2026-07-04",
      "cost": 34.27,
      "budgetStatus": "caution",
      "exceedance": null
    }
  ]
}
```

---

### 5. Cost Configuration

**Route:** `PUT /api/monitoring/cost/config`

**Method:** PUT
**Auth:** Bearer JWT + `admin` role
**Request Body:**
```json
{
  "dailyBudget": 75.00,
  "softAlertThreshold": 0.60,
  "hardAlertThreshold": 0.80,
  "autoPauseThreshold": 0.80,
  "alertChannels": ["dashboard", "telegram"],
  "pauseNotification": "auto-pause-new-workers"
}
```

**Response:**
```json
{
  "status": "ok",
  "config": {
    "dailyBudget": 75.00,
    "softAlertThreshold": 0.60,
    "hardAlertThreshold": 0.80,
    "autoPauseThreshold": 0.80
  },
  "updatedAt": "2026-07-04T14:35:00Z"
}
```

---

### 6. Alert Trigger for Auto-Pause

**Route:** `POST /api/monitoring/cost/pause-notification`

**Method:** POST
**Auth:** Bearer JWT
**Request Body:**
```json
{
  "currentCost": 40.00,
  "dailyBudget": 50.00,
  "thresholdStatus": "critical",
  "terminals": [
    {
      "name": "backend",
      "cost": 15.00
    }
  ]
}
```

**Response:**
```json
{
  "status": "acknowledged",
  "message": "Auto-pause triggered. New worker spawns disabled.",
  "coordinatorNotified": true
}
```

---

## Data Sources

| Endpoint | Source | Method | Caching | Notes |
|---|---|---|---|---|
| `/stream` | Worker registry + Conductor costs | Real-time aggregation | None | SSE, no caching |
| `/today` | Cost service (day start) | SUM query | 30 sec | Quick summary |
| `/terminal/:terminal` | Worker registry | Filtered query | 1 min | Per-terminal drill-down |
| `/history` | Cost audit log | Query last N days | 5 min | Historical trend |
| `/config` | Database (cost_config table) | SELECT | 10 min | Admin settings |

**Caching Strategy:**
- SSE stream: **No cache** (real-time updates)
- `/today`: **30s cache** (summary doesn't need sub-second updates)
- `/terminal/:terminal`: **1min cache** (worker detail list)
- `/history`: **5min cache** (trends don't change fast)
- `/config`: **10min cache** (admin settings rarely change)

---

## Implementation Pattern

### C# Minimal API (ASP.NET Core)

```csharp
app.MapGet("/api/monitoring/cost/stream", async (HttpResponse response, IServiceProvider services) =>
{
  response.ContentType = "text/event-stream";
  response.Headers["Cache-Control"] = "no-cache";

  var costService = services.GetRequiredService<ICostMonitoringService>();

  // Stream updates for 5 minutes (max)
  for (int i = 0; i < 150; i++)  // 150 × 2s = 5 min
  {
    var costData = await costService.GetCurrentCosts();
    var json = JsonSerializer.Serialize(costData);

    await response.WriteAsync($"event: cost-update\n");
    await response.WriteAsync($"data: {json}\n\n");
    await response.Body.FlushAsync();

    // Keep-alive comment every 30s
    if (i % 15 == 0)
      await response.WriteAsync($": keep-alive\n\n");

    await Task.Delay(2000); // 2-second interval
  }
})
.WithName("GetCostStream")
.Produces(200, "text/event-stream")
.WithOpenApi();

app.MapGet("/api/monitoring/cost/today", async (HttpContext context, IServiceProvider services) =>
{
  var costService = services.GetRequiredService<ICostMonitoringService>();
  var costs = await costService.GetTodayCosts();
  return Results.Ok(costs);
})
.WithName("GetTodayCosts")
.WithOpenApi();

app.MapGet("/api/monitoring/cost/terminal/{terminal}", async (string terminal, HttpContext context, IServiceProvider services) =>
{
  var costService = services.GetRequiredService<ICostMonitoringService>();
  var costs = await costService.GetTerminalCosts(terminal);
  return Results.Ok(costs);
})
.WithName("GetTerminalCosts")
.WithOpenApi();

app.MapGet("/api/monitoring/cost/history", async (HttpContext context, IServiceProvider services, [FromQuery] int days = 7) =>
{
  var costService = services.GetRequiredService<ICostMonitoringService>();
  var history = await costService.GetCostHistory(days);
  return Results.Ok(history);
})
.WithName("GetCostHistory")
.WithOpenApi();

app.MapPut("/api/monitoring/cost/config", async (CostConfigRequest request, HttpContext context, IServiceProvider services) =>
{
  // Verify admin role
  if (!context.User.HasClaim("role", "admin"))
    return Results.Forbid();

  var costService = services.GetRequiredService<ICostMonitoringService>();
  var config = await costService.UpdateCostConfig(request);
  return Results.Ok(config);
})
.WithName("UpdateCostConfig")
.WithOpenApi();
```

### Service Interface

```csharp
public interface ICostMonitoringService
{
  Task<CostStreamDto> GetCurrentCosts();
  Task<TodayCostDto> GetTodayCosts();
  Task<TerminalCostDto> GetTerminalCosts(string terminal);
  Task<CostHistoryDto> GetCostHistory(int days);
  Task<CostConfigDto> UpdateCostConfig(CostConfigRequest request);
}

public class CostStreamDto
{
  public DateTime Timestamp { get; set; }
  public decimal DailyBudget { get; set; }
  public decimal Current { get; set; }
  public string Currency { get; set; } = "USD";
  public string ThresholdStatus { get; set; }  // healthy|caution|critical|exceeded
  public List<TerminalCostDetailDto> Terminals { get; set; }
  public List<RecentCostDto> RecentCosts { get; set; }
}

public class TodayCostDto
{
  public string Date { get; set; }
  public decimal DailyBudget { get; set; }
  public decimal Current { get; set; }
  public string Currency { get; set; } = "USD";
  public string ThresholdStatus { get; set; }
  public decimal PercentageUsed { get; set; }
  public decimal Remaining { get; set; }
  public List<TerminalCostDetailDto> Terminals { get; set; }
}

public class TerminalCostDetailDto
{
  public string Name { get; set; }
  public decimal Cost { get; set; }
  public decimal Percentage { get; set; }
  public string Status { get; set; }  // healthy|caution|critical
  public int? MinutesToThreshold { get; set; }
}
```

---

## Acceptance Criteria

- [ ] `GET /api/monitoring/cost/stream` endpoint (SSE) implemented
- [ ] SSE response format correct (event + data lines)
- [ ] Real-time updates every 1-2 seconds
- [ ] Connection timeout 5 minutes (keep-alive)
- [ ] `GET /api/monitoring/cost/today` implemented
- [ ] `GET /api/monitoring/cost/terminal/:terminal` implemented
- [ ] `GET /api/monitoring/cost/history` implemented
- [ ] `PUT /api/monitoring/cost/config` implemented (admin only)
- [ ] `POST /api/monitoring/cost/pause-notification` implemented
- [ ] Threshold calculations correct (60%/80%/100% soft/hard/critical)
- [ ] Terminal cost breakdown aggregation correct
- [ ] Caching strategy implemented (30s-10m per endpoint)
- [ ] Error handling (graceful disconnect, invalid terminal, auth)
- [ ] Unit tests (mock cost service)
- [ ] Integration tests (real cost data queries)
- [ ] OpenAPI spec updated (all 5 endpoints documented)
- [ ] <500ms latency per request

---

## Data Integration

### Existing Conductor Cost Tracking

The Conductor already tracks worker costs. Backend should:
1. **Query Conductor's cost ledger** (already exists)
2. **Aggregate by terminal** (group workers by terminal name)
3. **Calculate thresholds** (60%/80%/100%)
4. **Format for Frontend** (the DTOs above)

**No new cost tracking logic needed** — just expose existing data.

---

## Testing

### Manual Test (Cost Stream)

```bash
# Terminal 1: Start server
dotnet run

# Terminal 2: Connect to SSE
curl -N http://localhost:5000/api/monitoring/cost/stream | jq .

# Should see:
# event: cost-update
# data: {"timestamp":"...","current":34.27,"dailyBudget":50.00,...}
# ... repeating every 2 seconds
```

### Manual Test (Today's Cost)

```bash
curl -H "Authorization: Bearer <TOKEN>" \
  http://localhost:5000/api/monitoring/cost/today | jq .
```

### Load Test (Multiple Concurrent Streams)

```bash
for i in {1..5}; do
  curl -N http://localhost:5000/api/monitoring/cost/stream &
done
```

---

## Performance Notes

**Latency breakdown:**
- Database queries: 20-50ms
- Aggregation: 10-15ms
- JSON serialization: 5-10ms
- SSE overhead: 5-10ms
- **Total:** <100ms per update

**Resource usage:**
- CPU: Low (<3% during updates)
- Memory: Streaming (no buffering)
- Database: 1 query per interval (cached)

---

## References

- MDN SSE: https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events
- ASP.NET Core streaming: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware
- Conductor cost tracking: Already implemented, just expose via API

---

## Next Steps

1. Implement ICostMonitoringService (day 1)
2. Implement 5 API endpoints (day 1)
3. Test with Frontend team (day 1-2)
4. Performance tuning if needed (day 2)
5. Deploy to staging (day 2)

---

**Dependency:** Frontend MSG-FRONTEND-101 (cost widget)
**Ready to start:** Yes (Conductor cost data available now)

🤖 Prepared by Explorer
📅 2026-07-04
