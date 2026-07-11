---
id: architect-signoff-template
title: "Architect Sign-off Template"
description: "Final accreditation document at Epic closure. Architect certifies that the implementation meets long-term strategic goals, architectural standards, and prior ADR decisions."
type: template
scope: global
last_updated: 2026-03-01
---

# Architect Sign-off: [[ EPIC_ID - NAME ]]

**Purpose:** Final accreditation at Epic closure. The Architect certifies that the implementation meets long-term strategic goals, architectural standards, and previously established architectural decisions (ADRs).

## **1. Input Validation**

* [ ] **Tech Lead Review**: `docs/{project}/epics/{EPIC}/epic_review.md` is accessible and validated.
* [ ] **QA Status**: All critical verifications passed.
* [ ] **Documentation**: All new features are documented in the system.

## **2. Architectural Assessment**

> **Use the Chain of Thought Pattern!**

### **Alignment with Standards**
*How does the implementation follow the established standards (e.g., Clean Architecture, DDD)?*
- [WRITTEN ASSESSMENT]

### **Technical Debt**
*Was any new technical debt introduced? If so, is there a plan to address it?*
- [WRITTEN ASSESSMENT]

### **ADR Compliance**
*Were all relevant ADRs followed?*
- [WRITTEN ASSESSMENT]

## **3. Strategic Lessons (for Product Owner)**

> **Use the Fact Summary Pattern!**

- Lesson 1: [DESCRIPTION]
- Lesson 2: [DESCRIPTION]

## **4. Calibration Recommendations (for Knowledge Steward)**

> **Use the Fact Summary Pattern!**

*Skills, workflows, patterns, or constraints to integrate into the knowledge base.*

- Recommendation 1: [DESCRIPTION — file, type, scope]
- Recommendation 2: [DESCRIPTION — file, type, scope]

## **5. Decision**

- [ ] **APPROVED** — Epic successfully closed.
- [ ] **CONDITIONALLY APPROVED** — Minor fixes required (see below).
- [ ] **REJECTED** — Serious structural flaws; redesign required.

**Rationale / Notes:**
[WRITTEN RATIONALE]

---

**Signature (Architect ID):** [[ ARCHITECT_ID ]]
**Date:** [[ TIMESTAMP ]]
