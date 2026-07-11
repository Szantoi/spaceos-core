# Datahaven Dashboard API 502 Bad Gateway

**Dátum:** 2026-07-02
**Prioritás:** 🟡 Medium
**Komponens:** datahaven-web (nginx/backend)
**Státusz:** Intermittent issue

## Probléma

A Datahaven Dashboard API endpoint-ok időnként **502 Bad Gateway** hibát adnak vissza.

### Tünet

```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -d '{"terminal":"root","status":"working"}'

# Válasz:
<html>
<head><title>502 Bad Gateway</title></head>
<body>
<center><h1>502 Bad Gateway</h1></center>
<hr><center>nginx</center>
</body>
</html>
```

### Időpontok

- **2026-07-02 04:32** - Root session start - 502 error
- **Session közben** - API calls időnként működtek, időnként nem

### Következmény

- Terminal status updates nem kerülnek be a Dashboard-ba
- Real-time monitoring megszakad
- Agent koordináció nehezített (nem látszik ki dolgozik)

## Lehetséges okok

### 1. Backend process halt/restart

```bash
# Check backend process
ps aux | grep "datahaven-web/src/server.js"
# PID 1784359 - running on port 3457
```

**Megfigyelés:** A backend process futott, de lehet hogy:
- Túlterhelt (sok request)
- Memory spike
- Unhandled exception → crash → restart

### 2. Nginx upstream timeout

```nginx
# /etc/nginx/sites-available/datahaven.joinerytech.hu
upstream datahaven_backend {
    server localhost:3457;
    # Nincs timeout konfiguráció!
}
```

**Hiányzó konfigurációk:**
- `proxy_connect_timeout`
- `proxy_send_timeout`
- `proxy_read_timeout`

### 3. Port confusion

**Észrevétel a session során:**
- Knowledge-service: **3456** (environment variable)
- Datahaven backend: **3457** (tényleges futó port)
- Knowledge-service **NEM futott** a 3456-on → crash-elt induláskor (TypeScript import hiba)

**Lehet, hogy:**
- Nginx upstream 3456-ra mutat, de 3457 fut
- Vagy fordítva

## Diagnosztika

### 1. Nginx config ellenőrzés

```bash
cat /etc/nginx/sites-available/datahaven.joinerytech.hu | grep -A5 "upstream"
cat /etc/nginx/sites-available/datahaven.joinerytech.hu | grep "proxy_pass"
```

### 2. Backend logs

```bash
# Datahaven backend logs
tail -100 /opt/spaceos/datahaven-web/logs/server.log

# Nginx error log
tail -100 /var/log/nginx/error.log | grep "datahaven"
```

### 3. Port verification

```bash
# Which process listens on which port?
lsof -iTCP -sTCP:LISTEN -n -P | grep -E "3456|3457"

# Expected:
# node (datahaven) → 3457
# node (knowledge-service) → 3456
```

## Javasolt javítás

### 1. Nginx timeout növelés

```nginx
# /etc/nginx/sites-available/datahaven.joinerytech.hu
upstream datahaven_backend {
    server localhost:3457 max_fails=3 fail_timeout=30s;
}

server {
    location /api/ {
        proxy_pass http://datahaven_backend;
        proxy_connect_timeout 10s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
        proxy_next_upstream error timeout http_502 http_503 http_504;
    }
}
```

### 2. Health check endpoint monitoring

```bash
# Cron job every 1 minute
*/1 * * * * curl -s http://localhost:3457/health || systemctl restart datahaven-backend
```

### 3. Backend process manager (PM2)

```bash
# Instead of nohup
npm install -g pm2

# Start with PM2
pm2 start /opt/spaceos/datahaven-web/src/server.js --name datahaven-backend

# Auto-restart on crash
pm2 startup
pm2 save
```

### 4. Logging és alerting

```javascript
// datahaven-web/src/server.js
process.on('uncaughtException', (err) => {
  console.error('UNCAUGHT EXCEPTION:', err);
  // Send alert to Telegram
  // Restart gracefully
});

process.on('unhandledRejection', (reason, promise) => {
  console.error('UNHANDLED REJECTION:', reason);
});
```

## Testing Checklist

- [ ] Nginx config has proper timeouts
- [ ] Backend runs on correct port (3457 or 3456?)
- [ ] Health check returns 200 OK consistently
- [ ] Load test: 100 requests/second → no 502
- [ ] PM2 auto-restart works
- [ ] Error logs captured and alerted

## Workaround

**Jelenleg működik:**
- Lokális knowledge-service API (localhost:3456) helyettesítő használata
- Terminal status updates MCP-n keresztül

## Priority

**MEDIUM** - Nem blokkoló (van workaround), de monitoring kritikus production-ben.

## Next Steps

1. Nginx config audit
2. Backend health check + alerting
3. PM2 process manager setup
4. Load testing
