---
id: MSG-INFRA-059
from: root
to: infra
type: task
priority: critical
status: READ
model: sonnet
created: 2026-06-17
---

# INFRA-059 — Fix https://joinerytech.hu/ 403 Forbidden

## Problem

https://joinerytech.hu/ returns **403 Forbidden** from nginx.

## Task

SSH to VPS and fix nginx config:

1. Check nginx config: `cat /etc/nginx/sites-enabled/joinerytech*`
2. Check document root exists and has correct permissions
3. Deploy frontend build to VPS document root
4. Test: `curl -I https://joinerytech.hu/`

## Expected Result

https://joinerytech.hu/ returns 200 OK with the JoineryTech Portal.

## Priority

CRITICAL - blocks Doorstar demo.
