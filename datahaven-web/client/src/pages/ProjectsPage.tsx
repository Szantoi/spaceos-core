import { useState, useEffect } from 'react';
import { useAuth } from '../hooks/useAuth';

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

export function ProjectsPage() {
  const { authToken } = useAuth();
  const [projects, setProjects] = useState<Project[]>([]);
  const [milestones, setMilestones] = useState<Milestone[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'list' | 'gantt'>('gantt');
  const [timeScale, setTimeScale] = useState<'week' | 'month' | 'quarter'>('month');

  useEffect(() => {
    loadProjectsData();
    const interval = setInterval(loadProjectsData, 60000);
    return () => clearInterval(interval);
  }, [authToken]);

  const loadProjectsData = async () => {
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
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'planning': return 'bg-blue-500/10 text-blue-400 border-blue-500/30';
      case 'active': return 'bg-green-500/10 text-green-400 border-green-500/30';
      case 'blocked': return 'bg-red-500/10 text-red-400 border-red-500/30';
      case 'done': return 'bg-gray-500/10 text-gray-400 border-gray-500/30';
      default: return 'bg-gray-500/10 text-gray-400 border-gray-500/30';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'critical': return 'text-red-400';
      case 'high': return 'text-orange-400';
      case 'medium': return 'text-yellow-400';
      case 'low': return 'text-blue-400';
      default: return 'text-gray-400';
    }
  };

  const calculatePosition = (dateStr: string) => {
    const now = new Date();
    const date = new Date(dateStr);
    const startOfTimeline = new Date(now);
    startOfTimeline.setMonth(now.getMonth() - 2);

    const endOfTimeline = new Date(now);
    endOfTimeline.setMonth(now.getMonth() + 6);

    const totalMs = endOfTimeline.getTime() - startOfTimeline.getTime();
    const offsetMs = date.getTime() - startOfTimeline.getTime();
    return (offsetMs / totalMs) * 100;
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
    return (durationMs / totalMs) * 100;
  };

  if (isLoading && projects.length === 0) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <div className="inline-block w-8 h-8 border-2 border-[var(--accent)] border-t-transparent rounded-full animate-spin mb-4" />
          <div className="text-[var(--text-secondary)]">Loading projects...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-[var(--text-primary)]">Projects</h1>
          <p className="text-[var(--text-secondary)] text-sm mt-1">
            {projects.length} active projects, {milestones.filter(m => m.status === 'upcoming').length} upcoming milestones
          </p>
        </div>
        <div className="flex gap-3">
          <div className="flex gap-2 bg-[var(--bg-secondary)] rounded-lg p-1">
            <button
              onClick={() => setViewMode('list')}
              className={`px-3 py-1 rounded transition ${
                viewMode === 'list'
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              List
            </button>
            <button
              onClick={() => setViewMode('gantt')}
              className={`px-3 py-1 rounded transition ${
                viewMode === 'gantt'
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              Gantt
            </button>
          </div>
          <button
            onClick={loadProjectsData}
            className="px-4 py-2 bg-[var(--accent)] text-white rounded-lg hover:bg-[var(--accent-hover)] transition"
            disabled={isLoading}
          >
            {isLoading ? 'Loading...' : 'Refresh'}
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-4">
          <div className="text-red-400">{error}</div>
        </div>
      )}

      {/* View Mode Content */}
      {viewMode === 'list' ? (
        <div className="space-y-3">
          {projects.length === 0 ? (
            <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-8 text-center">
              <div className="text-[var(--text-secondary)]">No projects found.</div>
            </div>
          ) : (
            projects.map((project) => (
              <div
                key={project.id}
                className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-4 hover:border-[var(--accent)] transition"
              >
                <div className="flex items-start justify-between mb-3">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <h3 className="text-[var(--text-primary)] font-medium">{project.name}</h3>
                      <span className={`px-2 py-1 rounded text-xs font-medium border ${getStatusColor(project.status)}`}>
                        {project.status}
                      </span>
                      <span className={`text-xs font-medium ${getPriorityColor(project.priority)}`}>
                        {project.priority.toUpperCase()}
                      </span>
                    </div>
                    <div className="flex items-center gap-4 text-sm text-[var(--text-secondary)]">
                      <span>Terminal: {project.terminal}</span>
                      {project.epic && <span>Epic: {project.epic}</span>}
                      <span>{new Date(project.startDate).toLocaleDateString()} → {new Date(project.endDate).toLocaleDateString()}</span>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="text-2xl font-bold text-[var(--accent)]">{project.progress}%</div>
                    <div className="text-xs text-[var(--text-secondary)]">
                      {project.completedTasks}/{project.tasks} tasks
                    </div>
                  </div>
                </div>
                <div className="w-full bg-[var(--bg-primary)] rounded-full h-2">
                  <div
                    className="bg-[var(--accent)] h-2 rounded-full transition-all"
                    style={{ width: `${project.progress}%` }}
                  />
                </div>
              </div>
            ))
          )}
        </div>
      ) : (
        <div className="space-y-4">
          {/* Time Scale Selector */}
          <div className="flex gap-2 justify-end">
            <select
              value={timeScale}
              onChange={(e) => setTimeScale(e.target.value as 'week' | 'month' | 'quarter')}
              className="px-3 py-2 bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg text-[var(--text-primary)] focus:outline-none focus:border-[var(--accent)]"
            >
              <option value="week">Weekly</option>
              <option value="month">Monthly</option>
              <option value="quarter">Quarterly</option>
            </select>
          </div>

          {/* Gantt Chart */}
          <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg overflow-hidden">
            {/* Timeline Header */}
            <div className="border-b border-[var(--border)] p-4">
              <div className="relative h-8">
                {/* Today marker */}
                <div
                  className="absolute top-0 bottom-0 w-0.5 bg-[var(--accent)] z-10"
                  style={{ left: `${calculatePosition(new Date().toISOString())}%` }}
                >
                  <div className="absolute -top-1 -left-8 bg-[var(--accent)] text-white text-xs px-2 py-0.5 rounded whitespace-nowrap">
                    Today
                  </div>
                </div>
                {/* Month labels */}
                <div className="flex text-xs text-[var(--text-secondary)]">
                  {Array.from({ length: 8 }).map((_, i) => {
                    const date = new Date();
                    date.setMonth(date.getMonth() - 2 + i);
                    return (
                      <div key={i} className="flex-1 text-center">
                        {date.toLocaleDateString('en', { month: 'short', year: 'numeric' })}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Project Bars */}
            <div className="divide-y divide-[var(--border)]">
              {projects.map((project) => (
                <div key={project.id} className="p-4">
                  <div className="flex items-center gap-4">
                    <div className="w-48 flex-shrink-0">
                      <div className="font-medium text-[var(--text-primary)] text-sm mb-1">
                        {project.name}
                      </div>
                      <div className="text-xs text-[var(--text-secondary)]">
                        {project.terminal}
                      </div>
                    </div>
                    <div className="flex-1 relative h-8">
                      <div
                        className={`absolute top-1/2 -translate-y-1/2 h-6 rounded cursor-pointer transition-all hover:h-7 ${
                          project.status === 'active' ? 'bg-green-500/30 border border-green-500' :
                          project.status === 'blocked' ? 'bg-red-500/30 border border-red-500' :
                          project.status === 'done' ? 'bg-gray-500/30 border border-gray-500' :
                          'bg-blue-500/30 border border-blue-500'
                        }`}
                        style={{
                          left: `${calculatePosition(project.startDate)}%`,
                          width: `${calculateWidth(project.startDate, project.endDate)}%`,
                        }}
                        title={`${project.name}: ${project.progress}% complete`}
                      >
                        <div className="h-full bg-[var(--accent)] rounded" style={{ width: `${project.progress}%` }} />
                      </div>
                    </div>
                  </div>
                </div>
              ))}

              {/* Milestones */}
              {milestones.map((milestone) => (
                <div key={milestone.id} className="p-4 bg-[var(--bg-primary)]">
                  <div className="flex items-center gap-4">
                    <div className="w-48 flex-shrink-0">
                      <div className="font-medium text-[var(--text-primary)] text-sm flex items-center gap-2">
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                          <path d="M10 2l2 6h6l-5 4 2 6-5-4-5 4 2-6-5-4h6l2-6z" />
                        </svg>
                        {milestone.name}
                      </div>
                      <div className="text-xs text-[var(--text-secondary)]">
                        {new Date(milestone.date).toLocaleDateString()}
                      </div>
                    </div>
                    <div className="flex-1 relative h-8">
                      <div
                        className="absolute top-1/2 -translate-y-1/2 -translate-x-1/2 w-3 h-3 rounded-full bg-yellow-400 border-2 border-[var(--bg-secondary)]"
                        style={{ left: `${calculatePosition(milestone.date)}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
