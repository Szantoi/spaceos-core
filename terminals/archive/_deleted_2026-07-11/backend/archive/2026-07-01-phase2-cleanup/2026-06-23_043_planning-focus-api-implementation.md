---
id: MSG-BACKEND-043
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
created: 2026-06-23
processed: 2026-06-23
content_hash: 2276a40a3527809c265a5bdc1c92a6fbd7c61424ade4fa64b23e3b1f5054d697
---

# Planning Focus Area API — Implementation

## Context

**Architecture source:** `/opt/spaceos/docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`
**Current state:** Architect prepared full design specification (MSG-ARCHITECT-012-DONE)
**Business value:** Enable Focus Area Panel in Datahaven UI for planning domain management

## Objective

Implement the **Planning Focus API** endpoints that enable the Datahaven UI Focus Area Panel to:
1. Read the current planning domain and criteria from `docs/planning/domain-focus.md`
2. Update the domain selection (7 predefined domains)
3. Update the criteria list (markdown format)
4. Validate all inputs and handle edge cases

## User Story

**As a** Conductor terminal (or Root)
**I want to** change the active planning domain via the Datahaven UI
**So that** the planning pipeline (Haiku scanner) focuses on the relevant domain

**Acceptance Criteria:**
- ✅ GET endpoint returns current domain + criteria
- ✅ PUT endpoint updates domain-focus.md file atomically
- ✅ Domain validation (must be one of 7 valid options)
- ✅ Criteria markdown sanitization (XSS protection)
- ✅ Rate limiting (max 10 writes/minute)
- ✅ Atomic file writes (no partial updates)
- ✅ API returns last updated timestamp

## Technical Approach

### File Structure

**Location:** `/opt/spaceos/docs/planning/domain-focus.md`

**Format:**
```markdown
---
domain: manufacturing
updated_at: 2026-06-23T12:34:56Z
---

# Planning Focus

**Active Domain:** manufacturing

## Criteria

- **Felhasználói érték**: Milyen funkció segíti...
- **Backend kapcsolhatóság**: Van-e már meglévő...
- **Iparági minták**: Mi az ami más ERP/MES...
- **Mobil első**: A funkciónak működnie kell...
- **Offline tűrés**: Ha az internet kimegy...
```

### Implementation Spec

**File:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)

**Dependencies:**
```typescript
import { Router } from 'express';
import { promises as fs } from 'node:fs';
import * as path from 'node:path';
import matter from 'gray-matter';
```

**Endpoints:**

#### 1. GET /api/planning/domain-focus

**Purpose:** Read current domain and criteria

**Implementation:**
```typescript
router.get('/domain-focus', async (req, res) => {
  const filePath = path.join(process.cwd(), '../docs/planning/domain-focus.md');

  try {
    const content = await fs.readFile(filePath, 'utf-8');
    const { data, content: criteria } = matter(content);

    res.json({
      domain: data.domain || 'manufacturing',
      criteria: criteria.trim(),
      updated_at: data.updated_at || new Date().toISOString()
    });
  } catch (err) {
    if (err.code === 'ENOENT') {
      // File doesn't exist - return defaults
      res.json({
        domain: 'manufacturing',
        criteria: '# Planning Focus\n\n(No criteria defined)',
        updated_at: new Date().toISOString()
      });
    } else {
      res.status(500).json({ error: 'Failed to read focus file' });
    }
  }
});
```

#### 2. PUT /api/planning/domain-focus

**Purpose:** Update domain and/or criteria

**Request Body:**
```typescript
interface UpdateFocusRequest {
  domain?: string;      // Optional - one of 7 valid domains
  criteria?: string;    // Optional - markdown string
}
```

