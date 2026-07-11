---
title: "Dev E — TASK-13-02 Assignment Sheet"
subtitle: "RBAC Discovery Filter & Track Routing — Middleware Integration"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P0"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ READY (after TASK-13-01)"
effort_estimate: "14 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-02 Assignment

**Task:** TASK-13-02 (RBAC Discovery Filter & Track Routing)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P0
**Effort Estimate:** 14 hours (~2 days)
**Dependency:** Requires TASK-13-01 (role definitions must exist)

---

## 🎯 Your Mission

Integrate discovery roles into the **middleware/RBAC system** so that discovery agents see only discovery-specific tools and workflows. This builds on EPIC-11's RbacFilter to add **track-based routing**.

**Key Deliverable:** Discovery agents get track="discovery" context, RBAC filters discovery tool access, middleware routes discovery-track requests correctly.

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: Track Property in Session Context ✅

- [ ] `agent_sessions` table has `track` column (discovery/delivery)
- [ ] `request_context()` call sets track based on role
- [ ] Track persists in session throughout workflow

### AC-2: Discovery Tool RBAC Filtering ✅

- [ ] Discovery agents calling delivery tools → UNAUTHORIZED error
- [ ] Delivery agents calling discovery tools → UNAUTHORIZED error
- [ ] Discovery/delivery agents can only call their own track's tools

### AC-3: E2E Discovery Routing ✅

- [ ] Bootstrap discovery agent → track="discovery"
- [ ] Call `request_context("ideation")` → Returns discovery ideation workflow
- [ ] Call `reference_prior_discovery()` → Returns discovery episodes only (not delivery)

---

## 🛠️ Implementation Checklist

- [ ] Modify `agent_sessions` schema: add `track TEXT` column
- [ ] Update RbacFilter to check track + role
- [ ] Integrate track into middleware context injection
- [ ] Write 8+ unit tests
- [ ] E2E test: discovery agent workflow

**Status:** BLOCKED by TASK-13-01 (waiting for role definitions)
