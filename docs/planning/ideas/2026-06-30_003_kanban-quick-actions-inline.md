---
id: IDEA-2026-06-30-003
title: "Kanban Card Quick Actions - Inline Operations"
category: ux
priority: medium
effort: small
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló

Inline quick action gombok a Kanban kártyákon (assign, priority change, mark blocked) modal megnyitás nélkül.

## Probléma

Jelenleg minden kártya interakcióhoz modal-t kell nyitni → lassú workflow. Egyszerű műveletek (priority váltás, terminál re-assign) túl sok kattintást igényelnek.

## Megoldás

**Quick Action Bar (card hover):**

```
┌────────────────────────────────────────┐
│ MSG-BACKEND-042: EHS Module v1        │
│ Priority: HIGH  Assigned: Backend     │
│                                        │
│ [🔼 Priority] [👤 Assign] [🚫 Block] │
└────────────────────────────────────────┘
```

**Műveletek:**
1. **Priority toggle** - Dropdown: Critical/High/Medium/Low
2. **Assign** - Terminál dropdown: backend/frontend/architect/stb.
3. **Block** - Quick block modal (reason textarea + save)
4. **Mark DONE** - Checkbox toggle (csak Delivery track-en)

**UI Pattern:**
- Hover → Quick Action Bar fade in (bottom of card)
- Click action → Inline update + API call
- Success → Card re-render + notification toast
- Error → Error toast + rollback

**API calls:**
```
PATCH /api/task/{id}/priority
PATCH /api/task/{id}/assign
POST  /api/task/{id}/block
POST  /api/task/{id}/complete
```

## Acceptance Criteria

- [ ] Quick Action Bar megjelenik card hover-re
- [ ] Priority dropdown működik (4 opció)
- [ ] Assign dropdown működik (7 terminál)
- [ ] Block quick modal működik (reason + save)
- [ ] Mark DONE checkbox működik
- [ ] Inline update + re-render < 300ms
- [ ] Error toast + rollback működik
- [ ] Mobile touch támogatás (long-press trigger)
