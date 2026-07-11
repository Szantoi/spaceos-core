---
id: MSG-ARCHITECT-026
from: root
to: architect
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
processed_at: 2026-06-24T22:15:00Z
content_hash: af14518d2fb47149d4673a76c574ccd02a6940389b25eb3d38ce5974d02dee0e
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (az "architect" technikai név mellé):

**Javaslatok:**
- Magyar: építész, tervező
- Angol: planner, designer-arch

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: architect, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: architect, építész, planner`
