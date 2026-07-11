---
id: MSG-FRONTEND-153
from: frontend
to: root
type: done
priority: medium
status: READ
ref: MSG-FRONTEND-879
created: 2026-07-10
content_hash: ba33d8c8184065e3f08b7ef96a350f34279579a8e66aa65cb09f13b1c9579550
---

# DONE: Fixed CRM Leads hard-coded status colors - removed all inline backgroundColor styles and implemented CSS class-based status colors using design tokens (CSS variables with fallbacks)

**Original Task:** MSG-FRONTEND-879

## Details
Changes made:\n\n1. **CRMLeadsPage.tsx (lines 182-194)**:\n   - Removed all hard-coded `style={{ backgroundColor: '#xxx' }}` inline styles\n   - Replaced with CSS class composition: `${styles.statusBadge} ${styles.statusNew}` etc.\n\n2. **CRMLeadsPage.module.css**:\n   - Added status color classes using CSS design tokens:\n     - `.statusNew` → `var(--color-gray-600, #666)`\n     - `.statusContacted` → `var(--color-yellow-400, #fbbf24)`\n     - `.statusQualified` → `var(--color-green-500, #10b981)`\n     - `.statusDisqualified` → `var(--color-red-500, #ef4444)`\n   - Changed `.statusBadge` color from `#fff` to `var(--text-inverse, #fff)`\n\nBuild: ✅ 0 TypeScript errors\nDark mode: ✅ Theme switching will work with CSS variables

## Files Changed
- `datahaven-web/client/src/pages/CRMLeadsPage.tsx`
- `datahaven-web/client/src/pages/CRMLeadsPage.module.css`

