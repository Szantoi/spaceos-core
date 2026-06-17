# Review Checklist Reference

Used in every review pass of the ddd-arch-planner pipeline.
A finding is raised for any violated rule.

---

## Database / Schema Rules (DB-01..DB-12)

| ID | Rule |
|----|------|
| DB-01 | Integer columns for measurements (mm), not DECIMAL/FLOAT |
| DB-02 | JSONB columns have CHECK constraints for required keys |
| DB-03 | Composite indexes cover actual query predicates |
| DB-04 | Partial indexes used for soft-delete filtered queries |
| DB-05 | UNIQUE constraints on natural keys (not just PK) |
| DB-06 | Foreign keys have ON DELETE behavior explicitly set |
| DB-07 | RLS enabled on all tenant-scoped tables |
| DB-08 | FORCE ROW LEVEL SECURITY on tables to prevent owner bypass |
| DB-09 | Table ownership assigned to dedicated schema role (not superuser) |
| DB-10 | Self-referencing FK has NO SELF LOOP CHECK constraint |
| DB-11 | `try_cast_uuid()` helper used for external input UUID casting |
| DB-12 | EXPLAIN ANALYZE shows Index Scan (no Seq Scan) on all query endpoints |

---

## Security Rules (SEC-01..SEC-13)

| ID | Rule |
|----|------|
| SEC-01 | No plaintext secrets in DB — Key Vault refs only |
| SEC-02 | JWT uses asymmetric signing (ES256 / RS256) — no HS256 |
| SEC-03 | TenantId sourced from JWT claim only — never HTTP header |
| SEC-04 | Cross-tenant data link triggers reject mismatched TenantId |
| SEC-05 | Audit table has DB-level INSERT-only role (no UPDATE/DELETE) |
| SEC-06 | Hash chain race condition prevented (advisory lock or single-writer) |
| SEC-07 | External write-only sink for audit hashes (WORM / Object Lock) |
| SEC-08 | Proof files stored with hash (ProofHash) — URL alone insufficient |
| SEC-09 | Input validation on all JSONB payloads (schema + depth + size) |
| SEC-10 | Rate limiting uses real IP (ForwardedHeaders, not 127.0.0.1) |
| SEC-11 | Redis AUTH + TLS + bind 127.0.0.1 if used |
| SEC-12 | SSE endpoints have AbortController disconnect cleanup |
| SEC-13 | Prompt injection guard on all LLM tool results |

---

## Backend / Clean Architecture Rules (BE-01..BE-13)

| ID | Rule |
|----|------|
| BE-01 | No `_children` / `_nodes` navigation properties on aggregate roots |
| BE-02 | Internal services (e.g. BvhTreeService) not exposed via API |
| BE-03 | Async recursive operations are repository-driven, not in-memory tree walk |
| BE-04 | FluentValidation validator exists for every Command |
| BE-05 | All list queries return `PagedList<T>` — never raw `List<T>` |
| BE-06 | Value Objects used for domain concepts (grid, dimension, bounding box) |
| BE-07 | TenantId on base entity class — not copy-pasted per entity |
| BE-08 | Domain events raised for every state mutation |
| BE-09 | EF Core `OwnsOne` used for Value Objects — not JSONB or manual mapping |
| BE-10 | Internal commands/queries not exposed in OpenAPI spec |
| BE-11 | `ConfigureAwait(false)` on every production async call |
| BE-12 | No `BuildServiceProvider()` anti-pattern in DI setup |
| BE-13 | Fire-and-forget tasks use `FireAndForget(logger, context)` — not `_ = task` |
