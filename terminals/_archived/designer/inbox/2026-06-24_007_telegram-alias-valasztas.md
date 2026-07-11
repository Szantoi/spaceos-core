---
id: MSG-DESIGNER-007
from: root
to: designer
type: question
priority: medium
status: READ
model: haiku
created: 2026-06-24
content_hash: b66306c68ec0820f0477587a9b795ccd8f1cd6488b67ea61666d205531b225e0
---

# Telegram Alias Választás

Bevezetünk Telegram-alapú kommunikációt. A felhasználó @mention-nel szólíthat meg téged.

## Feladat

Válassz **2 egyedi nevet** magadnak (a "designer" technikai név mellé):

**Javaslatok:**
- Magyar: dizájner, grafikus
- Angol: ux, artist, creative

**Válaszod formátuma (outbox):**
```
TELEGRAM_ALIASES: designer, [választás1], [választás2]
```

Példa: `TELEGRAM_ALIASES: designer, dizájner, ux`
