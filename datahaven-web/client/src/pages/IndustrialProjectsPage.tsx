import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { MetricGauge } from '../components/Industrial/MetricGauge';
import { JogWheel } from '../components/Industrial/JogWheel';

interface Project {
  id: string;
  name: string;
  status: 'planning' | 'active' | 'blocked' | 'done';
  priority: 'critical' | 'high' | 'medium' | 'low';
  startDate: string;
  endDate: string;
  progress: number;
  terminal: string;
  epic?: string;
  tasks: number;
  completedTasks: number;
}

interface Milestone {
  id: string;
  name: string;
  date: string;
  status: 'upcoming' | 'completed';
}

const STATUS_COLORS: Record<string, { bg: string; text: string; glow: string }> = {
  planning: { bg: 'rgba(59, 130, 246, 0.15)', text: '#60a5fa', glow: '#3b82f6' },
  active: { bg: 'rgba(74, 222, 128, 0.15)', text: '#4ade80', glow: '#22c55e' },
  blocked: { bg: 'rgba(239, 68, 68, 0.15)', text: '#f87171', glow: '#ef4444' },
  done: { bg: 'rgba(107, 114, 128, 0.15)', text: '#9ca3af', glow: '#6b7280' },
};

const PRIORITY_COLORS: Record<string, string> = {
  critical: '#ef4444',
  high: '#f97316',
  medium: '#fbbf24',
  low: '#60a5fa',
};

const STATUSES = ['all', 'planning', 'active', 'blocked', 'done'] as const;

