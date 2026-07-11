---
id: MSG-BACKEND-033
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/deployment/Q3_DEPLOY_CHECKLIST.md
created: 2026-06-23
content_hash: eb355bc8a3acc9ae7e3ac0b3910f43fd11cf1a6af0d9398ee8cbfbd4eaa47cda
---

# Q3 Backend Integration + Deployment

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 1 day
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Integrate the 3 Q3 tracks (Customer Portal, Pricing, ShopFloor) into a cohesive backend system and prepare for deployment to production.

**Prerequisites:** MSG-BACKEND-030, MSG-BACKEND-031, MSG-BACKEND-032 DONE

**Track D adds:**
1. E2E integration tests across all 3 tracks
2. Cross-track workflow validation
3. Production deployment preparation
4. Monitoring and alerting setup

---

## Acceptance Criteria

- [ ] **E2E Integration Tests** (10 scenarios)
  - Public Quote Request → Pricing → Email → Tracking → ShopFloor
  - Cross-track data flow validated
- [ ] **API Contract Validation**
  - All 12 new endpoints documented (OpenAPI)
  - Frontend contracts verified (mock integration)
- [ ] **Database Integration**
  - All migrations tested (rollback + forward)
  - Migration squashing (if needed)
  - Database seeding for 2. ügyfél (Lapszabász KKV)
- [ ] **Deployment Checklist**
  - VPS deployment script updated
  - Environment variables configured
  - Nginx routing updated
  - Rollback plan verified
- [ ] **Monitoring Setup**
  - Email delivery metrics (Brevo dashboard)
  - Pricing calculation metrics (avg time, accuracy)
  - ShopFloor queue metrics (machine utilization)

---

## E2E Integration Tests

### Scenario 1: Full Customer Journey
```
1. Public user submits quote request (Track A)
   → POST /public/cutting/quote-request
2. Backend resolves tenant from subdomain (Track A)
   → TenantResolver.GetTenantIdFromSubdomain()
3. Backend auto-calculates price (Track B)
   → PricingEngine.CalculatePrice()
4. Backend sends email notifications (Track A)
   → EmailService.SendQuoteRequestNotification()
5. Customer tracks quote via trackingToken (Track A)
   → GET /public/cutting/quotes/track/{token}
6. Admin approves quote (auto-price filled) (Track A + B)
   → PUT /api/cutting/quotes/{id}/approve
7. Cutting Plan created → Machine Queue (Track C)
   → POST /api/cutting/shopfloor/jobs/{id}/assign
8. Operator starts job on kiosk (Track C)
   → PUT /api/cutting/shopfloor/jobs/{id}/start
9. Operator completes job (Track C)
   → PUT /api/cutting/shopfloor/jobs/{id}/complete
10. Analytics updated (future)
```

**Expected:** All 10 steps complete without errors, data flows correctly.

### Scenario 2: Pricing Override
```
1. Quote Request → Auto-price calculated
2. Admin reviews price in Trade World (Track B)
3. Admin adjusts price manually (20% increase)
4. Quote approved with adjusted price
5. Email sent with correct (adjusted) price
```

### Scenario 3: Job Failure Handling
```
1. Job assigned to machine (Track C)
2. Operator starts job
3. Machine malfunction → job failed
4. Operator reports failure reason
5. Job re-queued with higher priority
6. Email notification to admin
```

### Additional Scenarios (7 more)
- Subdomain resolution failure → graceful error
- Email delivery failure → retry logic
- Pricing engine unavailable → fallback to manual
- Queue overflow → reject new jobs
- Operator PIN authentication (Track C)
- Concurrent job assignments (2 machines)
- Price calculation edge case (0 cuts, 100+ pieces)

---

## API Contract Validation

### New Endpoints Summary

**Track A (4 endpoints):**
1. `POST /public/cutting/quote-request` — public, no auth
2. `GET /public/cutting/quotes/track/{token}` — public, no auth
3. `PUT /public/cutting/quotes/track/{token}/accept` — public, no auth
4. Internal: TenantResolver (not exposed)

**Track B (4 endpoints):**
5. `POST /api/cutting/pricing/calculate` — auth required
6. `GET /api/cutting/pricing/rules` — auth required
7. `PUT /api/cutting/pricing/rules/{id}` — auth required, admin role
8. `GET /api/cutting/pricing/preview` — public (optional)

**Track C (5 endpoints):**
9. `GET /api/cutting/shopfloor/queue?machineId={id}` — auth required, operator role
10. `POST /api/cutting/shopfloor/jobs/{id}/assign` — auth required, admin role
11. `PUT /api/cutting/shopfloor/jobs/{id}/start` — auth required, operator role
12. `PUT /api/cutting/shopfloor/jobs/{id}/complete` — auth required, operator role
13. `PUT /api/cutting/shopfloor/jobs/{id}/fail` — auth required, operator role

