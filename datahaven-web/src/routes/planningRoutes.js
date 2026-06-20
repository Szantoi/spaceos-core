/**
 * Planning Routes — REST API for planning pipeline
 */

import { Router } from 'express';
import planningService from '../services/planningService.js';

const router = Router();

/**
 * GET /api/planning/ideas
 * List all ideas in the pipeline
 */
router.get('/ideas', async (req, res) => {
    try {
        const ideas = await planningService.getIdeas();
        res.json({
            stage: 'ideas',
            count: ideas.length,
            items: ideas,
            timestamp: new Date().toISOString(),
        });
    } catch (err) {
        console.error('Error getting ideas:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/selected
 * Get current selection (pending.md)
 */
router.get('/selected', async (req, res) => {
    try {
        const selected = await planningService.getSelected();
        res.json({
            stage: 'selected',
            count: selected.length,
            items: selected,
            timestamp: new Date().toISOString(),
        });
    } catch (err) {
        console.error('Error getting selected:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/debate
 * Get active debates
 */
router.get('/debate', async (req, res) => {
    try {
        const debates = await planningService.getDebates();
        res.json({
            stage: 'debate',
            count: debates.length,
            items: debates,
            timestamp: new Date().toISOString(),
        });
    } catch (err) {
        console.error('Error getting debates:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/queue
 * Get queue items ready for Conductor
 */
router.get('/queue', async (req, res) => {
    try {
        const queue = await planningService.getQueue();
        res.json({
            stage: 'queue',
            count: queue.length,
            items: queue,
            timestamp: new Date().toISOString(),
        });
    } catch (err) {
        console.error('Error getting queue:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/archive
 * Get archived items (limited)
 */
router.get('/archive', async (req, res) => {
    try {
        const limit = parseInt(req.query.limit) || 20;
        const archive = await planningService.getArchive(limit);
        res.json({
            stage: 'archive',
            count: archive.length,
            items: archive,
            timestamp: new Date().toISOString(),
        });
    } catch (err) {
        console.error('Error getting archive:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/pipeline
 * Get full pipeline state
 */
router.get('/pipeline', async (req, res) => {
    try {
        const state = await planningService.getPipelineState();
        res.json(state);
    } catch (err) {
        console.error('Error getting pipeline state:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/logs
 * Get pipeline log (last N lines)
 */
router.get('/logs', async (req, res) => {
    try {
        const lines = parseInt(req.query.lines) || 50;
        const log = await planningService.getPipelineLog(lines);
        res.json(log);
    } catch (err) {
        console.error('Error getting pipeline log:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/metrics
 * Get pipeline metrics
 */
router.get('/metrics', async (req, res) => {
    try {
        const metrics = await planningService.getPipelineMetrics();
        res.json(metrics);
    } catch (err) {
        console.error('Error getting pipeline metrics:', err);
        res.status(500).json({ error: err.message });
    }
});

/**
 * GET /api/planning/workflow
 * Get full workflow state (Plan → Project/Epic pipeline)
 */
router.get('/workflow', async (req, res) => {
    try {
        const workflow = await planningService.getWorkflow();
        res.json(workflow);
    } catch (err) {
        console.error('Error getting workflow:', err);
        res.status(500).json({ error: err.message });
    }
});

export default router;
