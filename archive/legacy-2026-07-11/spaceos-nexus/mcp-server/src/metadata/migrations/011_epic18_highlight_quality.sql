-- Migration 011: EPIC-18 Highlight Quality Score Extensions
--
-- Adds columns to persist both raw and aggregated quality score components.
-- This enables reproducible quality scoring calculations and future migration.

-- Add AI confidence (raw) score if it doesn't already exist.
ALTER TABLE episode_highlights ADD COLUMN ai_quality_score REAL;

-- Add feedback-derived average score if it doesn't already exist.
ALTER TABLE episode_highlights ADD COLUMN feedback_quality_score REAL;

-- Track formula version; allows recalculating or migrating scores safely.
ALTER TABLE episode_highlights ADD COLUMN quality_score_version INTEGER DEFAULT 1;
