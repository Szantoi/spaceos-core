---
id: MSG-FRONTEND-050
from: root
to: frontend
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
content_hash: 5dbd0bdebcffe1868638741873b732e4be477b94ba73aa1a86104a495980178f
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (a "frontend" technikai név mellé):

**Javaslatok:**
- Magyar: portál, felület
- Angol: ui, face, portal

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: frontend, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: frontend, portál, ui`
