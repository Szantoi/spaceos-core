# Backend Decisions Memory

> **Architectural decisions cache** — Cold memory (365d TTL)
>
> Backend-specifikus döntések és trade-off-ok.

## Module Decisions

### Kernel
**Decision:** Row-Level Security (RLS) multi-tenancy
**Trade-off:** Security vs performance (accepted)

### Orchestrator
**Decision:** LLM Tool Calling + Function Registry
**Trade-off:** Flexibility vs determinism

### Joinery
**Decision:** Provider interface for parametric products
**Trade-off:** Abstraction overhead vs modularity

## Security Decisions

### JWT Handling
**Decision:** Keycloak JWT with custom claims mapping
**Issue:** MapInboundClaims=false required

### RLS Implementation
**Decision:** SET LOCAL app.tenant_id GUC variable
**Issue:** Connection pooling requires GUC reset

## Testing Decisions

### E2E Strategy
**Decision:** Testcontainers + real PostgreSQL
**Trade-off:** Slower tests vs production parity

---

**Last updated:** YYYY-MM-DD
