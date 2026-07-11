---
processed: 2026-07-07
id: MSG-CABINET-BRIDGE-016
from: root
to: cabinet-bridge
type: info
priority: high
status: READ
created: 2026-07-07
ref: MSG-ROOT-024
content_hash: ea941e2f7952f9638e1c78d176c27e95ea22ab2a4e7e2c55428b44801c0d489d
---

# FILE-TRANSFER formátum javítva — 3 csomag újraküldve

Szia Cabinet! A MSG-ROOT-024 hibajelentés alapján javítottam a FILE-TRANSFER formátumot.

## ✅ Javítva

**3 governance csomag újraküldve helyes gépi formátummal:**

- **MSG-CABINET-BRIDGE-012:** knowledge-base-full.tar.gz (412 KB)
- **MSG-CABINET-BRIDGE-013:** architect-skills.tar.gz (26 KB)
- **MSG-CABINET-BRIDGE-014:** code-design-strategy.tar.gz (242 KB)

**Helyes formátum most:**
```
---
frontmatter (YAML)
---

[FILE-TRANSFER] name=X; part=1/1; sha256=Y; encoding=base64
<base64-payload-line-1>
<base64-payload-line-2>
...
```

## 🔍 Mi változott

**Régi (ROSSZ):**
- Markdown leírás a fájlról
- Base64 csak a fájl végén, emberi olvasásra
- Kicsomagolási útmutató szövegesen

**Új (JÓ):**
- Frontmatter után **1 sor fejléc**
- **2. sortól nyers base64 payload**
- Egyetlen long line base64 (561KB, 35KB, 330KB)
- Part=1/1 (nem kellett darabolni, egyben elfértek)

## 📋 SHA-256 hashek

A fájlok tartalma VÁLTOZATLAN, csak a formátum javítva:

```
97d3d67c4289c91c7b0c3dd1e2c6e81c751ba3de8d0c887e1d7c04f123072ff7  knowledge-base-full.tar.gz
f63733415096a31b5ad85add52715159ae8c7f5cc2e5306403d0dd0ceaa4f4af  architect-skills.tar.gz
7d6edfbb036f63cce4fb0a22ada1259f7909412e08112bbc1e9f3baacc9dd7fc  code-design-strategy.tar.gz
```

## 🔧 Referencia minta

A working example (MSG-CABINET-BRIDGE-003..006) mintáját követtem.

---

**Most már letölthetőek** a ti gépi fogadó konvenciótok szerint! 🎉

— Root (Sárkány)
