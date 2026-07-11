import { z } from 'zod';
import { Episode } from '../../episodic/types';

export const episodeHighlightOutputSchema = z.object({
    key_decisions: z.array(z.string().min(1)).max(5),
    lessons: z.array(z.string().min(1)).max(5),
    next_steps: z.array(z.string().min(1)).max(5),
    quality_score: z.number().min(0).max(1)
});

export type EpisodeHighlightOutput = z.infer<typeof episodeHighlightOutputSchema>;

export interface EpisodeHighlightPromptContract {
    version: 'epic-18.v1';
    prompt: string;
    output_schema: Record<string, unknown>;
}

export function buildEpisodeHighlightPrompt(episode: Episode): EpisodeHighlightPromptContract {
    return {
        version: 'epic-18.v1',
        prompt: [
            'Summarize the latest MCP session episode into normalized highlights.',
            'Return strict JSON only with these keys: key_decisions, lessons, next_steps, quality_score.',
            'Each list must contain short action-oriented strings.',
            `Domain: ${episode.domain}`,
            `Track: ${episode.track}`,
            `Phase: ${episode.phase}`,
            `Outcome summary: ${episode.outcomeSummary}`,
            `Tool calls: ${episode.toolCalls.map(toolCall => toolCall.tool).join(', ') || 'none'}`,
            `Artifacts: ${episode.artifacts.map(artifact => artifact.path).join(', ') || 'none'}`
        ].join('\n'),
        output_schema: {
            key_decisions: ['string'],
            lessons: ['string'],
            next_steps: ['string'],
            quality_score: 'number between 0 and 1'
        }
    };
}

export function parseEpisodeHighlightOutput(candidate: unknown): EpisodeHighlightOutput | null {
    let normalized: unknown = candidate;

    if (typeof candidate === 'string') {
        try {
            normalized = JSON.parse(candidate) as unknown;
        } catch {
            return null;
        }
    }

    const result = episodeHighlightOutputSchema.safeParse(normalized);
    return result.success ? result.data : null;
}

export function buildDeterministicEpisodeHighlight(episode: Episode): EpisodeHighlightOutput {
    const toolNames = episode.toolCalls.map(toolCall => toolCall.tool).filter(Boolean);
    const artifactPaths = episode.artifacts.map(artifact => artifact.path).filter(Boolean);
    const trimmedSummary = episode.outcomeSummary.trim();

    return {
        key_decisions: [
            trimmedSummary || `Advanced ${episode.domain} work during ${episode.phase}`,
            toolNames.length > 0 ? `Used tools: ${toolNames.slice(0, 3).join(', ')}` : `Stayed on ${episode.track} track`
        ],
        lessons: [
            trimmedSummary || 'Episode captured without explicit summary',
            artifactPaths.length > 0 ? `Artifacts produced: ${artifactPaths.slice(0, 2).join(', ')}` : 'No artifacts were attached to this episode'
        ],
        next_steps: [
            `Review follow-up work for ${episode.phase}`,
            artifactPaths.length > 0 ? 'Validate produced artifacts and share outcomes' : 'Capture concrete follow-up artifacts in the next episode'
        ],
        quality_score: trimmedSummary.length >= 80 ? 0.8 : 0.6
    };
}