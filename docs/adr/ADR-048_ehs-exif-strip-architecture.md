# ADR-048: EHS EXIF Strip Architecture (Worker Service)

**Status:** Accepted
**Date:** 2026-06-22
**Decision Makers:** Backend Team
**Related:** MSG-BACKEND-026 (EXIF Spike)

---

## Context

The EHS Incident Reporting API allows users to upload incident photos via S3 presigned URLs. These photos may contain sensitive EXIF metadata including:

- **GPS coordinates** (location privacy risk)
- **Device information** (camera model, serial number)
- **Timestamps** (exposure date/time)
- **Personal data** (copyright, author fields)

**GDPR Requirement:** All EXIF metadata must be stripped before photos are accessible to other users or stored long-term.

**Volume:** Initial estimate 10k photos/month, scaling to 100k+ photos/month in 2027.

---

## Decision

**We will implement EXIF stripping using a .NET 8 Worker Service (BackgroundService) with ImageSharp library.**

```
User → S3 upload → Queue (RabbitMQ) → Worker → EXIF strip → S3 overwrite
```

**Stack:**
- **Runtime:** .NET 8 Worker Service (BackgroundService)
- **Library:** SixLabors.ImageSharp 3.1
- **Queue:** RabbitMQ (self-hosted on VPS)
- **Deployment:** Systemd service on VPS (alongside EHS API)

---

## Rationale

### 1. Cost Efficiency (60% savings)

| Solution | Cost at 10k/mo | Cost at 100k/mo |
|----------|----------------|-----------------|
| AWS Lambda | $30/month | $300/month |
| Worker Service | $12/month | $20/month |

At current scale, Worker Service saves **$18/month** ($216/year). At 100k photos/month, savings increase to **$280/month** ($3,360/year).

### 2. Consistent Performance

- **Lambda:** 600ms avg + 300-500ms cold start penalty = **900-1100ms total**
- **Worker:** 800ms avg, **no cold start** = **800ms total**

Worker provides **more predictable user experience** (no cold start delays).

### 3. Debugging & Observability

- **Lambda:** CloudWatch logs, no local debugging
- **Worker:** Local breakpoints, Prometheus metrics, familiar .NET tooling

Development velocity is higher with Worker Service.

### 4. No Vendor Lock-in

- **Lambda:** Tied to AWS (migration cost if switching clouds)
- **Worker:** Portable to any cloud/on-prem (VPS, Kubernetes, Azure, GCP)

### 5. .NET Ecosystem Alignment

- EHS module is .NET 8 (Domain, Application, Infrastructure, API)
- Worker Service uses same libraries (ImageSharp, MediatR, EF Core)
- Shared logging, monitoring, and error handling patterns

---

## Implementation Details

### Worker Service Code

```csharp
public class ExifStripWorker : BackgroundService
{
    private readonly IS3Service _s3;
    private readonly IQueueConsumer _queue;
    private readonly ILogger<ExifStripWorker> _logger;

    public ExifStripWorker(
        IS3Service s3,
        IQueueConsumer queue,
        ILogger<ExifStripWorker> logger)
    {
        _s3 = s3;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ExifStripWorker started");

        await foreach (var message in _queue.ConsumeAsync("ehs-photo-upload", ct))
        {
            try
            {
                var s3Key = message.PhotoS3Key;
                _logger.LogInformation("Processing photo: {S3Key}", s3Key);

                // 1. Download from S3
                using var stream = await _s3.GetObjectStreamAsync(s3Key, ct)
                    .ConfigureAwait(false);

                // 2. Load image
                using var image = await Image.LoadAsync(stream, ct)
                    .ConfigureAwait(false);

                // 3. Strip EXIF (GDPR compliance)
                image.Metadata.ExifProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.XmpProfile = null;

                // 4. Auto-rotate (preserve correct orientation)
                image.Mutate(x => x.AutoOrient());

                // 5. Save to memory
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream, ct)
                    .ConfigureAwait(false);

                // 6. Upload back to S3
                outputStream.Position = 0;
                await _s3.PutObjectAsync(s3Key, outputStream.ToArray(), "image/jpeg", ct)
                    .ConfigureAwait(false);

                // 7. Acknowledge message
                await _queue.AckAsync(message, ct).ConfigureAwait(false);

                _logger.LogInformation("Photo processed successfully: {S3Key}", s3Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process photo");
                await _queue.NackAsync(message, requeue: true, ct)
                    .ConfigureAwait(false);
            }
        }
    }
}
```

### Queue Message Format

```json
{
  "photoS3Key": "incidents/2026-06-22/abc123.jpg",
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "eventId": "evt-12345",
  "timestamp": "2026-06-22T10:30:00Z"
}
```

### Deployment (Systemd)

**File:** `/etc/systemd/system/ehs-exif-worker.service`

```ini
[Unit]
Description=EHS EXIF Strip Worker
After=network.target rabbitmq-server.service

[Service]
Type=notify
WorkingDirectory=/opt/spaceos/backend/spaceos-modules-ehs-worker
ExecStart=/usr/bin/dotnet SpaceOS.Modules.Ehs.Worker.dll
Restart=always
RestartSec=10
User=spaceos
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5010

[Install]
WantedBy=multi-user.target
```

**Commands:**
```bash
sudo systemctl daemon-reload
sudo systemctl enable ehs-exif-worker
sudo systemctl start ehs-exif-worker
sudo systemctl status ehs-exif-worker
```

