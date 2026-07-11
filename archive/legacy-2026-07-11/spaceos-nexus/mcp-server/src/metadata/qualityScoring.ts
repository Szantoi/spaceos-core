import { SessionEpisodeHighlightRow } from '../mcp/AgentDb';

export interface HighlightVectorMetadata {
  highlight_id: string;
  episode_id: string;
  session_id: string;
  domain: string;
  track: string;
  quality_score: number;
}

export interface QualityScoreComponents {
  /** Raw AI confidence score (0.0..1.0). */
  aiScore: number;
  /** Average score from manual feedback (0.0..1.0). */
  feedbackAvg: number;
  /** Number of feedback rows included in feedbackAvg. */
  feedbackCount: number;
  /** Combined score that should be stored on the highlight (0.0..1.0). */
  computedScore: number;
  /** Version of the quality score formula used. */
  version: number;
}

const QUALITY_SCORE_VERSION = 1;
const DEFAULT_AI_SCORE = 0.5;

/**
 * Computes a highlight quality score using AI confidence + manual feedback.
 *
 * Formula (v1):
 *   w = min(1, feedbackCount / (feedbackCount + 1))
 *   combined = aiScore * (1 - w) + feedbackAvg * w
 *
 * This gives diminishing returns for small feedback counts and prevents
 * single feedback rows from overwhelming the AI confidence.
 */
export function computeQualityScore(
  aiScore: number | null | undefined,
  feedbackScores: Array<number | null | undefined>
): QualityScoreComponents {
  const clampedAi = clamp01(aiScore ?? DEFAULT_AI_SCORE);

  const validFeedback = feedbackScores
    .filter((v): v is number => typeof v === 'number' && !Number.isNaN(v))
    .map(clamp01);

  const feedbackCount = validFeedback.length;
  const feedbackAvg =
    feedbackCount === 0 ? clampedAi : validFeedback.reduce((sum, v) => sum + v, 0) / feedbackCount;

  const weight = feedbackCount === 0 ? 0 : Math.min(1, feedbackCount / (feedbackCount + 1));
  const combined = clampedAi * (1 - weight) + feedbackAvg * weight;

  return {
    aiScore: clampedAi,
    feedbackAvg,
    feedbackCount,
    computedScore: clamp01(roundTo(combined, 4)),
    version: QUALITY_SCORE_VERSION,
  };
}

function clamp01(value: number): number {
  if (Number.isNaN(value)) return 0;
  return Math.min(1, Math.max(0, value));
}

function roundTo(value: number, digits: number): number {
  const factor = 10 ** digits;
  return Math.round(value * factor) / factor;
}

export function buildHighlightEmbeddingContent(row: SessionEpisodeHighlightRow): string {
  const sections = [
    formatSection('Key decisions', parseJsonArray(row.key_decisions)),
    formatSection('Lessons', parseJsonArray(row.lessons)),
    formatSection('Next steps', parseJsonArray(row.next_steps)),
  ].filter((section) => section.length > 0);

  return sections.join('\n\n');
}

export function buildHighlightVectorMetadata(row: SessionEpisodeHighlightRow): HighlightVectorMetadata {
  return {
    highlight_id: row.id,
    episode_id: row.episode_id,
    session_id: row.session_id,
    domain: row.domain,
    track: row.track,
    quality_score: row.quality_score ?? 0,
  };
}

function parseJsonArray(candidate: string | null): string[] {
  if (!candidate) {
    return [];
  }

  try {
    const parsed = JSON.parse(candidate) as unknown;
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed.filter((value): value is string => typeof value === 'string' && value.trim().length > 0);
  } catch {
    return [];
  }
}

function formatSection(title: string, values: string[]): string {
  if (values.length === 0) {
    return '';
  }

  return `${title}: ${values.join(' | ')}`;
}