import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useAutonomous } from '../hooks/useAutonomous';
import { MetricGauge } from '../components/Industrial/MetricGauge';

interface Epic {
  id: string;
  name: string;
  project: string;
  status: 'done' | 'active' | 'pending' | 'blocked';
  target_date: string;
  description: string;
}

interface EpicsData {
  epics: Epic[];
  stats: {
    total: number;
    done: number;
    active: number;
    pending: number;
    blocked: number;
    completion: number;
  };
  updated: string;
}

interface Task {
  terminal: string;
  file: string;
  id: string;
  type: string;
  priority: string;
  status: string;
  model: string;
  title: string;
  created: string;
  modified: string;
  content?: string;
  epic?: string;
  ref?: string;
}

interface TasksData {
  tasks: Task[];
  totalActive: number;
}

export function IndustrialAutonomousPage() {
  const { authToken } = useAuth();
  const { data, isLoading, error, refresh } = useAutonomous(authToken, true);
  const [epicsData, setEpicsData] = useState<EpicsData | null>(null);
  const [tasksData, setTasksData] = useState<TasksData | null>(null);
  const [activeTab, setActiveTab] = useState<'cycles' | 'epics' | 'tasks'>('epics');
  const [expandedEpic, setExpandedEpic] = useState<string | null>(null);
  const [expandedTask, setExpandedTask] = useState<string | null>(null);

  // Fetch epics and tasks
  useEffect(() => {
    const fetchData = async () => {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };
        if (authToken) {
          headers['Authorization'] = `Bearer ${authToken}`;
        }

        const [epicsRes, tasksRes] = await Promise.all([
          fetch('/api/autonomous/epics', { headers }),
          fetch('/api/autonomous/tasks', { headers }),
        ]);

        if (epicsRes.ok) {
          setEpicsData(await epicsRes.json());
        }
        if (tasksRes.ok) {
          setTasksData(await tasksRes.json());
        }
      } catch (e) {
        console.error('Failed to fetch autonomous data:', e);
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, [authToken]);

  if (isLoading && !data) {
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
            LOADING AUTONOMOUS DATA...
          </div>
        </div>
      </div>
    );
  }

  if (error && !data) {
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
          ▲ SYSTEM ERROR
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
          onClick={refresh}
          className="ind-button"
          style={{ padding: '10px 24px' }}
        >
          RETRY CONNECTION
        </button>
      </div>
    );
  }

  if (!data) return null;

  // Calculate skip rate
  const totalCycles = data.history.length;
  const skippedCycles = data.history.filter(c => c.result.includes('Skipped') || c.result.includes('Busy')).length;
  const skipPercentage = totalCycles > 0 ? Math.round((skippedCycles / totalCycles) * 100) : 0;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Top metrics row */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '12px',
        }}>
          {/* Metric gauges */}
          <div style={{
            display: 'flex',
            gap: '10px',
            flexWrap: 'wrap',
          }}>
            <MetricGauge
              label="CURRENT"
              value={data.currentCycle}
              max={999}
              color="green"
              size="md"
            />
            <MetricGauge
              label="CYCLES"
              value={totalCycles}
              max={100}
              color="blue"
              size="md"
            />
            <MetricGauge
              label="SKIP"
              value={skipPercentage}
              max={100}
              unit="%"
              color={skipPercentage > 20 ? 'amber' : 'green'}
              size="md"
            />
            <MetricGauge
              label="DISPATCHED"
              value={totalCycles - skippedCycles}
              max={100}
              color="purple"
              size="md"
            />
          </div>

          {/* Next scheduled display */}
          <div style={{
            padding: '10px 14px',
            background: 'linear-gradient(180deg, #050e0a 0%, #0a1814 100%)',
            borderRadius: '6px',
            boxShadow: 'inset 0 1px 4px rgba(0,0,0,0.9), inset 0 0 16px rgba(74,222,128,0.06)',
            minWidth: '180px',
          }}>
            <div style={{
              fontFamily: "'IBM Plex Mono', monospace",
              fontSize: '8px',
              color: '#16a34a',
              letterSpacing: '0.2em',
              marginBottom: '4px',
            }}>
              ▸ NEXT SCHEDULED
            </div>
            <div style={{
              fontFamily: "'Share Tech Mono', monospace",
              fontSize: '14px',
              color: '#4ade80',
              textShadow: '0 0 6px #4ade80',
              marginBottom: '8px',
            }}>
              {data.nextScheduled}
            </div>
          </div>

          {/* Refresh button */}
          <button
            onClick={refresh}
            disabled={isLoading}
            className="ind-button"
            style={{
              padding: '12px 18px',
              opacity: isLoading ? 0.5 : 1,
            }}
          >
            {isLoading ? '⟳ SYNC...' : '⟳ REFRESH'}
          </button>
        </div>
      </div>

      {/* Tab navigation */}
      <div className="ind-chassis" style={{ padding: '10px 18px' }}>
        <div style={{ display: 'flex', gap: '8px' }}>
          {(['epics', 'tasks', 'cycles'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              style={{
                padding: '8px 16px',
                border: 'none',
                cursor: 'pointer',
                background: activeTab === tab
                  ? 'linear-gradient(180deg, #2c2f34 0%, #1a1d22 50%, #0a0c0f 100%)'
                  : 'linear-gradient(180deg, #5a5d63 0%, #4a4d52 50%, #3a3d42 100%)',
                borderRadius: '5px',
                boxShadow: activeTab === tab
                  ? 'inset 0 2px 4px rgba(0,0,0,0.9), 0 0 10px rgba(74,222,128,0.3)'
                  : 'inset 0 1px 0 rgba(255,255,255,0.18), 0 1px 2px rgba(0,0,0,0.5)',
                fontFamily: "'Oswald', sans-serif",
                fontWeight: 700,
                fontSize: '11px',
                letterSpacing: '0.18em',
                color: activeTab === tab ? '#4ade80' : '#0a0c0f',
                textShadow: activeTab === tab ? '0 0 6px #4ade80' : 'none',
              }}
            >
              {tab.toUpperCase()}
            </button>
          ))}
        </div>
      </div>

      {/* Epics Panel */}
      {activeTab === 'epics' && epicsData && (
        <div className="ind-chassis" style={{ padding: '14px 18px' }}>
          <div style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: '12px',
          }}>
            <div style={{
              fontFamily: "'Oswald', sans-serif",
              fontSize: '12px',
              fontWeight: 600,
              color: '#0a0c0f',
              letterSpacing: '0.18em',
              textShadow: '0 1px 0 rgba(255,255,255,0.2)',
            }}>
              EPIC STATUS ({epicsData.stats.completion}% COMPLETE)
            </div>
            <div style={{ display: 'flex', gap: '8px' }}>
              {['done', 'active', 'pending', 'blocked'].map(status => (
                <div key={status} style={{
                  padding: '4px 8px',
                  background: 'linear-gradient(180deg, #1a1d22, #0a0c0f)',
                  borderRadius: '4px',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px',
                }}>
                  <span style={{
                    width: '8px',
                    height: '8px',
                    borderRadius: '50%',
                    background: status === 'done' ? '#4ade80' :
                      status === 'active' ? '#38bdf8' :
                      status === 'pending' ? '#a855f7' : '#ef4444',
                    boxShadow: `0 0 4px ${status === 'done' ? '#4ade80' :
                      status === 'active' ? '#38bdf8' :
                      status === 'pending' ? '#a855f7' : '#ef4444'}`,
                  }} />
                  <span style={{
                    fontFamily: "'IBM Plex Mono', monospace",
                    fontSize: '10px',
                    color: '#94a3b8',
                  }}>
                    {epicsData.stats[status as keyof typeof epicsData.stats]}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Progress bar */}
          <div style={{
            height: '8px',
            background: 'linear-gradient(180deg, #1a1d22, #0a0c0f)',
            borderRadius: '4px',
            marginBottom: '16px',
            overflow: 'hidden',
          }}>
            <div style={{
              height: '100%',
              width: `${epicsData.stats.completion}%`,
              background: 'linear-gradient(90deg, #16a34a, #4ade80)',
              boxShadow: '0 0 8px #4ade80',
              transition: 'width 0.5s ease',
            }} />
          </div>

          {/* Epics list */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
            {epicsData.epics.map(epic => {
              const statusColor = epic.status === 'done' ? '#4ade80' :
                epic.status === 'active' ? '#38bdf8' :
                epic.status === 'pending' ? '#a855f7' : '#ef4444';
              const isExpanded = expandedEpic === epic.id;

              return (
                <div key={epic.id} style={{
                  background: 'linear-gradient(180deg, #2c2f34 0%, #1a1d22 100%)',
                  borderRadius: '4px',
                  borderLeft: `3px solid ${statusColor}`,
                  overflow: 'hidden',
                }}>
                  <div
                    onClick={() => setExpandedEpic(isExpanded ? null : epic.id)}
                    style={{
                      padding: '10px 14px',
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      cursor: 'pointer',
                      transition: 'background 0.15s ease',
                    }}
                    onMouseEnter={(e) => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
                    onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '12px',
                        color: statusColor,
                        transform: isExpanded ? 'rotate(90deg)' : 'rotate(0deg)',
                        transition: 'transform 0.2s ease',
                      }}>
                        ▶
                      </span>
                      <div>
                        <div style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '10px',
                          color: statusColor,
                          letterSpacing: '0.1em',
                          marginBottom: '2px',
                        }}>
                          {epic.id}
                        </div>
                        <div style={{
                          fontFamily: "'Oswald', sans-serif",
                          fontSize: '13px',
                          fontWeight: 500,
                          color: '#e2e8f0',
                        }}>
                          {epic.name}
                        </div>
                        <div style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '10px',
                          color: '#6b7280',
                          marginTop: '2px',
                        }}>
                          {epic.project} • Target: {epic.target_date}
                        </div>
                      </div>
                    </div>
                    <div style={{
                      padding: '4px 10px',
                      background: `linear-gradient(180deg, ${statusColor}22, ${statusColor}11)`,
                      borderRadius: '3px',
                      border: `1px solid ${statusColor}44`,
                    }}>
                      <span style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '10px',
                        color: statusColor,
                        letterSpacing: '0.1em',
                        textTransform: 'uppercase',
                      }}>
                        {epic.status}
                      </span>
                    </div>
                  </div>
                  {/* Expandable description */}
                  <div style={{
                    maxHeight: isExpanded ? '200px' : '0',
                    overflow: 'hidden',
                    transition: 'max-height 0.3s ease, padding 0.3s ease',
                    padding: isExpanded ? '0 14px 14px 30px' : '0 14px 0 30px',
                  }}>
                    <div style={{
                      padding: '10px 14px',
                      background: 'linear-gradient(180deg, #1a1d22 0%, #0a0c0f 100%)',
                      borderRadius: '4px',
                      borderLeft: `2px solid ${statusColor}44`,
                    }}>
                      <div style={{
                        fontFamily: "'IBM Plex Mono', monospace",
                        fontSize: '11px',
                        color: '#94a3b8',
                        lineHeight: 1.5,
                      }}>
                        {epic.description || 'Nincs leírás megadva.'}
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Tasks Panel */}
      {activeTab === 'tasks' && tasksData && (
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
            ACTIVE TERMINAL TASKS ({tasksData.totalActive})
          </div>

          {tasksData.tasks.length === 0 ? (
            <div style={{
              padding: '32px',
              textAlign: 'center',
              background: 'linear-gradient(180deg, #1a1d22, #0a0c0f)',
              borderRadius: '6px',
            }}>
              <span style={{
                fontFamily: "'IBM Plex Mono', monospace",
                fontSize: '11px',
                color: '#6b7280',
              }}>
                No active tasks in terminal inboxes
              </span>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
              {tasksData.tasks.slice(0, 15).map(task => {
                const priorityColor = task.priority === 'critical' ? '#ef4444' :
                  task.priority === 'high' ? '#f97316' :
                  task.priority === 'medium' ? '#fbbf24' : '#94a3b8';
                const terminalColor = {
                  conductor: '#4ade80',
                  architect: '#a855f7',
                  backend: '#38bdf8',
                  frontend: '#ec4899',
                  designer: '#f97316',
                  librarian: '#fbbf24',
                  explorer: '#06b6d4',
                }[task.terminal] || '#94a3b8';
                const taskKey = `${task.terminal}-${task.file}`;
                const isExpanded = expandedTask === taskKey;

                return (
                  <div key={taskKey} style={{
                    background: 'linear-gradient(180deg, #2c2f34 0%, #1a1d22 100%)',
                    borderRadius: '4px',
                    borderLeft: `3px solid ${terminalColor}`,
                    overflow: 'hidden',
                  }}>
                    <div
                      onClick={() => setExpandedTask(isExpanded ? null : taskKey)}
                      style={{
                        padding: '10px 14px',
                        cursor: 'pointer',
                        transition: 'background 0.15s ease',
                      }}
                      onMouseEnter={(e) => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
                      onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                    >
                      <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'flex-start',
                        marginBottom: '4px',
                      }}>
                        <div style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '8px',
                        }}>
                          <span style={{
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '12px',
                            color: terminalColor,
                            transform: isExpanded ? 'rotate(90deg)' : 'rotate(0deg)',
                            transition: 'transform 0.2s ease',
                          }}>
                            ▶
                          </span>
                          <span style={{
                            padding: '2px 6px',
                            background: `${terminalColor}22`,
                            border: `1px solid ${terminalColor}44`,
                            borderRadius: '3px',
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '9px',
                            color: terminalColor,
                            letterSpacing: '0.1em',
                            textTransform: 'uppercase',
                          }}>
                            {task.terminal}
                          </span>
                          <span style={{
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '9px',
                            color: priorityColor,
                          }}>
                            {task.priority.toUpperCase()}
                          </span>
                        </div>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '9px',
                          color: '#6b7280',
                        }}>
                          {task.model}
                        </span>
                      </div>
                      <div style={{
                        fontFamily: "'Oswald', sans-serif",
                        fontSize: '12px',
                        fontWeight: 500,
                        color: '#e2e8f0',
                        marginBottom: '2px',
                        paddingLeft: '20px',
                      }}>
                        {task.title}
                      </div>
                      <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px',
                        paddingLeft: '20px',
                        flexWrap: 'wrap',
                      }}>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '9px',
                          color: '#6b7280',
                        }}>
                          {task.id} • {new Date(task.modified).toLocaleString('hu-HU')}
                        </span>
                        {task.epic && (
                          <span style={{
                            padding: '2px 6px',
                            background: 'linear-gradient(180deg, #065f46 0%, #064e3b 100%)',
                            border: '1px solid #16a34a44',
                            borderRadius: '3px',
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '8px',
                            color: '#4ade80',
                            letterSpacing: '0.05em',
                            boxShadow: '0 0 4px rgba(74,222,128,0.2)',
                          }}>
                            {task.epic}
                          </span>
                        )}
                      </div>
                    </div>
                    {/* Expandable content */}
                    <div style={{
                      maxHeight: isExpanded ? '300px' : '0',
                      overflow: 'hidden',
                      transition: 'max-height 0.3s ease, padding 0.3s ease',
                      padding: isExpanded ? '0 14px 14px 30px' : '0 14px 0 30px',
                    }}>
                      <div style={{
                        padding: '10px 14px',
                        background: 'linear-gradient(180deg, #1a1d22 0%, #0a0c0f 100%)',
                        borderRadius: '4px',
                        borderLeft: `2px solid ${terminalColor}44`,
                      }}>
                        <div style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '11px',
                          color: '#94a3b8',
                          lineHeight: 1.5,
                          whiteSpace: 'pre-wrap',
                        }}>
                          {task.content || 'Nincs részletes leírás.'}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Cycles Panel (original history) */}
      {activeTab === 'cycles' && (
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
            CYCLE HISTORY
          </div>

          <div style={{ overflowX: 'auto' }}>
            <table style={{
              width: '100%',
              borderCollapse: 'separate',
              borderSpacing: '0 2px',
            }}>
              <thead>
                <tr style={{
                  background: 'linear-gradient(180deg, #1a1d22, #0a0c0f)',
                  borderRadius: '4px',
                }}>
                  <th style={tableHeaderStyle}>ID</th>
                  <th style={tableHeaderStyle}>TIMESTAMP</th>
                  <th style={tableHeaderStyle}>CONDUCTOR</th>
                  <th style={tableHeaderStyle}>DISPATCHED</th>
                  <th style={tableHeaderStyle}>RESULT</th>
                  <th style={tableHeaderStyle}>DURATION</th>
                </tr>
              </thead>
              <tbody>
                {data?.history.map((cycle) => {
                  const isSuccess = cycle.result.includes('Dispatched');
                  const isSkipped = cycle.result.includes('Skipped') || cycle.result.includes('Busy');

                  return (
                    <tr key={cycle.id} style={{
                      background: 'linear-gradient(180deg, #2c2f34 0%, #1a1d22 100%)',
                    }}>
                      <td style={tableCellStyle}>
                        <span style={{
                          fontFamily: "'Share Tech Mono', monospace",
                          fontSize: '12px',
                          color: '#4ade80',
                        }}>
                          #{cycle.id}
                        </span>
                      </td>
                      <td style={tableCellStyle}>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '11px',
                          color: '#94a3b8',
                        }}>
                          {new Date(cycle.timestamp).toLocaleString('hu-HU')}
                        </span>
                      </td>
                      <td style={tableCellStyle}>
                        <div style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '6px',
                          padding: '4px 8px',
                          background: cycle.conductor_status === 'WORKING'
                            ? 'linear-gradient(180deg, #065f46, #064e3b)'
                            : 'linear-gradient(180deg, #78350f, #451a03)',
                          borderRadius: '3px',
                          boxShadow: cycle.conductor_status === 'WORKING'
                            ? '0 0 6px rgba(74,222,128,0.3)'
                            : '0 0 6px rgba(251,191,36,0.3)',
                        }}>
                          <span style={{
                            width: '6px',
                            height: '6px',
                            borderRadius: '50%',
                            background: cycle.conductor_status === 'WORKING'
                              ? 'radial-gradient(circle at 30% 30%, #d1fae5 0%, #4ade80 45%, #16a34a 85%)'
                              : 'radial-gradient(circle at 30% 30%, #fef3c7 0%, #fbbf24 45%, #b45309 85%)',
                            boxShadow: cycle.conductor_status === 'WORKING'
                              ? '0 0 6px #4ade80'
                              : '0 0 6px #fbbf24',
                          }} />
                          <span style={{
                            fontFamily: "'IBM Plex Mono', monospace",
                            fontSize: '10px',
                            color: cycle.conductor_status === 'WORKING' ? '#4ade80' : '#fbbf24',
                            letterSpacing: '0.1em',
                          }}>
                            {cycle.conductor_status}
                          </span>
                        </div>
                      </td>
                      <td style={tableCellStyle}>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '11px',
                          color: cycle.dispatched_task ? '#94a3b8' : '#6b7280',
                        }}>
                          {cycle.dispatched_task || '—'}
                        </span>
                      </td>
                      <td style={tableCellStyle}>
                        <span style={{
                          fontFamily: "'IBM Plex Mono', monospace",
                          fontSize: '11px',
                          color: isSuccess ? '#4ade80' : isSkipped ? '#fbbf24' : '#ef4444',
                        }}>
                          {isSuccess ? '✅' : isSkipped ? '⏭️' : '❌'} {cycle.result}
                        </span>
                      </td>
                      <td style={tableCellStyle}>
                        <span style={{
                          fontFamily: "'Share Tech Mono', monospace",
                          fontSize: '11px',
                          color: '#94a3b8',
                        }}>
                          {(cycle.duration_ms / 1000).toFixed(1)}s
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

const tableHeaderStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontFamily: "'Oswald', sans-serif",
  fontSize: '9px',
  fontWeight: 600,
  color: '#6b7280',
  letterSpacing: '0.18em',
  textAlign: 'left',
  textTransform: 'uppercase',
};

const tableCellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderTop: '1px solid rgba(0,0,0,0.3)',
};
