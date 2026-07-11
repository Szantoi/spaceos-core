# SpaceOS Q3 Cutting Expansion - Monitoring & Logging Guide

> **Version:** 1.0.0
> **Date:** 2026-06-23

---

## Service Monitoring

### Systemd Service Status

```bash
# Check all SpaceOS services
sudo systemctl status spaceos-modules-*

# Individual service status
sudo systemctl status spaceos-modules-pricing
sudo systemctl status spaceos-modules-cutting
sudo systemctl status spaceos-modules-identity
```

**Healthy service indicators:**
- Status: `active (running)`
- Uptime: >0s
- Memory usage: <500 MB (per service)
- No recent restarts

---

## Log Monitoring

### Pricing Module Logs

```bash
# Follow live logs
journalctl -u spaceos-modules-pricing -f

# Last 100 lines
journalctl -u spaceos-modules-pricing -n 100 --no-pager

# Errors only (last hour)
journalctl -u spaceos-modules-pricing --since "1 hour ago" -p err --no-pager

# Filter by keyword
journalctl -u spaceos-modules-pricing | grep -i "price list"
```

**Key metrics to watch:**
- `PriceListCreated` events
- `QuotePriceCalculated` events
- Exceptions (stack traces)
- Performance warnings (>1s response time)

---

### Cutting Module Logs

```bash
# Follow live logs
journalctl -u spaceos-modules-cutting -f

# Public quote requests
journalctl -u spaceos-modules-cutting | grep "PublicQuoteRequestSubmitted"

# ShopFloor events
journalctl -u spaceos-modules-cutting | grep -E "OperatorSession|BatchProduction"
```

**Key metrics to watch:**
- `PublicQuoteRequestSubmitted` count
- `QuoteGeneratedEvent` count
- `BatchProductionStarted` / `BatchProductionCompleted` events
- Rate limit triggers (HTTP 429 responses)

---

### Nginx Access Logs

```bash
# Follow live access log
sudo tail -f /var/log/nginx/access.log

# Filter Q3 endpoints
sudo tail -f /var/log/nginx/access.log | grep -E "/pricing/|/cutting/api/public/|/shopfloor/"

# Count quote requests per hour
sudo grep "/cutting/api/public/quote-requests" /var/log/nginx/access.log | \
  grep "$(date +%Y-%m-%d)" | wc -l

# Check rate limit triggers (HTTP 429)
sudo grep " 429 " /var/log/nginx/access.log | tail -20
```

**Expected patterns:**
- `/pricing/health` — every 30s (if monitoring enabled)
- `/cutting/api/public/quote-requests` — sporadic, <10/hour per IP
- `/cutting/api/shopfloor/` — steady during production shifts

---

### Nginx Error Logs

```bash
# Follow live error log
sudo tail -f /var/log/nginx/error.log

# Recent errors (last 100 lines)
sudo tail -100 /var/log/nginx/error.log

# Upstream errors (backend connection issues)
sudo grep "upstream" /var/log/nginx/error.log | tail -20
```

**Red flags:**
- `502 Bad Gateway` — backend service down
- `upstream timed out` — backend slow response (>60s)
- `connect() failed` — backend not listening on port

---

## Performance Metrics

### Key Metrics to Track

| Metric | Expected Value | Alert Threshold |
|---|---|---|
| Public quote submissions | <10/hour | >50/hour (possible spam) |
| Quote generation latency | <2 seconds | >5 seconds |
| Price calculation latency | <500ms | >2 seconds |
| Machine queue depth | 0-5 batches | >20 batches (backlog) |
| Operator session duration | 4-8 hours | >10 hours (forgot logout) |
| Rate limit triggers | 1-2/day | >10/day (review limit) |

### Database Performance

