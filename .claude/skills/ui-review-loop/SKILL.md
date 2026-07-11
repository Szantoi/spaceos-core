# UI Review Loop Skill

> **Cél:** Designer és Frontend terminálok közötti koordinált UI review workflow, ahol a Designer ellenőrzi, hogy a Frontend implementáció megfelel-e a JoineryTech UI terveknek.

---

## Trigger Patterns

- "UI review", "design review", "check UI implementation"
- "ellenőrizd a frontendet", "UI megfelelés"
- "screenshot review", "visual comparison"

---

## Workflow Áttekintés

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Designer  │────▶│   Review    │────▶│   Frontend  │
│   Review    │     │   Feedback  │     │   Fix/Done  │
└─────────────┘     └─────────────┘     └─────────────┘
       │                  │                    │
       ▼                  ▼                    ▼
  Screenshot +       Issue List +         Implementation
  Comparison        Priority Tags          + Re-review
```

---

## 1. Designer Review Phase

### 1.1 Screenshot Capture (Playwright CLI)

```bash
# Capture current implementation
npx playwright screenshot http://localhost:5173/<route> /tmp/review-<page>.png

# Routes to check:
# - /dashboard/crm/leads
# - /dashboard/kontrolling
# - /dashboard/ehs
# - /dashboard/hr
# - /dashboard/maintenance
# - /dashboard/qa
# - /dashboard/dms
```

### 1.2 Reference Materials

**JoineryTech UI Prototípus:**
- Lokáció: `docs/tasks/new/joinerytech/`
- Screenshots: `docs/tasks/new/joinerytech/screenshots/`
- JSX komponensek: `docs/tasks/new/joinerytech/page-*.jsx`
- CLAUDE.md: Teljes UI specifikáció

**Datahaven React App:**
- Lokáció: `datahaven-web/client/src/`
- Pages: `datahaven-web/client/src/pages/`
- Components: `datahaven-web/client/src/components/`

### 1.3 Review Checklist

```markdown
## UI Review Checklist - [Page Name]

### Layout & Structure
- [ ] Grid/layout matches spec
- [ ] Responsive breakpoints correct
- [ ] Spacing/padding consistent

### Components
- [ ] KPI cards present and styled
- [ ] Tables have correct columns
- [ ] Forms have all required fields
- [ ] Modals/SlideOvers work correctly

### Visual Design
- [ ] Colors match brand palette
- [ ] Typography correct
- [ ] Icons present and correct
- [ ] Dark theme consistent

### Functionality
- [ ] Navigation works
- [ ] Filters functional
- [ ] Actions trigger correctly
- [ ] Loading states present

### Data
- [ ] API endpoints connected
- [ ] Mock data removed
- [ ] Error states handled
```

---

## 2. Feedback Message Format

Designer creates feedback in outbox with this structure:

```yaml
---
id: MSG-DESIGNER-NNN
from: designer
to: frontend
type: review
priority: high
status: UNREAD
ref: [original task ID]
created: YYYY-MM-DD
---

# UI Review: [Page/Component Name]

## Summary
[1-2 sentence overview]

## Screenshot Comparison
- Current: `/tmp/review-<page>.png`
- Reference: `docs/tasks/new/joinerytech/screenshots/<ref>.png`

## Issues Found

### Critical (blocking)
1. [Issue description] - [file:line if known]

### Major (should fix)
1. [Issue description]

### Minor (nice to have)
1. [Issue description]

## Verdict
- [ ] APPROVED - Ready for production
- [ ] CHANGES REQUESTED - See issues above
- [ ] BLOCKED - Missing backend dependency

## Next Steps
[Specific actions for Frontend]
```

---

## 3. MCP Tool Usage

### Screenshot és Review

```
# Designer: Capture screenshot
Bash: npx playwright screenshot <URL> /tmp/review.png

# Designer: Read reference spec
Read: docs/tasks/new/joinerytech/page-<name>.jsx

# Designer: Send feedback to Frontend
mcp__spaceos-knowledge__create_task
  from: "designer"
  to: "frontend"
  title: "UI Review: [Component]"
  description: "[Markdown feedback]"
  priority: "high"
```

### Frontend Response

```
# Frontend: Acknowledge review
mcp__spaceos-knowledge__ack_task
  terminal: "frontend"
  message_id: "MSG-DESIGNER-NNN"

# Frontend: After fixes, request re-review
mcp__spaceos-knowledge__create_task
  from: "frontend"
  to: "designer"
  title: "Re-review: [Component]"
  description: "Issues fixed, please re-review"
  priority: "medium"
```

---

## 4. Review Loop States

```
┌──────────────────────────────────────────────────────────┐
│                    UI Review Loop FSM                     │
├──────────────────────────────────────────────────────────┤
│                                                          │
│   PENDING_REVIEW ──▶ REVIEWING ──▶ CHANGES_REQUESTED    │
│         ▲                              │                 │
│         │                              ▼                 │
│         └────────── IN_PROGRESS ◀──────┘                 │
│                          │                               │
│                          ▼                               │
│                      APPROVED                            │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## 5. Priority Modules (JoineryTech)

Based on `FE_Design_Requirements_2026.md`:

| Priority | Module | Status | Notes |
|----------|--------|--------|-------|
| 1 | CRM Leads/Opportunities | In Progress | Basic layout exists |
| 2 | Kontrolling Dashboard | In Progress | Widgets loading |
| 3 | EHS (Munkavédelem) | Pending | Full spec in prototípus |
| 4 | HR Dashboard | Pending | |
| 5 | Maintenance | Pending | |
| 6 | QA Dashboard | Pending | |
| 7 | DMS | Pending | |

---

## 6. Collaboration Pattern

### Daily Standup (Optional)

Designer és Frontend közös review:

```bash
# Designer captures all key pages
for page in crm kontrolling ehs hr maintenance qa dms; do
  npx playwright screenshot http://localhost:5173/dashboard/$page /tmp/review-$page.png
done

# Share via MCP task
mcp__spaceos-knowledge__create_task
  from: "designer"
  to: "frontend"
  title: "Daily UI Review - $(date +%Y-%m-%d)"
  description: "Screenshots captured, issues listed below..."
```

### Escalation

Ha 2+ iteration után sem egyezik:

```
mcp__spaceos-knowledge__create_task
  from: "designer"
  to: "architect"
  title: "UI Review Escalation: [Component]"
  description: "Frontend and Designer cannot agree on..."
  priority: "high"
```

---

## 7. Acceptance Criteria Template

```markdown
## Acceptance Criteria - [Feature]

### Visual Match
- [ ] Layout matches `screenshots/<ref>.png`
- [ ] Components render as in `page-<name>.jsx`
- [ ] Responsive at 1920px, 1440px, 768px

### Functional
- [ ] All user flows work
- [ ] API integration complete
- [ ] Error handling present

### Performance
- [ ] Page loads < 2s
- [ ] No console errors
- [ ] Lighthouse score > 80

### Sign-off
- [ ] Designer APPROVED
- [ ] QA tested (if applicable)
```

---

## References

- JoineryTech Prototípus: `docs/tasks/new/joinerytech/`
- Design Requirements: `docs/tasks/new/FE_Design_Requirements_2026.md`
- React App: `datahaven-web/client/src/`
- Playwright CLI: `npx playwright screenshot <URL> <output>`