export function IndustrialProjectsPage() {
  const navigate = useNavigate();
  const { authToken } = useAuth();
  const [projects, setProjects] = useState<Project[]>([]);
  const [milestones, setMilestones] = useState<Milestone[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'list' | 'gantt'>('gantt');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [selectedStatusIndex, setSelectedStatusIndex] = useState(0);

  const loadProjectsData = useCallback(async () => {
    if (!authToken) return;

    try {
      const headers = {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json',
      };

      const res = await fetch('/api/projects', { headers });
      if (res.ok) {
        const data = await res.json();
        setProjects(data.projects || []);
        setMilestones(data.milestones || []);
      }

      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load projects');
    } finally {
      setIsLoading(false);
    }
  }, [authToken]);

  useEffect(() => {
    loadProjectsData();
    const interval = setInterval(loadProjectsData, 60000);
    return () => clearInterval(interval);
  }, [loadProjectsData]);

  const calculatePosition = (dateStr: string) => {
    const now = new Date();
    const date = new Date(dateStr);
    const startOfTimeline = new Date(now);
    startOfTimeline.setMonth(now.getMonth() - 2);

    const endOfTimeline = new Date(now);
    endOfTimeline.setMonth(now.getMonth() + 6);

    const totalMs = endOfTimeline.getTime() - startOfTimeline.getTime();
    const offsetMs = date.getTime() - startOfTimeline.getTime();
    return Math.max(0, Math.min(100, (offsetMs / totalMs) * 100));
  };

  const calculateWidth = (start: string, end: string) => {
    const startDate = new Date(start);
    const endDate = new Date(end);
    const now = new Date();

    const startOfTimeline = new Date(now);
    startOfTimeline.setMonth(now.getMonth() - 2);

    const endOfTimeline = new Date(now);
    endOfTimeline.setMonth(now.getMonth() + 6);

    const totalMs = endOfTimeline.getTime() - startOfTimeline.getTime();
    const durationMs = endDate.getTime() - startDate.getTime();
    return Math.max(2, (durationMs / totalMs) * 100);
  };

  // Filter projects
  const filteredProjects = statusFilter === 'all'
    ? projects
    : projects.filter(p => p.status === statusFilter);

  // Calculate stats
  const activeProjects = projects.filter(p => p.status === 'active').length;
  const blockedProjects = projects.filter(p => p.status === 'blocked').length;
  const avgProgress = projects.length > 0
    ? Math.round(projects.reduce((sum, p) => sum + p.progress, 0) / projects.length)
    : 0;
  const upcomingMilestones = milestones.filter(m => m.status === 'upcoming').length;

  if (isLoading && projects.length === 0) {
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
            LOADING PROJECTS...
          </div>
        </div>
      </div>
    );
  }

  if (error && projects.length === 0) {
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
          PROJECT ERROR
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
          onClick={loadProjectsData}
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
            <MetricGauge label="TOTAL" value={projects.length} max={20} color="purple" size="md" />
            <MetricGauge label="ACTIVE" value={activeProjects} max={10} color="green" size="md" />
            <MetricGauge label="BLOCKED" value={blockedProjects} max={5} color="red" size="md" />
            <MetricGauge label="PROGRESS" value={avgProgress} max={100} color="blue" size="md" />
            <MetricGauge label="MILESTONES" value={upcomingMilestones} max={10} color="amber" size="md" />
          </div>

          <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
            <JogWheel
              items={STATUSES.map(s => s === 'all' ? 'ALL' : s.toUpperCase())}
              selectedIndex={selectedStatusIndex}
              onSelect={(idx) => setSelectedStatusIndex(idx)}
              onCommit={() => setStatusFilter(STATUSES[selectedStatusIndex])}
            />
            <div style={{
              display: 'flex',
              background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
              borderRadius: '4px',
              border: '1px solid rgba(74, 222, 128, 0.2)',
              overflow: 'hidden',
            }}>
              <button
                onClick={() => setViewMode('list')}
                style={{
                  padding: '8px 12px',
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '10px',
                  color: viewMode === 'list' ? '#4ade80' : '#6b7280',
                  background: viewMode === 'list' ? 'rgba(74, 222, 128, 0.15)' : 'transparent',
                  border: 'none',
                  cursor: 'pointer',
                  letterSpacing: '0.1em',
                }}
              >
                LIST
              </button>
              <button
                onClick={() => setViewMode('gantt')}
                style={{
                  padding: '8px 12px',
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '10px',
                  color: viewMode === 'gantt' ? '#4ade80' : '#6b7280',
                  background: viewMode === 'gantt' ? 'rgba(74, 222, 128, 0.15)' : 'transparent',
                  border: 'none',
                  cursor: 'pointer',
                  letterSpacing: '0.1em',
                }}
              >
                GANTT
              </button>
            </div>
            <button
              onClick={() => navigate('/flow')}
              className="ind-button"
              style={{
                padding: '8px 14px',
              }}
            >
              FLOW EDITOR
            </button>
            <button
              onClick={loadProjectsData}
              disabled={isLoading}
              className="ind-button"
              style={{
                padding: '8px 14px',
                opacity: isLoading ? 0.5 : 1,
              }}
            >
              {isLoading ? 'SYNC...' : 'REFRESH'}
            </button>
          </div>
        </div>
      </div>

      {/* View Mode Content */}
      {viewMode === 'list' ? (
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
            PROJECT LIST
          </div>
          <div style={{
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
          }}>
            {filteredProjects.length === 0 ? (
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
                  No projects found.
                </div>
              </div>
            ) : (
              filteredProjects.map((project) => (
                <div key={project.id} style={{
                  background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
                  borderRadius: '6px',
                  padding: '12px 14px',
                  boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9)',
                  borderLeft: `3px solid ${STATUS_COLORS[project.status]?.text || '#6b7280'}`,
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
                          fontSize: '12px',
                          color: '#e2e8f0',
                          fontWeight: 'bold',
                        }}>
                          {project.name}
                        </span>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '8px',
                          color: STATUS_COLORS[project.status]?.text || '#6b7280',
                          letterSpacing: '0.1em',
                          textTransform: 'uppercase',
                          padding: '2px 6px',
                          background: STATUS_COLORS[project.status]?.bg || 'transparent',
                          borderRadius: '3px',
                        }}>
                          {project.status}
                        </span>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '8px',
                          color: PRIORITY_COLORS[project.priority] || '#6b7280',
                          letterSpacing: '0.1em',
                          textTransform: 'uppercase',
                        }}>
                          {project.priority}
                        </span>
                      </div>
                      <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '12px',
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '9px',
                        color: '#6b7280',
                      }}>
                        <span>TERM: <span style={{ color: '#94a3b8' }}>{project.terminal}</span></span>
                        {project.epic && <span>EPIC: <span style={{ color: '#94a3b8' }}>{project.epic}</span></span>}
                        <span>
                          {new Date(project.startDate).toLocaleDateString()} →{' '}
                          {new Date(project.endDate).toLocaleDateString()}
                        </span>
                      </div>
                      {/* Progress bar */}
                      <div style={{
                        marginTop: '8px',
                        height: '4px',
                        background: 'rgba(0,0,0,0.5)',
                        borderRadius: '2px',
                        overflow: 'hidden',
                      }}>
                        <div style={{
                          height: '100%',
                          width: `${project.progress}%`,
                          background: 'linear-gradient(90deg, #4ade80, #22c55e)',
                          borderRadius: '2px',
                          boxShadow: '0 0 6px #4ade80',
                        }} />
                      </div>
                    </div>
                    <div style={{
                      textAlign: 'right',
                    }}>
                      <div style={{
                        fontFamily: "'Share Tech Mono', monospace",
                        fontSize: '20px',
                        color: '#4ade80',
                        textShadow: '0 0 6px #4ade80',
                      }}>
                        {project.progress}%
                      </div>
                      <div style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '8px',
                        color: '#6b7280',
                      }}>
                        {project.completedTasks}/{project.tasks} TASKS
                      </div>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      ) : (
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
            GANTT TIMELINE
          </div>

          {/* Timeline Header */}
          <div style={{
            background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
            borderRadius: '6px',
            padding: '8px 12px',
            marginBottom: '8px',
          }}>
            <div style={{
              position: 'relative',
              height: '24px',
            }}>
              {/* Today marker */}
              <div style={{
                position: 'absolute',
                top: '0',
                bottom: '0',
                width: '2px',
                background: '#4ade80',
                left: `${calculatePosition(new Date().toISOString())}%`,
                zIndex: 10,
                boxShadow: '0 0 8px #4ade80',
              }}>
                <div style={{
                  position: 'absolute',
                  top: '-2px',
                  left: '50%',
                  transform: 'translateX(-50%)',
                  background: '#4ade80',
                  color: '#0a0c0f',
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '7px',
                  padding: '1px 4px',
                  borderRadius: '2px',
                  whiteSpace: 'nowrap',
                  letterSpacing: '0.05em',
                }}>
                  TODAY
                </div>
              </div>
              {/* Month labels */}
              <div style={{
                display: 'flex',
                fontFamily: "'IBM Plex Mono', monospace",
                fontSize: '8px',
                color: '#6b7280',
              }}>
                {Array.from({ length: 8 }).map((_, i) => {
                  const date = new Date();
                  date.setMonth(date.getMonth() - 2 + i);
                  return (
                    <div key={i} style={{
                      flex: 1,
                      textAlign: 'center',
                      letterSpacing: '0.05em',
                    }}>
                      {date.toLocaleDateString('en', { month: 'short' }).toUpperCase()}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Project Bars */}
          <div style={{
            display: 'flex',
            flexDirection: 'column',
            gap: '4px',
          }}>
            {filteredProjects.map((project) => (
              <div key={project.id} style={{
                background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
                borderRadius: '4px',
                padding: '8px 12px',
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
              }}>
                <div style={{
                  width: '140px',
                  flexShrink: 0,
                }}>
                  <div style={{
                    fontFamily: "'IBM Plex Mono', monospace",
                    fontSize: '10px',
                    color: '#e2e8f0',
                    whiteSpace: 'nowrap',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    marginBottom: '2px',
                  }}>
                    {project.name}
                  </div>
                  <div style={{
                    fontFamily: "'IBM Plex Mono', monospace",
                    fontSize: '8px',
                    color: '#6b7280',
                  }}>
                    {project.terminal}
                  </div>
                </div>
                <div style={{
                  flex: 1,
                  position: 'relative',
                  height: '20px',
                }}>
                  <div style={{
                    position: 'absolute',
                    top: '50%',
                    transform: 'translateY(-50%)',
                    height: '14px',
                    borderRadius: '3px',
                    left: `${calculatePosition(project.startDate)}%`,
                    width: `${calculateWidth(project.startDate, project.endDate)}%`,
                    background: STATUS_COLORS[project.status]?.bg || 'rgba(107, 114, 128, 0.15)',
                    border: `1px solid ${STATUS_COLORS[project.status]?.text || '#6b7280'}`,
                    overflow: 'hidden',
                  }}>
                    <div style={{
                      height: '100%',
                      width: `${project.progress}%`,
                      background: STATUS_COLORS[project.status]?.text || '#6b7280',
                      opacity: 0.6,
                    }} />
                  </div>
                </div>
                <div style={{
                  width: '40px',
                  textAlign: 'right',
                  fontFamily: "'Share Tech Mono', monospace",
                  fontSize: '11px',
                  color: '#4ade80',
                }}>
                  {project.progress}%
                </div>
              </div>
            ))}

            {/* Milestones */}
            {milestones.map((milestone) => (
              <div key={milestone.id} style={{
                background: 'linear-gradient(180deg, #0a1510 0%, #0f1f18 100%)',
                borderRadius: '4px',
                padding: '8px 12px',
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
              }}>
                <div style={{
                  width: '140px',
                  flexShrink: 0,
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                }}>
                  <span style={{ color: '#fbbf24', fontSize: '12px' }}>*</span>
                  <div>
                    <div style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '10px',
                      color: '#fbbf24',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}>
                      {milestone.name}
                    </div>
                    <div style={{
                      fontFamily: "'IBM Plex Mono', monospace",
                      fontSize: '8px',
                      color: '#6b7280',
                    }}>
                      {new Date(milestone.date).toLocaleDateString()}
                    </div>
                  </div>
                </div>
                <div style={{
                  flex: 1,
                  position: 'relative',
                  height: '20px',
                }}>
                  <div style={{
                    position: 'absolute',
                    top: '50%',
                    left: `${calculatePosition(milestone.date)}%`,
                    transform: 'translate(-50%, -50%)',
                    width: '10px',
                    height: '10px',
                    borderRadius: '50%',
                    background: '#fbbf24',
                    boxShadow: '0 0 8px #fbbf24',
                  }} />
                </div>
                <div style={{ width: '40px' }} />
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