**OpenAPI Documentation:**
```bash
# Generate OpenAPI spec
dotnet swagger tofile --output /tmp/spaceos-cutting-q3.json \
  /opt/spaceos/backend/spaceos-modules-cutting/bin/Release/net8.0/SpaceOS.Modules.Cutting.dll v1
```

---

## Database Integration

### Migration Sequence

```bash
# 1. Track A
dotnet ef migrations add AddTenantSubdomain
dotnet ef migrations add AddEmailTemplates

# 2. Track B
dotnet ef migrations add AddPricingTables

# 3. Track C
dotnet ef migrations add AddMachineQueueTables

# 4. Update database
dotnet ef database update
```

### Migration Squashing (if needed)

If migrations exceed 10 files, squash into single migration:
```bash
# Create squashed migration
dotnet ef migrations add Q3_Cutting_Expansion_Squashed --no-build

# Remove old migrations (Track A, B, C individual files)
rm Migrations/*AddTenantSubdomain* Migrations/*AddPricingTables* Migrations/*AddMachineQueueTables*
```

### Database Seeding

**File:** `Migrations/SeedQ3Data.cs`

```csharp
// Seed data for 2. ügyfél (Lapszabász KKV)
migrationBuilder.Sql(@"
  -- Update Doorstar tenant with subdomain
  UPDATE ""Tenants""
  SET ""Subdomain"" = 'doorstar'
  WHERE ""Name"" = 'Doorstar Kft.';

  -- Create default price list
  INSERT INTO ""PriceLists"" (""Id"", ""TenantId"", ""Name"", ""EffectiveFrom"", ""IsActive"")
  VALUES (gen_random_uuid(), (SELECT ""Id"" FROM ""Tenants"" WHERE ""Subdomain"" = 'doorstar'), 'Default 2026', '2026-01-01', true);

  -- Seed material pricing
  INSERT INTO ""MaterialPricing"" (""Id"", ""PriceListId"", ""MaterialType"", ""PricePerSquareMeter"", ""Currency"")
  VALUES
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'MDF', 4000.00, 'HUF'),
    (gen_random_uuid(), (SELECT ""Id"" FROM ""PriceLists"" WHERE ""Name"" = 'Default 2026'), 'Plywood', 6500.00, 'HUF');

  -- Seed machine (if not exists)
  INSERT INTO ""Workstations"" (""Id"", ""TenantId"", ""Name"", ""Type"")
  VALUES (gen_random_uuid(), (SELECT ""Id"" FROM ""Tenants"" WHERE ""Subdomain"" = 'doorstar'), 'HOLZMA-01', 'CNC Saw')
  ON CONFLICT DO NOTHING;
");
```

---

## Deployment Preparation

### 1. Environment Variables

**File:** `/opt/spaceos/backend/spaceos-modules-cutting/.env.production`

```bash
# Track A
SUBDOMAIN_ENABLED=true
EMAIL_PROVIDER=brevo
BREVO_SMTP_HOST=smtp-relay.brevo.com
BREVO_SMTP_PORT=587
BREVO_API_KEY=${BREVO_API_KEY}  # From secrets

# Track B
PRICING_ENGINE_ENABLED=true
PRICING_BASE_FEE=2500
PRICING_CURRENCY=HUF

# Track C
SHOPFLOOR_ENABLED=true
SHOPFLOOR_POLLING_INTERVAL=5000  # 5 seconds
```

### 2. Nginx Routing Update

**File:** `/etc/nginx/sites-available/spaceos`

```nginx
# Subdomain routing (Track A)
server {
    server_name *.joinerytech.hu;

    location /public/cutting/ {
        proxy_pass http://localhost:5005;
        proxy_set_header X-Original-Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /api/cutting/pricing/ {
        proxy_pass http://localhost:5005;
    }

    location /api/cutting/shopfloor/ {
        proxy_pass http://localhost:5005;
    }
}
```

### 3. Systemd Service Update

**File:** `/etc/systemd/system/spaceos-cutting.service`

```ini
[Unit]
Description=SpaceOS Cutting Module Q3
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/spaceos/backend/spaceos-modules-cutting
ExecStart=/usr/bin/dotnet /opt/spaceos/backend/spaceos-modules-cutting/bin/Release/net8.0/SpaceOS.Modules.Cutting.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=spaceos-cutting
User=spaceos
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

### 4. Deployment Script

**File:** `/opt/spaceos/scripts/deploy-q3-cutting.sh`

```bash
#!/bin/bash
set -euo pipefail

echo "🚀 Deploying Q3 Cutting Module..."

