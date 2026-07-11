---
id: MSG-BACKEND-026
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-BACKEND-024
created: 2026-06-22
content_hash: ea784b714a37bbe610a630c50f0bee3440d839efb5a4c59f12afb76386a93503
---

# EHS EXIF Strip Spike — Architecture Decision (4h)

## Context

Az EHS Incident Reporting API fotó feltöltést támogat (S3 presigned URL). A fotók EXIF metaadatai **GPS koordinátákat és személyes adatokat** tartalmazhatnak (GDPR risk).

**Sprint 2 feladat:** 4 órás spike - döntés Lambda vs. Background Worker közötti választásról.

## Task

Végezz el egy 4 órás technikai spike-ot az EXIF strip megoldásra:

### 1. Architecture Options

**Option A: AWS Lambda (S3 Event Trigger)**
```
User upload → S3 → S3 Event → Lambda → EXIF strip → S3 overwrite
```

**Pros:**
- Serverless, auto-scaling
- Native S3 integration
- Low latency (event-driven)

**Cons:**
- AWS dependency (költség, vendor lock-in)
- Lambda cold start (500ms-2s)
- Deployment complexity (CloudFormation, IAM)

**Option B: Background Worker (.NET Worker Service)**
```
User upload → S3 → Queue (RabbitMQ/SQS) → Worker → EXIF strip → S3 overwrite
```

**Pros:**
- Self-hosted (VPS control)
- .NET ecosystem (ImageSharp library)
- Debugging egyszerűbb

**Cons:**
- Manual scaling (worker instances)
- VPS resource igény (CPU, RAM)
- Queue setup (RabbitMQ or SQS)

### 2. Benchmark Requirements

**Dataset:** 1000 fotó szimulálása
- Átlagos méret: 5MB (max allowed)
- EXIF adatok: GPS, Device, Timestamp
- Formátumok: JPEG, PNG

**Metrics:**
- Processing time (avg, p95, p99)
- Memory usage (peak, avg)
- Cost estimation (AWS Lambda vs VPS CPU hours)
- Cold start penalty (Lambda only)

**Tools:**
- Lambda: Localstack vagy AWS Free Tier
- Worker: .NET Worker Service + ImageSharp
- Benchmark: BenchmarkDotNet

### 3. Implementation Sketch

**Option A: Lambda (Node.js vagy .NET)**
```typescript
// Lambda handler (Node.js)
import sharp from 'sharp';
import { S3 } from 'aws-sdk';

export const handler = async (event) => {
  const bucket = event.Records[0].s3.bucket.name;
  const key = decodeURIComponent(event.Records[0].s3.object.key);

  const s3 = new S3();
  const object = await s3.getObject({ Bucket: bucket, Key: key }).promise();

  const strippedBuffer = await sharp(object.Body)
    .rotate() // Auto-rotate based on EXIF
    .withMetadata({ exif: {} }) // Strip EXIF
    .toBuffer();

  await s3.putObject({ Bucket: bucket, Key: key, Body: strippedBuffer }).promise();
};
```

**Option B: Worker (.NET Worker Service)**
```csharp
// Worker Service (BackgroundService)
public class ExifStripWorker : BackgroundService
{
    private readonly IS3Service _s3;
    private readonly IQueueConsumer _queue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var message in _queue.ConsumeAsync(ct))
        {
            var s3Key = message.PhotoS3Key;
            using var stream = await _s3.GetObjectStreamAsync(s3Key);
            using var image = await Image.LoadAsync(stream);

            image.Metadata.ExifProfile = null; // Strip EXIF

            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            await _s3.PutObjectAsync(s3Key, outputStream.ToArray());
        }
    }
}
```

### 4. Decision Criteria

**Evaluate:**
- Performance (throughput, latency)
- Cost (AWS vs VPS)
- Complexity (deployment, maintenance)
- GDPR compliance (EXIF strip guarantee)
- Scalability (peak load handling)

**Decision Matrix:**
| Criteria | Lambda | Worker | Weight | Winner |
|----------|--------|--------|--------|--------|
| Performance | ? | ? | 30% | ? |
| Cost | ? | ? | 25% | ? |
| Complexity | ? | ? | 20% | ? |
| GDPR | ? | ? | 15% | ? |
| Scalability | ? | ? | 10% | ? |
| **Total** | ? | ? | 100% | ? |

## Definition of Done

- [ ] Option A (Lambda) proof-of-concept implementálva
- [ ] Option B (Worker) proof-of-concept implementálva
- [ ] Benchmark futtatva (1000 fotó, metrics összegyűjtve)
- [ ] Decision matrix kitöltve (pontszámokkal)
- [ ] **Döntés meghozva:** Lambda VAGY Worker (indoklással)
- [ ] ADR dokumentum írva (`docs/adr/ADR-048_ehs-exif-strip-architecture.md`)
- [ ] Cost estimation spreadsheet (`docs/deployment/ehs-exif-cost-estimation.xlsx`)

## Deliverables

1. **ADR-048:** Architecture Decision Record (EXIF strip)
2. **Benchmark report:** `docs/spike/ehs-exif-benchmark-report.md`
3. **Cost estimation:** AWS Lambda vs VPS worker ($/month, 10k photos/month)
4. **Deployment instructions:** `docs/deployment/ehs-exif-deployment.md`

## Resources

- **ImageSharp Docs:** https://docs.sixlabors.com/articles/imagesharp/
- **AWS Lambda S3 Trigger:** https://docs.aws.amazon.com/lambda/latest/dg/with-s3.html
- **BenchmarkDotNet:** https://benchmarkdotnet.org/

## Time Budget

**Total:** 4 hours
- Setup (Lambda + Worker PoC): 1.5h
- Benchmark implementation: 1h
- Metric collection + analysis: 1h
- Decision + ADR writing: 0.5h

## Estimated Effort

**1 session** (4h spike)

---

**Priority:** MEDIUM
**Blocker:** Nincs
**Next:** Döntés után deployment (Infra terminál) vagy Backend implementáció
