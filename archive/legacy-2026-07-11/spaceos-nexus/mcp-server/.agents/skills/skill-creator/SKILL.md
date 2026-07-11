---
name: skill-creator
description: Guide for creating effective skills. This skill should be used when users want to create a new skill (or update an existing skill) that extends Claude's capabilities with specialized knowledge, workflows, or tool integrations.
---

# Skill Creator

This skill provides guidance for creating effective skills.

## Core Principles
1. **Concise is Key**: Claude is already smart. Only add context it doesn't have.
2. **Progressive Disclosure**: Use metadata (triggers), SKILL.md (essentials), and boxed resources (details/scripts).
3. **Anatomy**: `SKILL.md` (metadata + body), `scripts/`, `references/`, `assets/`.

## Skill Creation Process
1. Understand with concrete examples.
2. Plan reusable content (scripts, references, assets).
3. Initialize (`init_skill.py`).
4. Edit (procedural knowledge, examples).
5. Package (`package_skill.py`).

## Writing Guidelines
- Use imperative/infinitive form.
- Keep `SKILL.md` body under 500 lines.
- Move details to `references/`.
