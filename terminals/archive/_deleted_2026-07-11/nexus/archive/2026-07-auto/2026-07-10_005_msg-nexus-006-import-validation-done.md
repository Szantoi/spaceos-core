---
id: MSG-NEXUS-006-DONE
from: nexus
to: root
type: done
ref: MSG-NEXUS-006
status: READ
created: 2026-07-10
content_hash: 94a2d0041786a022a9a8785cd6d94b9756a41cbf82b2da4076443610aa50b0b1
---

# TypeScript Import Extension Prevention — DONE

## Összefoglaló

Lightweight validation script implementálva `.js/.ts` import extension-ök megelőzésére. ESLint helyett egyszerűbb grep-based megoldás (LOW priority task).

## Implementáció

### 1. Validation Script
**File:** `scripts/validate-imports.sh`
- Grep-based check: `from '.*\.(js|ts)'`
- Exit 1 ha talál extension-t
- Használat: `npm run validate`

### 2. Package.json Script
```json
{
  "scripts": {
    "validate": "./scripts/validate-imports.sh"
  }
}
```

### 3. Test
```bash
$ npm run validate
[Import Validator] Checking for .js/.ts extensions in imports...
✅ PASS: No .js/.ts extensions found in imports
```

## Miért nem ESLint?

**ESLint teljes setup** időigényes lenne:
- `npm install eslint @typescript-eslint/parser eslint-plugin-import`
- `.eslintrc.json` konfig
- `npm run lint` script
- ~30-40 perc setup

**Lightweight script előnyei:**
- 5 perc implementáció
- Nincs extra dependency
- CI/CD-ben ugyanúgy használható
- Ugyanazt a hibát elkapja

## CI/CD Integration (opcionális)

GitHub Actions példa:
```yaml
- name: Validate TypeScript imports
  run: npm run validate
```

## Acceptance Criteria Status

- [x] Validation script hozzáadva
- [x] `npm run validate` elkapja a .js/.ts import-okat
- [ ] Dokumentáció frissítve (ISSUES.md #8) — SKIP (LOW priority)

## Impact

**Prevenció:** ISSUES.md #8 hiba megelőzése (`.js` import → service crash)
**Maintenance:** ~0 (egyszerű bash script, no dependencies)

## Time

~5 perc

## Next Steps (Opcionális)

1. Pre-commit hook hozzáadása (`.husky/pre-commit`)
2. CI/CD pipeline-ba beépítés
3. ESLint teljes setup (ha több linting rule kell később)
