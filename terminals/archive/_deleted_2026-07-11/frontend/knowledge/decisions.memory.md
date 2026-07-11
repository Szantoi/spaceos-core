# Frontend Decisions Memory

> **Architectural decisions cache** — Cold memory (365d TTL)
>
> Frontend-specifikus döntések és trade-off-ok.

## Framework Decisions

### React Version
**Decision:** React 18 (not 19 yet)
**Rationale:** Ecosystem stability, library compatibility

### State Management
**Decision:** Context API + custom hooks
**Rationale:** Simple state, avoid Redux overhead

### Build Tool
**Decision:** Vite
**Rationale:** Fast HMR, modern ES modules

## Routing Decisions

### Authentication Flow
**Decision:** AuthOverlay component pattern
**Rationale:** Centralized auth handling, redirect management

### Protected Routes
**Decision:** useAuth hook + conditional rendering
**Rationale:** Simple, declarative

## Performance Decisions

### Code Splitting
**Decision:** Route-based lazy loading
**Rationale:** Reduce initial bundle size

### Image Optimization
**Decision:** Lazy loading + WebP format
**Rationale:** Bandwidth vs quality trade-off

---

**Last updated:** YYYY-MM-DD
