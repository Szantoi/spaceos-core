---
id: MSG-BACKEND-125
from: explorer
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-EXPLORER-015
created: 2026-07-04
completed: 2026-07-04
---

# Task: KPI Metrics SSE Endpoint Implementation (Backend)

**Epic:** JoineryTech Dashboard Enhancement
**Priority:** HIGH
**Timeline:** 1-2 days
**Effort:** Low (API endpoint + aggregation logic)
**Blocker:** None

---

## Overview

Implement **SSE (Server-Sent Events) endpoint** streaming real-time KPI metrics for Datahaven Dashboard KPI strip.

---

## Endpoint Specification

### Route: `GET /api/dashboard/metrics/stream`

**Type:** Server-Sent Events (SSE)
**Protocol:** HTTP/1.1 with `Content-Type: text/event-stream`
**Auth:** Bearer JWT (existing)
**Latency Target:** <1s end-to-end

### Response Format

```
event: metrics
data: {
  "activeTerminals": 7,
  "inboxQueue": 23,
  "avgTaskTime": 1680,
  "pipelineHealth": 0.94,
  "apiUptime": 0.999,
  "lastTaskDone": {
    "time": "2026-07-04T11:15:00Z",
    "taskId": "MSG-EXPLORER-015"
  }
}

event: metrics
data: {
  ...
}
```

**Update frequency:** Every 2-3 seconds
**Connection timeout:** 5 minutes (keep-alive with comment)

---

## Metrics to Calculate

### 1. Active Terminals (`activeTerminals`: int)

```csharp
var activeTerminals = GetWorkingTerminals().Count();
// Returns: count of terminals with status="working"
```

**Source:** Terminal status table (already tracked)

### 2. Inbox Queue (`inboxQueue`: int)

```csharp
var unreadCount = await _mailbox.CountUnreadMessages();
// Returns: sum of UNREAD status items across all terminals
```

**Source:** Mailbox service (existing)

### 3. Average Task Time (`avgTaskTime`: int, seconds)

```csharp
var avgTime = await _taskService.GetAverageCompletionTime(last24Hours: true);
// Returns: median completion time in seconds (DONE timestamp - created timestamp)
```

**Source:** Task audit logs (last 24 hours)

### 4. Pipeline Health (`pipelineHealth`: float, 0-1)

```csharp
var doneCount = await _taskService.CountByStatus("DONE", last24Hours: true);
var blockedCount = await _taskService.CountByStatus("BLOCKED", last24Hours: true);
var health = doneCount / (doneCount + blockedCount + 1); // Avoid /0
// Returns: DONE/(DONE+BLOCKED) ratio
```

**Source:** Task audit logs (last 24 hours)

### 5. API Uptime (`apiUptime`: float, 0-1)

```csharp
var uptime = await _monitoring.GetUptime(last24Hours: true);
// Returns: HTTP 200+ success rate (sum of successful requests / total requests)
// If not available, default to 0.99 (assume healthy)
```

**Source:** API gateway logs or monitoring service (if available)

### 6. Latest DONE Task (`lastTaskDone`: object)

```csharp
var latest = await _taskService.GetLatestCompleted();
return new {
  time = latest.CompletedAt.ToIso8601(),
  taskId = latest.Id
};
// Returns: most recent DONE outbox message
```

**Source:** Task audit logs (last record with status=DONE)

---

## Implementation Pattern

### C# Minimal API (ASP.NET Core)

```csharp
app.MapGet("/api/dashboard/metrics/stream", async (HttpResponse response, IServiceProvider services) =>
{
  response.ContentType = "text/event-stream";
  response.Headers["Cache-Control"] = "no-cache";

  var metricsService = services.GetRequiredService<IKPIMetricsService>();

  // Send metrics every 2-3 seconds for 5 minutes (max)
  for (int i = 0; i < 100; i++)
  {
    var metrics = await metricsService.GetCurrentMetrics();
    var json = JsonSerializer.Serialize(metrics);

    await response.WriteAsync($"event: metrics\n");
    await response.WriteAsync($"data: {json}\n\n");
    await response.Body.FlushAsync();

    // Keep-alive comment every 30s
    if (i % 10 == 0)
      await response.WriteAsync($": keep-alive\n\n");

    await Task.Delay(2000); // 2-second interval
  }
})
.WithName("GetKPIMetricsStream")
.Produces(200, "text/event-stream")
.WithOpenApi();
```

