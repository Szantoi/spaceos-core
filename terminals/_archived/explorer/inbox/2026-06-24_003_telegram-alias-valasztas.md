---
id: MSG-EXPLORER-003
from: root
to: explorer
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
processed: 2026-06-24
content_hash: 14d83cf8f531288a1c860e1bd065db511373018a72de1e9f3f09d612312fab58
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (az "explorer" technikai név mellé):

**Javaslatok:**
- Magyar: felfedező, kutató
- Angol: scout, researcher

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: explorer, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: explorer, felfedező, scout`
