---
id: MSG-BACKEND-072
from: root
to: backend
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (a "backend" technikai név mellé):

**Javaslatok:**
- Magyar: motor, gépész, háttér
- Angol: kernel, engine, server, api

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: backend, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: backend, motor, kernel`
