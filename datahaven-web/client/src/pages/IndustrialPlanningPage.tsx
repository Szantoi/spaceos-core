import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../hooks/useAuth';
import { MetricGauge } from '../components/Industrial/MetricGauge';
import { JogWheel } from '../components/Industrial/JogWheel';

interface PlanningItem {
  id: string;
  title: string;
  status: 'idea' | 'selected' | 'debate' | 'consensus' | 'queue';
  priority: 'critical' | 'high' | 'medium' | 'low';
  segment?: string;
  createdAt: string;
  confidence?: number;
}

interface PlanningMetrics {
  ideas: number;
  selected: number;
  inDebate: number;
  consensus: number;
  queued: number;
  lastScan?: string;
}

const STAGE_COLORS: Record<string, { bg: string; text: string; glow: string }> = {
  idea: { bg: 'rgba(59, 130, 246, 0.15)', text: '#60a5fa', glow: '#3b82f6' },
  selected: { bg: 'rgba(168, 85, 247, 0.15)', text: '#c084fc', glow: '#a855f7' },
  debate: { bg: 'rgba(251, 191, 36, 0.15)', text: '#fbbf24', glow: '#f59e0b' },
  consensus: { bg: 'rgba(74, 222, 128, 0.15)', text: '#4ade80', glow: '#22c55e' },
  queue: { bg: 'rgba(34, 211, 238, 0.15)', text: '#22d3ee', glow: '#06b6d4' },
};

const PRIORITY_COLORS: Record<string, string> = {
  critical: '#ef4444',
  high: '#f97316',
  medium: '#fbbf24',
  low: '#60a5fa',
};

const STAGES = ['all', 'idea', 'selected', 'debate', 'consensus', 'queue'] as const;

