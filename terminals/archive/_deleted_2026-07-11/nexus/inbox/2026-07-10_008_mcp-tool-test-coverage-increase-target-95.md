---
id: MSG-NEXUS-008
from: root
to: nexus
type: task
priority: low
status: UNREAD
model: haiku
created: 2026-07-10
content_hash: 1f6690e127b9fd4c8c0b895e9fabb2440f9fc3a23461b6fbabedbd103a63cb0f
---

# MCP Tool Test Coverage Increase — Target 95%

## Kontextus

Az MCP_TOOLS_CATALOGUE.md szerint a teszt coverage:
- Terminal Status Aggregator: 94%
- Dependency Resolver: 91%
- Context Transfer: 93%
- Component Scaffold: 96%
- Pattern Matcher: **89%** ← legalacsonyabb

**Cél:** Minden tool > 95% coverage

## Feladat

### 1. Pattern Matcher Coverage (89% → 95%)
- Edge case-ek: üres description, unknown domain
- Vector search fallback tesztek
- Error handling tesztek

### 2. Dependency Resolver Coverage (91% → 95%)
- Circular dependency detection tesztek
- Missing epic handling
- Empty EPICS.yaml scenario

### 3. Integration Tests
- MCP tool → HTTP → Handler → Response flow
- Error propagation tesztek

## Érintett Fájlok
- `src/__tests__/unit/domainPatternMatcher.test.ts`
- `src/__tests__/unit/dependencyResolver.test.ts`
- `src/__tests__/integration/mcp-tools.integration.test.ts`

## Acceptance Criteria
- [ ] Pattern Matcher: ≥95% coverage
- [ ] Dependency Resolver: ≥95% coverage
- [ ] Integration tests futnak
- [ ] `npm test` all green

## Acceptance Criteria

- [ ] Pattern Matcher: ≥95% coverage
- [ ] Dependency Resolver: ≥95% coverage
- [ ] Integration tests futnak
- [ ] npm test all green
