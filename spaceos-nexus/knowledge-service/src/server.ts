/**
 * SpaceOS Knowledge Service — Express server
 *
 * Endpoints:
 *   GET  /health
 *   GET  /api/knowledge/search?q=...&topK=5
 *   POST /api/knowledge/search   { q: string, topK?: number }
 *   POST /api/knowledge/index    (re-index docs/knowledge/)
 */

import 'dotenv/config';
import express, { Request, Response, NextFunction } from 'express';
import {
  initVectorStore,
  searchKnowledge,
  getDocumentCount,
  usingChroma,
} from './vectorStore';
import { embeddingBackend } from './embeddings';
import { buildIndex } from './indexer';

const app = express();
const PORT = parseInt(process.env.PORT || '3456', 10);

app.use(express.json());

// ─── Health ──────────────────────────────────────────────────────────────────

app.get('/health', async (_req: Request, res: Response) => {
  const count = await getDocumentCount();
  res.json({
    status: 'ok',
    vectorBackend: usingChroma() ? 'chromadb' : 'memory',
    embeddingBackend: embeddingBackend(),
    documents: count,
    knowledgePath: process.env.KNOWLEDGE_BASE_PATH || '(default)',
    port: PORT,
  });
});

// ─── Search (GET) ─────────────────────────────────────────────────────────────

app.get('/api/knowledge/search', async (req: Request, res: Response) => {
  const q = typeof req.query.q === 'string' ? req.query.q : '';
  const topK = parseInt(String(req.query.topK || '5'), 10);

  if (!q) {
    res.status(400).json({ error: 'q query param required' });
    return;
  }

  try {
    const results = await searchKnowledge(q, topK);
    res.json({ query: q, topK, count: results.length, results });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Search (POST) ────────────────────────────────────────────────────────────

app.post('/api/knowledge/search', async (req: Request, res: Response) => {
  const { q, topK = 5 } = req.body as { q?: string; topK?: number };

  if (!q) {
    res.status(400).json({ error: 'q field required in request body' });
    return;
  }

  try {
    const results = await searchKnowledge(q, topK);
    res.json({ query: q, topK, count: results.length, results });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Re-index ─────────────────────────────────────────────────────────────────

app.post('/api/knowledge/index', async (_req: Request, res: Response) => {
  try {
    const result = await buildIndex();
    const count = await getDocumentCount();
    res.json({ success: true, totalInStore: count, ...result });
  } catch (err: unknown) {
    const msg = err instanceof Error ? err.message : String(err);
    res.status(500).json({ error: msg });
  }
});

// ─── Error handler ────────────────────────────────────────────────────────────

// eslint-disable-next-line @typescript-eslint/no-unused-vars
app.use((err: Error, _req: Request, res: Response, _next: NextFunction) => {
  console.error('[ERROR]', err.message);
  res.status(500).json({ error: err.message });
});

// ─── Startup ──────────────────────────────────────────────────────────────────

async function main() {
  await initVectorStore();

  const count = await getDocumentCount();
  if (count === 0) {
    console.log('📚 Store empty — running initial knowledge base indexing...');
    await buildIndex();
  } else {
    console.log(
      `📚 Store has ${count} documents. POST /api/knowledge/index to re-index.`
    );
  }

  app.listen(PORT, () => {
    console.log(`\n🚀 SpaceOS Knowledge Service on port ${PORT}`);
    console.log(`   GET  /health`);
    console.log(`   GET  /api/knowledge/search?q=...&topK=5`);
    console.log(`   POST /api/knowledge/search   { q, topK? }`);
    console.log(`   POST /api/knowledge/index    (re-index)\n`);
  });
}

main().catch(err => {
  console.error('Fatal startup error:', err);
  process.exit(1);
});
