import { ChromaClient } from 'chromadb';
import { GoogleGenerativeAIEmbeddings } from '@langchain/google-genai';
import { AgentDb, SessionEpisodeHighlightRow } from '../mcp/AgentDb';
import { buildHighlightEmbeddingContent, buildHighlightVectorMetadata, type HighlightVectorMetadata } from '../metadata/qualityScoring';

export interface ReflectedHighlight {
  highlight_id: string;
  episode_id: string;
  session_id: string;
  key_decisions: string[];
  lessons: string[];
  next_steps: string[];
  quality_score: number | null;
  ai_generated: boolean;
  ai_model: string | null;
  ai_tokens_used: number | null;
  created_at: string;
  outcome_summary: string | null;
  retrieval_reason: 'current-session' | 'keyword-match' | 'recent-fallback';
  matched_terms: string[];
}

export function getCurrentSessionHighlights(
  agentDb: AgentDb,
  sessionId: string
): ReflectedHighlight[] {
  return agentDb
    .listEpisodeHighlightsBySession(sessionId)
    .map((row) => mapHighlightRow(row, 'current-session', []));
}

export function retrievePriorHighlights(
  agentDb: AgentDb,
  sessionId: string,
  currentHighlights: ReflectedHighlight[],
  limit: number
): ReflectedHighlight[] {
  const keywords = buildHighlightKeywords(currentHighlights);
  const seen = new Set<string>();
  const results: ReflectedHighlight[] = [];

  const keywordMatches = agentDb.searchEpisodeHighlightsByKeywords({
    excludeSessionId: sessionId,
    keywords,
    limit,
  });

  for (const row of keywordMatches) {
    if (seen.has(row.id)) {
      continue;
    }

    const matchedTerms = matchTerms(row, keywords);
    results.push(mapHighlightRow(row, 'keyword-match', matchedTerms));
    seen.add(row.id);
  }

  if (results.length < limit) {
    const fallbackRows = agentDb.searchEpisodeHighlightsByKeywords({
      excludeSessionId: sessionId,
      keywords: [],
      limit: limit * 2,
    });

    for (const row of fallbackRows) {
      if (seen.has(row.id)) {
        continue;
      }
      results.push(mapHighlightRow(row, 'recent-fallback', []));
      seen.add(row.id);
      if (results.length >= limit) {
        break;
      }
    }
  }

  return results.slice(0, limit);
}

function mapHighlightRow(
  row: SessionEpisodeHighlightRow,
  retrievalReason: ReflectedHighlight['retrieval_reason'],
  matchedTerms: string[]
): ReflectedHighlight {
  return {
    highlight_id: row.id,
    episode_id: row.episode_id,
    session_id: row.session_id,
    key_decisions: parseStringArray(row.key_decisions),
    lessons: parseStringArray(row.lessons),
    next_steps: parseStringArray(row.next_steps),
    quality_score: row.quality_score,
    ai_generated: row.ai_generated === 1,
    ai_model: row.ai_model,
    ai_tokens_used: row.ai_tokens_used,
    created_at: row.created_at,
    outcome_summary: row.outcome_summary,
    retrieval_reason: retrievalReason,
    matched_terms: matchedTerms,
  };
}

function buildHighlightKeywords(highlights: ReflectedHighlight[]): string[] {
  const keywords = new Set<string>();
  for (const highlight of highlights) {
    for (const text of [
      ...highlight.key_decisions,
      ...highlight.lessons,
      ...highlight.next_steps,
      highlight.outcome_summary ?? '',
    ]) {
      for (const token of tokenize(text)) {
        keywords.add(token);
      }
    }
  }
  return Array.from(keywords).slice(0, 12);
}

function matchTerms(row: SessionEpisodeHighlightRow, keywords: string[]): string[] {
  const haystack = [row.key_decisions, row.lessons, row.next_steps, row.outcome_summary]
    .filter((value): value is string => typeof value === 'string')
    .join(' ')
    .toLowerCase();

  return keywords.filter((keyword) => haystack.includes(keyword)).slice(0, 5);
}