# 1. Backup database
pg_dump -U spaceos spaceos_cutting > /tmp/spaceos_cutting_backup_$(date +%Y%m%d_%H%M%S).sql

# 2. Pull latest code
cd /opt/spaceos
git pull origin main

# 3. Build
cd backend/spaceos-modules-cutting
dotnet build --configuration Release

# 4. Run migrations
dotnet ef database update

# 5. Restart service
sudo systemctl restart spaceos-cutting

# 6. Health check
sleep 5
curl -f http://localhost:5005/health || {
  echo "❌ Health check failed, rolling back..."
  sudo systemctl restart spaceos-cutting
  exit 1
}

echo "✅ Q3 Cutting Module deployed successfully"
```

---

## Monitoring Setup

### 1. Email Delivery Metrics

**Brevo Dashboard:**
- Track delivery rate (target: 100%)
- Track bounce rate (target: <1%)
- Track open rate (customer emails)

**Custom Metrics:**
```csharp
// Add to EmailService
_metrics.Increment("email.sent", new[] { "type:quote_request" });
_metrics.Timing("email.delivery_time", deliveryMs);
```

### 2. Pricing Calculation Metrics

```csharp
// Add to PricingEngine
_metrics.Timing("pricing.calculation_time", calculationMs);
_metrics.Increment("pricing.calculations", new[] { $"material:{materialType}" });
```

### 3. ShopFloor Queue Metrics

```csharp
// Add to MachineQueueService
_metrics.Gauge("shopfloor.queue_length", queueLength, new[] { $"machine:{machineId}" });
_metrics.Timing("shopfloor.job_duration", durationMs);
_metrics.Increment("shopfloor.jobs_completed");
```

---

## Rollback Plan

**See:** `/opt/spaceos/docs/deployment/Q3_ROLLBACK_PLAN.md`

**Quick Rollback:**
```bash
# 1. Revert database
psql -U spaceos spaceos_cutting < /tmp/spaceos_cutting_backup_YYYYMMDD_HHMMSS.sql

# 2. Checkout previous commit
git checkout {previous_commit_sha}

# 3. Rebuild
dotnet build --configuration Release

# 4. Restart
sudo systemctl restart spaceos-cutting
```

---

## Files to Create

1. `Tests/Integration/Q3_FullCustomerJourney_E2E.cs`
2. `Tests/Integration/Q3_PricingOverride_E2E.cs`
3. `Tests/Integration/Q3_JobFailure_E2E.cs`
4. `Migrations/SeedQ3Data.cs`
5. `Scripts/deploy-q3-cutting.sh`
6. `Docs/API_CONTRACTS_Q3.md` (OpenAPI export)

---

## Files to Modify

1. `/etc/nginx/sites-available/spaceos` (VPS)
2. `/etc/systemd/system/spaceos-cutting.service` (VPS)
3. `.env.production` (environment variables)

---

## Testing Requirements

### Integration Tests (Q3_E2E.cs)

1. Full customer journey (10 steps) ✅
2. Pricing override ✅
3. Job failure handling ✅
4. Subdomain resolution failure → graceful error ✅
5. Email delivery failure → retry ✅
6. Pricing engine unavailable → fallback ✅
7. Queue overflow → reject ✅
8. Concurrent job assignments ✅
9. Price calculation edge case ✅
10. Cross-tenant isolation (doorstar vs. tenant2) ✅

**Expected:** All 10 E2E tests pass, no cross-tenant data leaks.

---

## Build & Test Gate

```bash
cd /opt/spaceos/backend/spaceos-modules-cutting

# Build
dotnet build --configuration Release

# Run ALL tests (Unit + Integration)
dotnet test --configuration Release --logger "console;verbosity=detailed"

# Check test count
echo "Expected: 994+ tests (Phase 1) + 50+ tests (Q3) = 1044+ total"
dotnet test --list-tests | wc -l
```

**Expected:** 1044+ tests pass, 0 failures.

---

## Dependencies

**Blocked by:**
- MSG-BACKEND-030 (Track A) DONE
- MSG-BACKEND-031 (Track B) DONE
- MSG-BACKEND-032 (Track C) DONE

**Blocks:**
- Frontend integration (MSG-FRONTEND-018, 019, 020)
- Production deployment

**References:**
- Deployment checklist: `/opt/spaceos/docs/deployment/Q3_DEPLOY_CHECKLIST.md`
- Rollback plan: `/opt/spaceos/docs/deployment/Q3_ROLLBACK_PLAN.md`
- Monitoring guide: `/opt/spaceos/docs/deployment/Q3_MONITORING_GUIDE.md`

---

**Estimated effort:** 1 day (8 hours)
**Model:** sonnet
**Priority:** HIGH