---

## Consequences

### Positive

✅ **60% cost savings** at current scale ($12/mo vs $30/mo)
✅ **Consistent latency** (no cold start penalty)
✅ **Better debugging** (local breakpoints, familiar tooling)
✅ **No vendor lock-in** (portable to any cloud)
✅ **.NET ecosystem alignment** (same libraries, patterns)
✅ **GDPR compliant** (EXIF strip guaranteed)

### Negative

❌ **Manual scaling** (need to add worker instances)
  - *Mitigation:* Kubernetes auto-scaling (future)
  - *Current scale:* 1 worker handles 10k-50k photos/month

❌ **VPS maintenance** (systemd, monitoring, restarts)
  - *Mitigation:* Prometheus + Grafana alerts
  - *Effort:* ~1h/month

❌ **Queue dependency** (RabbitMQ setup)
  - *Mitigation:* RabbitMQ is battle-tested, stable
  - *Effort:* 2h one-time setup

---

## Alternatives Considered

### Alternative 1: AWS Lambda (Node.js + sharp)

**Rejected** due to:
- Higher cost ($30/mo vs $12/mo)
- Vendor lock-in (AWS-specific)
- Cold start penalty (300-500ms)
- Deployment complexity (CloudFormation, IAM)

**When to reconsider:**
- Photo volume exceeds 100k/month (auto-scaling advantage)
- Team has strong AWS expertise
- Serverless architecture is a hard requirement

### Alternative 2: AWS Lambda (.NET 8 + ImageSharp)

**Rejected** due to:
- Same cost/vendor lock-in issues as Node.js Lambda
- **Worse cold start** (1.5-2s vs 300-500ms for Node.js)
- .NET Lambda runtime overhead

### Alternative 3: Inline Processing (API endpoint)

**Rejected** due to:
- Blocks API response (15s+ processing time)
- No retry mechanism (if S3 fails)
- API timeout issues

---

## Validation & Testing

### GDPR Compliance Test

```bash
# Before upload (exiftool validation)
exiftool test-photo.jpg | grep GPS
# GPS Position: 47.4979° N, 19.0402° E

# After processing
exiftool processed-photo.jpg | grep GPS
# (no output - GPS data stripped)
```

### Automated Test

```csharp
[Fact]
public async Task ExifStripWorker_ShouldRemoveExifMetadata()
{
    // Arrange
    var s3Key = "test/photo-with-exif.jpg";
    await _s3.PutObjectAsync(s3Key, _testPhotoWithExif);

    // Act
    await _worker.ProcessAsync(new PhotoMessage { PhotoS3Key = s3Key });

    // Assert
    using var stream = await _s3.GetObjectStreamAsync(s3Key);
    using var image = await Image.LoadAsync(stream);
    image.Metadata.ExifProfile.Should().BeNull();
    image.Metadata.IptcProfile.Should().BeNull();
}
```

---

## Monitoring & Alerts

### Prometheus Metrics

```csharp
public class ExifStripMetrics
{
    public static readonly Counter PhotosProcessed = Metrics.CreateCounter(
        "ehs_exif_photos_processed_total",
        "Total photos processed");

    public static readonly Histogram ProcessingDuration = Metrics.CreateHistogram(
        "ehs_exif_processing_duration_seconds",
        "Photo processing duration");

    public static readonly Gauge QueueDepth = Metrics.CreateGauge(
        "ehs_exif_queue_depth",
        "RabbitMQ queue depth");
}
```

### Grafana Alerts

- **Queue depth > 100:** Warning (worker might be overloaded)
- **Processing time > 2s (p95):** Warning (performance degradation)
- **Worker crash:** Critical (systemd restart + Telegram alert)

---

## Rollout Plan

### Phase 1: Development (1 day)
- Implement `ExifStripWorker` (BackgroundService)
- ImageSharp integration
- RabbitMQ consumer

### Phase 2: Testing (0.5 day)
- Upload 1000 test photos with EXIF data
- Validate EXIF stripped (exiftool)
- Verify GPS coordinates removed

### Phase 3: Deployment (0.5 day)
- Deploy to VPS (systemd service)
- Prometheus metrics integration
- Telegram alerting

### Phase 4: Monitoring (1 week)
- Observe queue depth, processing time
- Tune worker count (if needed)
- Adjust RabbitMQ prefetch

**Total effort:** 3 days

---

## Future Considerations

### Scaling Beyond 100k Photos/Month

**Option 1:** Horizontal scaling (multiple workers)
- Add 2-3 worker instances (systemd @ different VPS)
- Load balance via RabbitMQ round-robin

**Option 2:** Kubernetes auto-scaling
- Deploy worker as K8s Deployment
- HPA (Horizontal Pod Autoscaler) based on queue depth

**Option 3:** Reconsider Lambda
- If cost/complexity of managing 10+ workers becomes high
- Lambda auto-scaling advantage becomes significant

---

## References

- **ImageSharp Docs:** https://docs.sixlabors.com/articles/imagesharp/
- **RabbitMQ .NET Client:** https://www.rabbitmq.com/dotnet.html
- **GDPR EXIF Requirements:** https://gdpr.eu/eu-gdpr-personal-data/
- **Spike Report:** `/opt/spaceos/docs/spike/ehs-exif-benchmark-report.md`

---

**Decision:** Accepted
**Implementation:** Sprint 3 (week of 2026-06-29)
