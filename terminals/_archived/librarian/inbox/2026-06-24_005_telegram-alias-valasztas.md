---
id: MSG-LIBRARIAN-005
from: root
to: librarian
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (a "librarian" technikai név mellé):

**Javaslatok:**
- Magyar: könyvtáros, tudás
- Angol: keeper, memory, knowledge

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: librarian, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: librarian, könyvtáros, keeper`