### Service Interface

```csharp
public interface IKPIMetricsService
{
  Task<KPIMetricsDto> GetCurrentMetrics();
}

public class KPIMetricsDto
{
  public int ActiveTerminals { get; set; }
  public int InboxQueue { get; set; }
  public int AvgTaskTime { get; set; }      // seconds
  public float PipelineHealth { get; set; }  // 0-1
  public float ApiUptime { get; set; }       // 0-1
  public TaskSummaryDto LastTaskDone { get; set; }
}

public class TaskSummaryDto
{
  public DateTime Time { get; set; }
  public string TaskId { get; set; }
}
```

---

## Data Sources

| Metric | Source | Method | Caching |
|--------|--------|--------|---------|
| activeTerminals | Terminal status table | SQL query | 5 min |
| inboxQueue | Mailbox DB | COUNT UNREAD | 1 min |
| avgTaskTime | Task audit logs | Median (SQL) | 10 min |
| pipelineHealth | Task audit logs | COUNT/SUM | 5 min |
| apiUptime | Monitoring API/logs | Aggregate | 10 min |
| lastTaskDone | Task audit logs | MAX timestamp | 2 min |

**Note:** Cache aggressively to avoid database load. SSE updates every 2-3s but queries don't need to refresh that frequently.

---

## Acceptance Criteria

- [ ] `GET /api/dashboard/metrics/stream` endpoint implemented
- [ ] SSE response format correct (event + data lines)
- [ ] 6 metrics calculated & populated
- [ ] Real-time updates every 2-3 seconds
- [ ] Connection timeout 5 minutes (keep-alive)
- [ ] <1s latency per metric calculation
- [ ] Error handling (graceful disconnect)
- [ ] Unit tests (mock metrics service)
- [ ] Integration tests (real database queries)
- [ ] OpenAPI spec updated (text/event-stream type)

---

## Blockers

❌ **Frontend KPI component** required to consume endpoint
- Frontend task: MSG-FRONTEND-100 (parallel)
- Can use mock endpoint while Frontend develops

---

## Testing

### Manual Test

```bash
# Terminal 1: Start server
dotnet run

# Terminal 2: Connect to SSE
curl -N http://localhost:5000/api/dashboard/metrics/stream | jq .

# Should see:
# event: metrics
# data: {"activeTerminals":7,"inboxQueue":23,...}
# ... repeating every 2-3 seconds
```

### Load Test

```bash
# Multiple concurrent connections
for i in {1..10}; do
  curl -N http://localhost:5000/api/dashboard/metrics/stream &
done
```

---

## Performance Notes

**Latency breakdown:**
- Database queries: 50-100ms
- Calculation: 10-20ms
- SSE overhead: 5-10ms
- **Total:** <200ms per update

**Resource usage:**
- CPU: Low (<5% during updates)
- Memory: Streaming (no buffering)
- Database: 1 query per interval (cached)

---

## References

- MDN SSE: https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events
- ASP.NET Core streaming: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware
- Existing monitoring: `src/Monitoring/IMonitoringService.cs`

---

## Next Steps

1. Implement IKPIMetricsService (day 1)
2. Implement SSE endpoint (day 1)
3. Test with Frontend team (day 2)
4. Performance tuning if needed (day 2)
5. Deploy to staging (day 2)

---

**Dependency:** Frontend MSG-FRONTEND-100 (KPI component)
**Ready to start:** Yes (mock data while Frontend develops)

🤖 Prepared by Explorer
📅 2026-07-04
