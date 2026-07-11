# CLAUDE.md — src/config/

**Module:** Environment configuration
**Rule:** This is the ONLY place that reads `process.env`. Everywhere else uses `env.*`.

---

## What lives here

| File | Purpose |
|---|---|
| `env.ts` | Zod schema + `safeParse` — fail-fast on startup if any var is missing/invalid |

## Rules

- Every new env var → add to `envSchema` in `env.ts` AND to `.env.example` in the root
- Zod `default()` only for truly optional vars — required vars must be `z.string().min(1)`
- Export only the `env` const — never re-export `process.env`
- No business logic here — pure config extraction

## Pattern

```typescript
// env.ts
const envSchema = z.object({
  MY_NEW_VAR: z.string().min(1),               // required
  MY_OPTIONAL: z.string().default('fallback'),  // optional with default
});

const parsed = envSchema.safeParse(process.env);
if (!parsed.success) { console.error(...); process.exit(1); }
export const env = parsed.data;
```
