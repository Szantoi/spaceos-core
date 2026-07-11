---
id: template-dqm-canvas
title: "Domain Quality Mapping (DQM) Canvas"
description: "Quality attribute mapping canvas (ISO 25010 aligned). Used by the Product Owner to define quality priorities, scope guards, and fitness functions for an Epic before planning begins."
type: template
scope: global
last_updated: 2026-03-01
---

# DQM Canvas: {EPIC_TITLE}

**Project**: {PROJECT_NAME}
**Epic**: {EPIC_ID}
**Date**: {DATE}

---

## 1. Business Quality Attributes (ISO 25010 Alignment)

Select at least 3 priorities from `domain_quality_mapping.knowledge.md`:

| Attribute | Priority (MoSCoW) | Business Rationale |
|:-----------|:-----------------|:------------------|
| **Functional Suitability** | Must Have | {Why is this critical for the business?} |
| **Performance Efficiency** | Should Have | {What response time is expected?} |
| **Compatibility** | Must Have | {What must it interoperate with?} |
| **Usability** | Could Have | {How important is UX right now?} |
| **Reliability** | Must Have | {How critical is failure?} |
| **Security** | Should Have | {Level of data protection required?} |
| **Maintainability** | Must Have | {Long-term sustainability?} |
| **Portability** | Won't Have | {Portability requirements?} |

## 2. Scope Guard (Won't Have)

What does this Epic explicitly **NOT** include at a technical level? (Prevent gold-plating)

- [ ] {Item 1}
- [ ] {Item 2}
- [ ] {Item 3}

## 3. Quality Gates & Fitness Functions

How do we measure quality automatically?

| Test Type | Fitness Function | Expected Value |
|:----------|:----------------|:--------------|
| **Unit Test** | Coverage | > {X}% |
| **Architecture Check** | NetArchTest rules | 0 violations |
| **Performance** | API Response Time | < {X}ms |
| **Static Analysis** | Lint errors | 0 errors |

## 4. Domain Consistency

- **Ubiquitous Language**: New terms to propagate: `{Term1}, {Term2}`
- **Context Boundaries**: Affected domains: `{Domain A}, {Domain B}`

---

## PO Sign-off
- [ ] Business value validated
- [ ] Scope guard defined