function parseStringArray(candidate: string | null): string[] {
  if (!candidate) {
    return [];
  }

  try {
    const parsed = JSON.parse(candidate) as unknown;
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed.filter((value): value is string => typeof value === 'string');
  } catch {
    return [];
  }
}

function tokenize(text: string): string[] {
  return text
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((token) => token.length >= 5)
    .filter((token) => !STOP_WORDS.has(token));
}

const STOP_WORDS = new Set([
  'about',
  'after',
  'before',
  'could',
  'should',
  'their',
  'there',
  'these',
  'those',
  'through',
  'using',
  'where',
  'which',
  'while',
]);

export interface HighlightEmbeddingProvider {
  embedDocuments(texts: string[]): Promise<number[][]>;
  embedQuery(text: string): Promise<number[]>;
}

export interface HighlightCollection {
  upsert(params: {
    ids: string[];
    documents?: string[];
    embeddings?: number[][];
    metadatas?: any[];
    uris?: string[];
  }): Promise<void>;
  query(params: any): Promise<any>;
}

export interface HighlightsChromaClientLike {
  heartbeat(): Promise<unknown>;
  getOrCreateCollection(params: {
    name: string;
    metadata?: Record<string, unknown>;
  }): Promise<any>;
}

export interface HighlightSearchResult extends ReflectedHighlight {
  similarity_score: number;
}

export interface SyncHighlightsResult {
  collection_name: string;
  indexed_count: number;
  warnings: string[];
}

interface IndexedHighlightDocument {
  highlightId: string;
  vectorId: string;
  embedding: number[];
  metadata: HighlightVectorMetadata;
}

export class EpisodeHighlightsIndex {
  private static readonly collectionName = 'episode_highlights';

  private readonly client: HighlightsChromaClientLike;
  private readonly embeddings: HighlightEmbeddingProvider;
  private readonly embeddingModel: string;
  private collection: HighlightCollection | null = null;
  private initialized = false;
  private degraded = false;
  private warnings = new Set<string>();
  private readonly memoryIndex = new Map<string, IndexedHighlightDocument>();

  constructor(
    private readonly agentDb: AgentDb,
    options?: {
      client?: HighlightsChromaClientLike;
      embeddings?: HighlightEmbeddingProvider;
      embeddingModel?: string;
    }
  ) {
    this.client = options?.client ?? new ChromaClient();
    this.embeddings = options?.embeddings ?? new GoogleGenerativeAIEmbeddings({
      apiKey: process.env.GOOGLE_API_KEY,
      modelName: 'gemini-embedding-001',
    });
    this.embeddingModel = options?.embeddingModel ?? 'gemini-embedding-001';
  }

  async syncHighlights(params?: {
    highlightIds?: string[];
    limit?: number;
  }): Promise<SyncHighlightsResult> {
    await this.init();

    const rows = this.loadHighlights(params);
    let indexedCount = 0;

    for (const row of rows) {
      const content = buildHighlightEmbeddingContent(row);
      if (content.trim().length === 0) {
        continue;
      }

      const [embedding] = await this.embeddings.embedDocuments([content]);
      const vectorId = this.getVectorId(row.id);
      const metadata = buildHighlightVectorMetadata(row);

      if (!this.degraded && this.collection) {
        await this.collection.upsert({
          ids: [vectorId],
          documents: [content],
          embeddings: [embedding],
          metadatas: [metadata],
        });
      } else {
        this.memoryIndex.set(vectorId, {
          highlightId: row.id,
          vectorId,
          embedding,
          metadata,
        });
      }

      this.agentDb.upsertHighlightChromaSync({
        highlight_id: row.id,
        vector_id: vectorId,
        embedding_model: this.degraded ? `memory-fallback:${this.embeddingModel}` : this.embeddingModel,
      });
      indexedCount += 1;
    }

    return {
      collection_name: EpisodeHighlightsIndex.collectionName,
      indexed_count: indexedCount,
      warnings: Array.from(this.warnings),
    };
  }

