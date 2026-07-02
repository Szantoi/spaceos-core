"use strict";
// epicManager.ts - Load and manage EPICS.yaml
// ADR-053: Mode #4 Program-Awareness (2026-07-02)
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.loadActiveEpic = loadActiveEpic;
exports.getEpicProgress = getEpicProgress;
exports.getNextCheckpoint = getNextCheckpoint;
exports.completeEpic = completeEpic;
exports.loadAllEpics = loadAllEpics;
const fs = __importStar(require("fs"));
const yaml = __importStar(require("js-yaml"));
const EPICS_PATH = '/opt/spaceos/docs/projects/EPICS.yaml';
/**
 * Load active epic from EPICS.yaml
 * Returns the first epic with status='active', or null if none found
 */
function loadActiveEpic() {
    try {
        if (!fs.existsSync(EPICS_PATH)) {
            console.warn('[epicManager] EPICS.yaml not found');
            return null;
        }
        const content = fs.readFileSync(EPICS_PATH, 'utf-8');
        const data = yaml.load(content);
        if (!data || !data.epics || !Array.isArray(data.epics)) {
            console.warn('[epicManager] Invalid EPICS.yaml structure');
            return null;
        }
        const activeEpic = data.epics.find(epic => epic.status === 'active');
        return activeEpic || null;
    }
    catch (error) {
        console.error('[epicManager] Failed to load active epic:', error);
        return null;
    }
}
/**
 * Get epic completion progress (percentage)
 * Based on checkpoint completion
 */
function getEpicProgress(epic) {
    if (!epic.checkpoints || epic.checkpoints.length === 0) {
        return 0;
    }
    const doneCount = epic.checkpoints.filter(cp => cp.status === 'done').length;
    return Math.round((doneCount / epic.checkpoints.length) * 100);
}
/**
 * Get next pending checkpoint for an epic
 */
function getNextCheckpoint(epic) {
    if (!epic.checkpoints) {
        return null;
    }
    return epic.checkpoints.find(cp => cp.status === 'pending') || null;
}
/**
 * Mark epic as complete in EPICS.yaml
 * Updates the epic status to 'done'
 *
 * @param epicId Epic ID to complete
 * @returns true if successful, false otherwise
 */
function completeEpic(epicId) {
    try {
        if (!fs.existsSync(EPICS_PATH)) {
            console.error('[epicManager] EPICS.yaml not found');
            return false;
        }
        const content = fs.readFileSync(EPICS_PATH, 'utf-8');
        const data = yaml.load(content);
        if (!data || !data.epics) {
            console.error('[epicManager] Invalid EPICS.yaml structure');
            return false;
        }
        const epic = data.epics.find(e => e.id === epicId);
        if (!epic) {
            console.error(`[epicManager] Epic ${epicId} not found`);
            return false;
        }
        // Update status
        epic.status = 'done';
        // Write back to file
        const yamlContent = yaml.dump(data, { lineWidth: -1 });
        fs.writeFileSync(EPICS_PATH, yamlContent, 'utf-8');
        console.log(`[epicManager] Epic ${epicId} marked as DONE`);
        return true;
    }
    catch (error) {
        console.error('[epicManager] Failed to complete epic:', error);
        return false;
    }
}
/**
 * Load all epics (for dependency checking)
 */
function loadAllEpics() {
    try {
        if (!fs.existsSync(EPICS_PATH)) {
            return [];
        }
        const content = fs.readFileSync(EPICS_PATH, 'utf-8');
        const data = yaml.load(content);
        return data?.epics || [];
    }
    catch (error) {
        console.error('[epicManager] Failed to load epics:', error);
        return [];
    }
}