export function IndustrialPlanningPage() {
  const { authToken } = useAuth();
  const [items, setItems] = useState<PlanningItem[]>([]);
  const [metrics, setMetrics] = useState<PlanningMetrics>({
    ideas: 0,
    selected: 0,
    inDebate: 0,
    consensus: 0,
    queued: 0,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [selectedStageIndex, setSelectedStageIndex] = useState(0);

  const loadPlanningData = useCallback(async () => {
    if (!authToken) return;

    try {
      const headers = {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      };

      const itemsRes = await fetch('/api/planning/items', { headers });
      if (itemsRes.ok) {
        const data = await itemsRes.json();
        setItems(data.items || []);
        setMetrics(data.metrics || metrics);
      }

      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load planning data');
    } finally {
      setIsLoading(false);
    }
  }, [authToken, metrics]);

  useEffect(() => {
    loadPlanningData();
    const interval = setInterval(loadPlanningData, 60000);
    return () => clearInterval(interval);
  }, [loadPlanningData]);

  const filteredItems = items.filter(item => {
    if (filterStatus !== 'all' && item.status !== filterStatus) return false;
    return true;
  });


  if (isLoading && items.length === 0) {
    return (
      <div className="ind-chassis" style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '48px',
        margin: '24px',
      }}>
        <div style={{ textAlign: 'center' }}>
          <div style={{
            width: '48px',
            height: '48px',
            margin: '0 auto 16px',
            border: '3px solid transparent',
            borderTopColor: '#4ade80',
            borderRadius: '50%',
            animation: 'spin-detent 1s linear infinite',
          }} />
          <div style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '14px',
            color: '#4ade80',
            letterSpacing: '0.2em',
            textShadow: '0 0 6px #4ade80',
          }}>
            LOADING PIPELINE...
          </div>
        </div>
      </div>
    );
  }

  if (error && items.length === 0) {
    return (
      <div className="ind-chassis" style={{
        padding: '32px',
        margin: '24px',
        textAlign: 'center',
      }}>
        <div style={{
          fontFamily: "'Oswald', sans-serif",
          fontSize: '18px',
          color: '#ef4444',
          letterSpacing: '0.15em',
          marginBottom: '16px',
        }}>
          PIPELINE ERROR
        </div>
        <div style={{
          fontFamily: "'IBM Plex Mono', monospace",
          fontSize: '12px',
          color: '#94a3b8',
          marginBottom: '24px',
        }}>
          {error}
        </div>
        <button
          onClick={loadPlanningData}
          className="ind-button"
          style={{ padding: '10px 24px' }}
        >
          RETRY CONNECTION
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Metrics Panel */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '12px',
        }}>
          <div style={{
            display: 'flex',
            gap: '10px',
            flexWrap: 'wrap',
          }}>
            <MetricGauge label="IDEAS" value={metrics.ideas} max={20} color="blue" size="md" />
            <MetricGauge label="SELECT" value={metrics.selected} max={10} color="purple" size="md" />
            <MetricGauge label="DEBATE" value={metrics.inDebate} max={5} color="amber" size="md" />
            <MetricGauge label="CONSENS" value={metrics.consensus} max={5} color="green" size="md" />
            <MetricGauge label="QUEUE" value={metrics.queued} max={10} color="blue" size="md" />
          </div>

          <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
            <JogWheel
              items={STAGES.map(s => s === 'all' ? 'ALL STAGES' : s.toUpperCase())}
              selectedIndex={selectedStageIndex}
              onSelect={(idx) => setSelectedStageIndex(idx)}
              onCommit={() => setFilterStatus(STAGES[selectedStageIndex])}
            />
            <button
              onClick={loadPlanningData}
              disabled={isLoading}
              className="ind-button"
              style={{
                padding: '12px 20px',
                fontSize: '12px',
                opacity: isLoading ? 0.5 : 1,
              }}
            >
              {isLoading ? 'SYNC...' : 'REFRESH'}
            </button>
          </div>
        </div>
      </div>

      {/* Pipeline Flow Visualization */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          fontFamily: "'Oswald', sans-serif",
          fontSize: '12px',
          fontWeight: 600,
          color: '#0a0c0f',
          letterSpacing: '0.18em',
          textShadow: '0 1px 0 rgba(255,255,255,0.2)',
          marginBottom: '12px',
        }}>
          PLANNING PIPELINE
        </div>
        <div style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          flexWrap: 'wrap',
          gap: '16px',
          padding: '8px 0',
        }}>
          {(['idea', 'selected', 'debate', 'consensus', 'queue'] as const).map((stage) => (
            <div key={stage} style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              gap: '6px',
            }}>
              <div style={{
                width: '40px',
                height: '40px',
                borderRadius: '50%',
                background: STAGE_COLORS[stage].bg,
                border: `2px solid ${STAGE_COLORS[stage].text}`,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                boxShadow: `0 0 8px ${STAGE_COLORS[stage].glow}40`,
              }}>
                <span style={{
                  fontFamily: "'Share Tech Mono', monospace",
                  fontSize: '14px',
                  color: STAGE_COLORS[stage].text,
                  fontWeight: 'bold',
                }}>
                  {stage === 'idea' ? metrics.ideas :
                   stage === 'selected' ? metrics.selected :
                   stage === 'debate' ? metrics.inDebate :
                   stage === 'consensus' ? metrics.consensus :
                   metrics.queued}
                </span>
              </div>
              <span style={{
                fontFamily: "'IBM Plex Mono', monospace",
                fontSize: '9px',
                color: STAGE_COLORS[stage].text,
                letterSpacing: '0.1em',
                textTransform: 'uppercase',
              }}>
                {stage}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Items List */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          fontFamily: "'Oswald', sans-serif",
          fontSize: '12px',
          fontWeight: 600,
          color: '#0a0c0f',
          letterSpacing: '0.18em',
          textShadow: '0 1px 0 rgba(255,255,255,0.2)',
          marginBottom: '12px',
        }}>
          PLANNING ITEMS ({filteredItems.length})
        </div>
        <div style={{
          display: 'flex',
          flexDirection: 'column',
          gap: '8px',
          maxHeight: '400px',
          overflowY: 'auto',
        }}>
          {filteredItems.length === 0 ? (
            <div style={{
              textAlign: 'center',
              padding: '32px',
              background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
              borderRadius: '6px',
            }}>
              <div style={{
                fontFamily: "'IBM Plex Mono', monospace",
                fontSize: '12px',
                color: '#6b7280',
              }}>
                {filterStatus === 'all'
                  ? 'No planning items. Pipeline generates ideas every 30 minutes.'
                  : `No items in ${filterStatus} stage.`}
              </div>
            </div>
          ) : (
            filteredItems.map((item) => (
              <div key={item.id} style={{
                background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
                borderRadius: '6px',
                padding: '10px 12px',
                boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9)',
                borderLeft: `3px solid ${STAGE_COLORS[item.status]?.text || '#6b7280'}`,
              }}>
                <div style={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  justifyContent: 'space-between',
                  gap: '12px',
                }}>
                  <div style={{ flex: 1 }}>
                    <div style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '8px',
                      marginBottom: '6px',
                    }}>
                      <span style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '8px',
                        color: STAGE_COLORS[item.status]?.text || '#6b7280',
                        letterSpacing: '0.1em',
                        textTransform: 'uppercase',
                        padding: '2px 6px',
                        background: STAGE_COLORS[item.status]?.bg || 'transparent',
                        borderRadius: '3px',
                      }}>
                        {item.status}
                      </span>
                      <span style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '8px',
                        color: PRIORITY_COLORS[item.priority] || '#6b7280',
                        letterSpacing: '0.1em',
                        textTransform: 'uppercase',
                      }}>
                        {item.priority}
                      </span>
                      {item.segment && (
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '8px',
                          color: '#6b7280',
                        }}>
                          {item.segment}
                        </span>
                      )}
                    </div>
                    <div style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '11px',
                      color: '#e2e8f0',
                    }}>
                      {item.title}
                    </div>
                    <div style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '8px',
                      color: '#6b7280',
                      marginTop: '4px',
                    }}>
                      Created: {new Date(item.createdAt).toLocaleDateString()}
                    </div>
                  </div>
                  {item.confidence !== undefined && (
                    <div style={{
                      textAlign: 'right',
                    }}>
                      <div style={{
                        fontFamily: "'Share Tech Mono', monospace",
                        fontSize: '16px',
                        color: '#4ade80',
                        textShadow: '0 0 6px #4ade80',
                      }}>
                        {Math.round(item.confidence * 100)}%
                      </div>
                      <div style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '7px',
                        color: '#6b7280',
                        letterSpacing: '0.1em',
                      }}>
                        CONFIDENCE
                      </div>
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Last Scan Info */}
      {metrics.lastScan && (
        <div style={{
          textAlign: 'center',
          fontFamily: "'IBM Plex Mono', monospace",
          fontSize: '9px',
          color: '#6b7280',
        }}>
          Last scan: {new Date(metrics.lastScan).toLocaleString()}
        </div>
      )}
    </div>
  );
}