```bash
# Connect to database
psql -U spaceos -d spaceos

# Active connections
SELECT count(*) FROM pg_stat_activity WHERE datname = 'spaceos';

# Long-running queries (>10s)
SELECT pid, now() - query_start AS duration, query
FROM pg_stat_activity
WHERE state = 'active' AND now() - query_start > interval '10 seconds';

# Table sizes (Pricing module)
SELECT schemaname, tablename, pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'spaceos_pricing'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

## Alerts & Notifications

### Critical Alerts

**Service Down:**
```bash
# Check service status every 5 minutes (cron)
*/5 * * * * systemctl is-active spaceos-modules-pricing || echo "Pricing service DOWN!" | mail -s "ALERT: Pricing Down" ops@joinerytech.hu
```

**High Error Rate:**
```bash
# Check error logs every 15 minutes
*/15 * * * * [ $(journalctl -u spaceos-modules-pricing --since "15 min ago" -p err | wc -l) -gt 10 ] && echo "High error rate in Pricing module" | mail -s "ALERT: Pricing Errors" ops@joinerytech.hu
```

### Warning Alerts

**Disk Space:**
```bash
# Check disk usage daily
0 8 * * * df -h /opt/spaceos | awk '$5+0 > 80 {print "Disk usage above 80%: " $0}' | mail -s "WARNING: Disk Space" ops@joinerytech.hu
```

**Database Size:**
```bash
# Check database size weekly
0 9 * * 1 psql -U spaceos -d spaceos -c "SELECT pg_size_pretty(pg_database_size('spaceos'));" | mail -s "INFO: DB Size" ops@joinerytech.hu
```

---

## Troubleshooting Common Issues

### Issue 1: Pricing Module Returns 500

**Symptoms:** `/pricing/api/*` endpoints return HTTP 500

**Diagnosis:**
```bash
journalctl -u spaceos-modules-pricing -n 100 --no-pager | grep -A 5 "Exception"
```

**Common causes:**
- Database connection failure → check PostgreSQL service
- RLS policy missing → verify migration ran successfully
- Invalid price list data → check validation errors

---

### Issue 2: Rate Limit Not Working

**Symptoms:** Public quote endpoint accepts >10 requests/hour

**Diagnosis:**
```bash
# Check nginx config
sudo nginx -T | grep -A 5 "quote_limit"

# Check rate limit zone
sudo ls -lh /var/lib/nginx/quote_limit/
```

**Common causes:**
- Rate limit zone not configured in nginx.conf
- Nginx config not reloaded after changes
- IP bypassing via proxy (check X-Forwarded-For)

---

### Issue 3: Operator Can't Login (Kiosk)

**Symptoms:** Kiosk login returns 401

**Diagnosis:**
```bash
# Check Identity module logs
journalctl -u spaceos-modules-identity | grep -i "operator"

# Verify OperatorPin in database
psql -U spaceos -d spaceos -c "SELECT id, email, operator_pin FROM spaceos_identity.spaceos_users WHERE operator_pin IS NOT NULL;"
```

**Common causes:**
- OperatorPin not set for user
- PIN validation logic error
- OperatorSession creation failure

---

## Dashboard Recommendations

**Recommended monitoring tools:**
- **Grafana** — metrics dashboards
- **Prometheus** — time-series metrics
- **Loki** — log aggregation
- **Uptime Kuma** — uptime monitoring

**Key dashboards to create:**
1. Q3 Quote Request Funnel (submissions → quotes → orders)
2. Pricing Calculation Performance
3. ShopFloor Production Metrics
4. Service Health Overview

---

## Log Retention Policy

| Log Type | Retention | Rotation |
|---|---|---|
| Systemd journals | 30 days | Daily |
| Nginx access logs | 90 days | Weekly |
| Nginx error logs | 90 days | Weekly |
| Database logs | 14 days | Daily |

```bash
# Configure systemd journal retention
sudo nano /etc/systemd/journald.conf
# Set: MaxRetentionSec=30day
```

---

## Monitoring Checklist (Daily)

- [ ] All services running (pricing, cutting, identity)
- [ ] No critical errors in last 24h
- [ ] Disk usage <80%
- [ ] Database connections <50
- [ ] Rate limit functioning correctly
- [ ] Public quote submissions <100/day (adjust based on traffic)

---

**Last updated:** 2026-06-23
**Owner:** Backend Terminal
