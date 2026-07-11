// src/interpreter/tool-registry.test.ts
import { describe, it, expect } from 'vitest';
import { DESIGN_PORTAL_TOOLS } from './tool-registry';

describe('DESIGN_PORTAL_TOOLS', () => {
  it('contains at least one tool per aggregate', () => {
    const names = DESIGN_PORTAL_TOOLS.map((t) => t.name);

    expect(names.some((n) => n.includes('tenant'))).toBe(true);
    expect(names.some((n) => n.includes('facility'))).toBe(true);
    expect(names.some((n) => n.includes('workstation'))).toBe(true);
    expect(names.some((n) => n.includes('spacelayer'))).toBe(true);
    expect(names.some((n) => n.includes('flowepic'))).toBe(true);
  });

  it('every tool has a non-empty description', () => {
    for (const tool of DESIGN_PORTAL_TOOLS) {
      expect(tool.description.trim().length, `Tool "${tool.name}" missing description`).toBeGreaterThan(0);
    }
  });

  it('every required field is listed in properties', () => {
    for (const tool of DESIGN_PORTAL_TOOLS) {
      const { required, properties } = tool.input_schema;
      for (const field of required) {
        expect(
          Object.keys(properties),
          `Tool "${tool.name}": required field "${field}" not in properties`,
        ).toContain(field);
      }
    }
  });

  it('tool names are unique', () => {
    const names = DESIGN_PORTAL_TOOLS.map((t) => t.name);
    const unique = new Set(names);
    expect(unique.size).toBe(names.length);
  });
});
