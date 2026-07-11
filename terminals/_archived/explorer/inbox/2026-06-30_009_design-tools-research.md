---
id: MSG-EXPLORER-009
from: root
to: explorer
type: task
priority: high
status: READ
read: 2026-07-01
model: haiku
created: 2026-06-30
content_hash: 6132ed3359ef40ee18c558f1526c5fa220938724c6dca0a19b329c8e66aabf79
---

# Design Eszközök Kutatás — Vízió Terminál

## Cél

Keress eszközöket és MCP tool-okat a Designer (Vízió) terminál számára, amelyekkel UI/UX elemzést és javítást végezhet.

## Kutatási Területek

### 1. Figma MCP Integration
- Van-e `figma-mcp` vagy hasonló MCP server?
- Lehet-e Figma design-okat elemezni Claude-dal?
- Export/import lehetőségek

### 2. Screenshot Analysis Tools
- Playwright screenshot → Claude vision elemzés
- Automatikus UI audit workflow
- Accessibility ellenőrzés

### 3. CSS/Design System Tools
- Design token generátorok
- CSS változók kinyerése
- Színpaletta elemzés

### 4. UX Audit Eszközök
- Lighthouse integráció
- Accessibility audit (axe-core)
- Mobile responsiveness tesztelés

## Elvárt Output

`terminals/explorer/outbox/2026-06-30_XXX_design-tools-research-done.md`

```markdown
# Design Tools Research — Vízió Terminál

## MCP Tools

| Tool | Típus | Install | Használat |
|------|-------|---------|-----------|
| figma-mcp | Figma read | npm | Design export |
| playwright | Screenshot | npm | UI capture |

## Ajánlott Workflow

1. Screenshot készítés (Playwright)
2. Vision elemzés (Claude)
3. CSS audit (Stylelint)
4. Accessibility check (axe)

## Integration Points

- Datahaven-web: `/public/css/` elemzés
- Planning.html: UI consistency audit
```

## Constraint

- 30 perc kutatás
- Fókusz: MCP integrálható eszközök
- DONE outbox amikor kész