**Validation Rules:**
```typescript
const VALID_DOMAINS = [
  'manufacturing',
  'sales',
  'logistics',
  'finance',
  'quality',
  'hr',
  'all'
];

function validateDomain(domain: string): boolean {
  return VALID_DOMAINS.includes(domain);
}

function sanitizeCriteria(criteria: string): string {
  // Basic XSS protection - strip script tags and event handlers
  return criteria
    .replace(/<script[^>]*>.*?<\/script>/gi, '')
    .replace(/on\w+="[^"]*"/gi, '')
    .replace(/on\w+='[^']*'/gi, '')
    .trim();
}
```

**Implementation:**
```typescript
// Rate limiting state (simple in-memory - production should use Redis)
const rateLimitMap = new Map<string, number[]>();

function checkRateLimit(ip: string): boolean {
  const now = Date.now();
  const windowMs = 60 * 1000; // 1 minute
  const maxRequests = 10;

  const timestamps = rateLimitMap.get(ip) || [];
  const recentTimestamps = timestamps.filter(t => now - t < windowMs);

  if (recentTimestamps.length >= maxRequests) {
    return false; // Rate limit exceeded
  }

  recentTimestamps.push(now);
  rateLimitMap.set(ip, recentTimestamps);
  return true;
}

router.put('/domain-focus', async (req, res) => {
  const clientIp = req.ip || 'unknown';

  // Rate limiting
  if (!checkRateLimit(clientIp)) {
    return res.status(429).json({
      error: 'Rate limit exceeded',
      message: 'Max 10 updates per minute'
    });
  }

  const { domain, criteria } = req.body;

  // Validation
  if (domain && !validateDomain(domain)) {
    return res.status(400).json({
      error: 'Invalid domain',
      message: `Domain must be one of: ${VALID_DOMAINS.join(', ')}`
    });
  }

  const filePath = path.join(process.cwd(), '../docs/planning/domain-focus.md');

  try {
    // Read current file
    let currentContent = '';
    let currentData: any = {};

    try {
      currentContent = await fs.readFile(filePath, 'utf-8');
      const parsed = matter(currentContent);
      currentData = parsed.data;
      currentContent = parsed.content;
    } catch (err) {
      // File doesn't exist - start with defaults
      currentData = { domain: 'manufacturing' };
      currentContent = '# Planning Focus\n\n(No criteria defined)';
    }

    // Update fields
    if (domain) {
      currentData.domain = domain;
    }

    let newCriteria = currentContent;
    if (criteria !== undefined) {
      newCriteria = sanitizeCriteria(criteria);
    }

    currentData.updated_at = new Date().toISOString();

    // Write atomically (write to temp file, then rename)
    const newContent = matter.stringify(newCriteria, currentData);
    const tempPath = filePath + '.tmp';

    await fs.writeFile(tempPath, newContent, 'utf-8');
    await fs.rename(tempPath, filePath);

    res.json({
      success: true,
      domain: currentData.domain,
      criteria: newCriteria,
      updated_at: currentData.updated_at
    });
  } catch (err) {
    console.error('Failed to update focus file:', err);
    res.status(500).json({ error: 'Failed to update focus file' });
  }
});
```

#### 3. Integration to server.ts

**File:** `spaceos-nexus/knowledge-service/src/server.ts`

```typescript
import { createPlanningRouter } from './api/planningRoutes';

// Add to routes
app.use('/api/planning', createPlanningRouter());
```

### Testing Strategy

**Unit tests:** `src/__tests__/unit/planningRoutes.test.ts`

