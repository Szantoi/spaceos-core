-- Migration: 004-dwi-schema.sql
-- Purpose: Create Discovery Work Item (DWI) schema for tracking Discovery track progress in DB
-- Standards: database/standards/01-discovery/discovery.work-item.standard.md
-- Idempotent: Yes (uses IF NOT EXISTS)

-- Table 1: discovery_work_items
-- Stores the state file data from dwi-state.md
CREATE TABLE IF NOT EXISTS discovery_work_items (
  id TEXT PRIMARY KEY,
  -- Format: dwi-<topic-slug>, e.g., dwi-ssot-memory

  topic TEXT NOT NULL,
  -- Human-readable topic name (matches folder name in docs/joinerytech-flow/discovery/<topic>/)

  status TEXT NOT NULL CHECK (status IN ('open', 'in_progress', 'concluded', 'archived')),
  -- State machine: open → in_progress → concluded | archived

  current_phase INTEGER NOT NULL CHECK (current_phase >= 0 AND current_phase <= 4),
  -- Double Diamond phase: 0=Discover, 1=Define, 2=Ideate, 3=Prototype, 4=Learn

  next_action TEXT NOT NULL,
  -- The single most important next step. Must include role (Explorer, Framer, etc.) + artifact.
  -- Max length: 500 chars

  verdict TEXT CHECK (verdict IN ('validated', 'invalidated', 'pivoted', NULL)),
  -- Set at Phase 4 close. NULL until verdict decided.

  hypothesis_count INTEGER NOT NULL DEFAULT 0,
  -- Total number of linked hyp-*.md files

  validated_count INTEGER NOT NULL DEFAULT 0,
  -- Count of hypotheses with status='validated'

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  created_by TEXT,
  -- Agent or user who created the DWI (e.g., "explorer", "orchestrator")

  updated_by TEXT
  -- Agent or user who last updated the DWI
);

-- Table 2: dwi_phase_gates
-- Stores the phase gate history from DWI state file (Section 4, "Phase Gate History" table)
CREATE TABLE IF NOT EXISTS dwi_phase_gates (
  id INTEGER PRIMARY KEY AUTOINCREMENT,

  dwi_id TEXT NOT NULL REFERENCES discovery_work_items(id) ON DELETE CASCADE,
  -- Foreign key to parent DWI

  phase INTEGER NOT NULL CHECK (phase >= 0 AND phase <= 4),
  -- Phase number: 0=Discover, 1=Define, 2=Ideate, 3=Prototype, 4=Learn

  gate_crossed BOOLEAN NOT NULL DEFAULT 0,
  -- Whether the phase gate criterion was met (1 = crossed, 0 = not yet)

  gate_crossed_date DATETIME,
  -- Date when gate was crossed. NULL if gate_crossed=0.

  notes TEXT,
  -- Optional notes about the gate (e.g., "Initial observations logged")

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Table 3: dwi_hypotheses
-- Stores the extended frontmatter data from hyp-*.md files
CREATE TABLE IF NOT EXISTS dwi_hypotheses (
  id TEXT PRIMARY KEY,
  -- Format: hyp-NNN, e.g., hyp-001

  dwi_id TEXT NOT NULL REFERENCES discovery_work_items(id) ON DELETE CASCADE,
  -- Foreign key to parent DWI

  statement TEXT NOT NULL,
  -- Hypothesis statement: "If [action] then [measurable result] because [mechanism]"
  -- Max length: 1000 chars

  status TEXT NOT NULL CHECK (status IN ('open', 'testing', 'validated', 'invalidated')),
  -- Hypothesis lifecycle: open → testing → validated | invalidated

  phase INTEGER NOT NULL CHECK (phase >= 1 AND phase <= 4),
  -- Phase where this hypothesis was created (1–4; 0 is observation, not hypothesis)

  artifact_path TEXT,
  -- Relative path to the hyp-*.md file in docs/joinerytech-flow/discovery/<topic>/
  -- Example: discovery/ssot-memory/01_define/hyp-001.md

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  closed_at DATETIME,
  -- Date when hypothesis reached terminal state (validated | invalidated). NULL if open/testing.

  created_by TEXT,
  updated_by TEXT
);

-- Indexes for performance (discovery dashboard queries)

CREATE INDEX IF NOT EXISTS idx_discovery_work_items_status_phase
ON discovery_work_items(status, current_phase);
-- Dashboard query: list DWIs by status and phase

CREATE INDEX IF NOT EXISTS idx_discovery_work_items_topic
ON discovery_work_items(topic);
-- Dashboard query: find DWI by topic name

CREATE INDEX IF NOT EXISTS idx_dwi_phase_gates_dwi_id
ON dwi_phase_gates(dwi_id, phase);
-- Phase history queries: list all gates for a DWI, lookup specific phase

CREATE UNIQUE INDEX IF NOT EXISTS idx_dwi_phase_gates_dwi_id_phase_unique
ON dwi_phase_gates(dwi_id, phase);
-- Ensure idempotent upsert based on DWI + phase

CREATE INDEX IF NOT EXISTS idx_dwi_hypotheses_dwi_id_status
ON dwi_hypotheses(dwi_id, status);
-- Hypothesis queries: count validated/invalidated per DWI

CREATE INDEX IF NOT EXISTS idx_dwi_hypotheses_status
ON dwi_hypotheses(status);
-- Global dashboard query: count hypotheses by status across all DWIs
