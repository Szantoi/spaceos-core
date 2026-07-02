"use strict";
// modeDetection.ts - Detect SpaceOS operation mode
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
exports.detectOperationMode = detectOperationMode;
exports.getModeDescription = getModeDescription;
const fs = __importStar(require("fs"));
const yaml = __importStar(require("js-yaml"));
const EPICS_PATH = '/opt/spaceos/docs/projects/EPICS.yaml';
/**
 * Detect current SpaceOS operation mode
 *
 * Mode detection logic:
 * 1. Check for SPACEOS_MODE env var (explicit override)
 * 2. Check EPICS.yaml for active epic → structured_program
 * 3. Check planning queue directory → planning_pipeline
 * 4. Default: manual
 *
 * @returns Current operation mode
 */
function detectOperationMode() {
    // 1. Explicit env var override (for testing)
    const envMode = process.env.SPACEOS_MODE;
    if (envMode === 'manual' || envMode === 'planning_pipeline' || envMode === 'structured_program') {
        return envMode;
    }
    // 2. Check for active epic in EPICS.yaml
    try {
        if (fs.existsSync(EPICS_PATH)) {
            const content = fs.readFileSync(EPICS_PATH, 'utf-8');
            const data = yaml.load(content);
            if (data && data.epics && Array.isArray(data.epics)) {
                const hasActiveEpic = data.epics.some((epic) => epic.status === 'active');
                if (hasActiveEpic) {
                    return 'structured_program';
                }
            }
        }
    }
    catch (error) {
        console.warn('[modeDetection] Failed to read EPICS.yaml:', error);
        // Continue to next check
    }
    // 3. Check planning queue (fallback for Mode #2/#3)
    const queuePath = '/opt/spaceos/docs/planning/queue';
    try {
        if (fs.existsSync(queuePath)) {
            const files = fs.readdirSync(queuePath);
            if (files.length > 0) {
                return 'planning_pipeline';
            }
        }
    }
    catch (error) {
        console.warn('[modeDetection] Failed to check planning queue:', error);
    }
    // 4. Default: manual mode
    return 'manual';
}
/**
 * Get human-readable mode description
 */
function getModeDescription(mode) {
    switch (mode) {
        case 'structured_program':
            return 'Mode #4: Structured Program (EPICS.yaml-driven)';
        case 'planning_pipeline':
            return 'Mode #2/3: Planning Pipeline (idea → consensus)';
        case 'manual':
            return 'Mode #1: Manual (ad-hoc tasks)';
    }
}