```typescript
describe('Planning Focus API', () => {
  describe('GET /api/planning/domain-focus', () => {
    it('returns current domain and criteria', async () => {
      const res = await request(app).get('/api/planning/domain-focus');
      expect(res.status).toBe(200);
      expect(res.body).toHaveProperty('domain');
      expect(res.body).toHaveProperty('criteria');
      expect(res.body).toHaveProperty('updated_at');
    });

    it('returns defaults if file does not exist', async () => {
      // Test with non-existent file
      const res = await request(app).get('/api/planning/domain-focus');
      expect(res.body.domain).toBe('manufacturing');
    });
  });

  describe('PUT /api/planning/domain-focus', () => {
    it('updates domain successfully', async () => {
      const res = await request(app)
        .put('/api/planning/domain-focus')
        .send({ domain: 'sales' });

      expect(res.status).toBe(200);
      expect(res.body.success).toBe(true);
      expect(res.body.domain).toBe('sales');
    });

    it('rejects invalid domain', async () => {
      const res = await request(app)
        .put('/api/planning/domain-focus')
        .send({ domain: 'invalid_domain' });

      expect(res.status).toBe(400);
      expect(res.body.error).toContain('Invalid domain');
    });

    it('sanitizes XSS in criteria', async () => {
      const malicious = '<script>alert("xss")</script>Valid criteria';
      const res = await request(app)
        .put('/api/planning/domain-focus')
        .send({ criteria: malicious });

      expect(res.status).toBe(200);
      expect(res.body.criteria).not.toContain('<script>');
    });

    it('enforces rate limiting', async () => {
      // Make 11 requests rapidly
      const promises = Array.from({ length: 11 }, () =>
        request(app).put('/api/planning/domain-focus').send({ domain: 'sales' })
      );

      const results = await Promise.all(promises);
      const rateLimited = results.filter(r => r.status === 429);

      expect(rateLimited.length).toBeGreaterThan(0);
    });
  });
});
```

**Integration test:** Manual test via curl

```bash
# GET current focus
curl -s http://localhost:3456/api/planning/domain-focus | jq .

# PUT update domain
curl -X PUT http://localhost:3456/api/planning/domain-focus \
  -H "Content-Type: application/json" \
  -d '{"domain":"sales"}' | jq .

# PUT update criteria
curl -X PUT http://localhost:3456/api/planning/domain-focus \
  -H "Content-Type: application/json" \
  -d '{"criteria":"# New criteria\n- Point 1\n- Point 2"}' | jq .
```

## Definition of Done

- [ ] `planningRoutes.ts` file created in `spaceos-nexus/knowledge-service/src/api/`
- [ ] GET `/api/planning/domain-focus` endpoint implemented
- [ ] PUT `/api/planning/domain-focus` endpoint implemented
- [ ] Domain validation (7 valid options)
- [ ] Criteria sanitization (XSS protection)
- [ ] Rate limiting (10 writes/min)
- [ ] Atomic file writes (temp file + rename)
- [ ] Error handling (file not found, invalid input, rate limit)
- [ ] Unit tests (5+ test cases)
- [ ] Integration tests (manual curl commands)
- [ ] Router integrated to `server.ts`
- [ ] No TypeScript errors
- [ ] API tested and working

## Timeline

**Estimated:** 3-4 hours

**Breakdown:**
- planningRoutes.ts implementation: 1.5 hours
- Unit tests: 1 hour
- Integration to server.ts: 0.5 hour
- Manual testing + bug fixes: 1 hour

## Security Considerations

1. **XSS Protection** — Sanitize criteria markdown (strip <script> tags, event handlers)
2. **Rate Limiting** — Max 10 writes/minute per IP
3. **Input Validation** — Domain must be in VALID_DOMAINS list
4. **Atomic Writes** — Use temp file + rename to prevent partial updates
5. **Path Traversal** — Use path.join, never accept file path from client

## Dependencies

**Existing:**
- `gray-matter` — Already in package.json (YAML frontmatter parsing)
- `express` — Already in package.json

**No new dependencies needed.**

## Notes

- This API does **not** automatically trigger the planning pipeline — it only updates the focus file
- The Haiku scanner (plan-scan.sh) will read the updated file on the next cycle
- The domain-focus.md file may not exist initially — API creates it with defaults
- Rate limiting is in-memory for now — production should use Redis or similar

## Questions?

If the domain-focus.md file structure differs from the spec above, check the current file format and adapt accordingly.

---

**Conductor**
2026-06-23
Planning Focus API Implementation Task