  async searchHighlights(query: string, topK: number): Promise<HighlightSearchResult[]> {
    await this.init();
    const queryEmbedding = await this.embeddings.embedQuery(query);

    if (!this.degraded && this.collection) {
      const result = await this.collection.query({
        queryEmbeddings: [queryEmbedding],
        nResults: topK,
      });

      const ids = result.ids[0] ?? [];
      const distances = result.distances?.[0] ?? [];
      return ids
        .map((vectorId, index) => this.mapSearchHit(vectorId, 1 - (distances[index] ?? 1)))
        .filter((row): row is HighlightSearchResult => row !== null);
    }

    return Array.from(this.memoryIndex.values())
      .map((entry) => ({
        entry,
        score: cosineSimilarity(queryEmbedding, entry.embedding),
      }))
      .sort((left, right) => right.score - left.score)
      .slice(0, topK)
      .map(({ entry, score }) => this.mapSearchHit(entry.vectorId, score))
      .filter((row): row is HighlightSearchResult => row !== null);
  }

  getWarnings(): string[] {
    return Array.from(this.warnings);
  }

  private async init(): Promise<void> {
    if (this.initialized) {
      return;
    }

    try {
      await this.client.heartbeat();
      this.collection = await this.client.getOrCreateCollection({
        name: EpisodeHighlightsIndex.collectionName,
        metadata: { description: 'Semantic index for episodic highlights' },
      });
      this.degraded = false;
    } catch {
      this.degraded = true;
      this.warnings.add('CHROMADB_UNAVAILABLE');
    }

    this.initialized = true;
  }

  private loadHighlights(params?: {
    highlightIds?: string[];
    limit?: number;
  }): SessionEpisodeHighlightRow[] {
    if (params?.highlightIds && params.highlightIds.length > 0) {
      return params.highlightIds
        .map((highlightId) => this.agentDb.getEpisodeHighlightWithContextById(highlightId))
        .filter((row): row is SessionEpisodeHighlightRow => row !== null);
    }

    return this.agentDb.listEpisodeHighlights(params?.limit);
  }

  private mapSearchHit(vectorId: string, similarityScore: number): HighlightSearchResult | null {
    const highlightId = vectorId.replace(/^highlight:/, '');
    const row = this.agentDb.getEpisodeHighlightWithContextById(highlightId);
    if (!row) {
      return null;
    }

    return {
      ...mapHighlightRow(row, 'recent-fallback', []),
      similarity_score: similarityScore,
    };
  }

  private getVectorId(highlightId: string): string {
    return `highlight:${highlightId}`;
  }
}

const highlightIndexRegistry = new WeakMap<AgentDb, EpisodeHighlightsIndex>();

export function getEpisodeHighlightsIndex(agentDb: AgentDb): EpisodeHighlightsIndex {
  const existing = highlightIndexRegistry.get(agentDb);
  if (existing) {
    return existing;
  }

  const created = new EpisodeHighlightsIndex(agentDb);
  highlightIndexRegistry.set(agentDb, created);
  return created;
}

export async function syncHighlightsCollection(
  agentDb: AgentDb,
  params?: { highlightIds?: string[]; limit?: number }
): Promise<SyncHighlightsResult> {
  return getEpisodeHighlightsIndex(agentDb).syncHighlights(params);
}

export async function searchHighlights(
  agentDb: AgentDb,
  query: string,
  topK: number
): Promise<HighlightSearchResult[]> {
  return getEpisodeHighlightsIndex(agentDb).searchHighlights(query, topK);
}

function cosineSimilarity(left: number[], right: number[]): number {
  const maxLength = Math.max(left.length, right.length);
  let dotProduct = 0;
  let leftMagnitude = 0;
  let rightMagnitude = 0;

  for (let index = 0; index < maxLength; index += 1) {
    const leftValue = left[index] ?? 0;
    const rightValue = right[index] ?? 0;
    dotProduct += leftValue * rightValue;
    leftMagnitude += leftValue * leftValue;
    rightMagnitude += rightValue * rightValue;
  }

  if (leftMagnitude === 0 || rightMagnitude === 0) {
    return 0;
  }

  return dotProduct / (Math.sqrt(leftMagnitude) * Math.sqrt(rightMagnitude));
}