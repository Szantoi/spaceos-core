import { useAuth } from '../hooks/useAuth';
import { useDashboard } from '../hooks/useDashboard';
import { MetricGauge } from '../components/Industrial/MetricGauge';

export function IndustrialDashboardPage() {
  const { authToken } = useAuth();
  const { data, isLoading, error, refresh } = useDashboard(authToken, true);

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
            LOADING SYSTEM...
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

  // Calculate stats
  const workingCount = data.terminals.filter(t => t.state === 'WORKING').length;
  const totalUnread = data.terminals.reduce((sum, t) => sum + (t.unreadCount || 0), 0);
  const unreadCount = data.stats?.totalUnread || totalUnread;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Top metrics row */}
      <div className="ind-chassis" style={{ padding: '14px 18px' }}>
        <div style={{
          display: 'flex',
          alignItems: 'flex-start',
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
              label="UNREAD"
              value={unreadCount}
              max={50}
              color={unreadCount > 10 ? 'amber' : 'green'}
              size="md"
            />
            <MetricGauge
              label="ACTIVE"
              value={workingCount}
              max={data.terminals.length}
              color="green"
              size="md"
            />
            <MetricGauge
              label="IDLE"
              value={data.stats?.idleTerminals || 0}
              max={data.terminals.length}
              color="blue"
              size="md"
            />
            <MetricGauge
              label="TOTAL"
              value={data.terminals.length}
              max={20}
              color="purple"
              size="md"
            />
            <MetricGauge
              label="DOCS"
              value={data.stats?.documentsIndexed || 0}
              max={1000}
              color="amber"
              size="md"
            />
          </div>

          {/* System status display */}
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
              ▸ SYSTEM STATUS
            </div>
            <div style={{
              fontFamily: "'Share Tech Mono', monospace",
              fontSize: '14px',
              color: '#4ade80',
              textShadow: '0 0 6px #4ade80',
              marginBottom: '8px',
            }}>
              {data.serviceHealth?.status?.toUpperCase() || 'NOMINAL'}
            </div>
            <div style={{
              fontFamily: "'IBM Plex Mono', monospace",
              fontSize: '9px',
              color: '#6b7280',
            }}>
              Updated: {new Date(data.timestamp).toLocaleTimeString('hu-HU')}
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

      {/* Bottom info strip */}
      <div style={{
        display: 'flex',
        gap: '12px',
        flexWrap: 'wrap',
      }}>
        {/* Pipeline status */}
        <div className="ind-chassis" style={{
          flex: 1,
          minWidth: '200px',
          padding: '12px 14px',
        }}>
          <div style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '10px',
            fontWeight: 600,
            color: '#0a0c0f',
            letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.2)',
            marginBottom: '8px',
          }}>
            PIPELINE STATUS
          </div>
          <div style={{
            display: 'flex',
            gap: '16px',
          }}>
            {['IDEA', 'SELECT', 'DEBATE', 'QUEUE'].map((stage, i) => (
              <div key={stage} style={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: '4px',
              }}>
                <div style={{
                  width: '10px',
                  height: '10px',
                  borderRadius: '50%',
                  background: i < 3
                    ? 'radial-gradient(circle at 30% 30%, #d1fae5 0%, #4ade80 45%, #16a34a 85%)'
                    : 'radial-gradient(circle at 30% 30%, #fef3c7 0%, #fbbf24 45%, #b45309 85%)',
                  boxShadow: i < 3 ? '0 0 6px #4ade80' : '0 0 6px #fbbf24',
                }} />
                <span style={{
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '8px',
                  color: '#6b7280',
                  letterSpacing: '0.1em',
                }}>
                  {stage}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Health indicators */}
        <div className="ind-chassis" style={{
          flex: 1,
          minWidth: '200px',
          padding: '12px 14px',
        }}>
          <div style={{
            fontFamily: "'Oswald', sans-serif",
            fontSize: '10px',
            fontWeight: 600,
            color: '#0a0c0f',
            letterSpacing: '0.18em',
            textShadow: '0 1px 0 rgba(255,255,255,0.2)',
            marginBottom: '8px',
          }}>
            SERVICE HEALTH
          </div>
          <div style={{
            display: 'flex',
            gap: '12px',
          }}>
            {['API', 'DB', 'SSE', 'CRON'].map(service => (
              <div key={service} style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
              }}>
                <div className="ind-led ind-led-green" style={{ width: '8px', height: '8px' }} />
                <span style={{
                  fontFamily: "'IBM Plex Mono', monospace",
                  fontSize: '9px',
                  color: '#94a3b8',
                  letterSpacing: '0.1em',
                }}>
                  {service}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
