import { Link, useLocation } from 'react-router-dom';

interface HeaderProps {
  isConnected: boolean;
}

export function Header({ isConnected }: HeaderProps) {
  const location = useLocation();

  const isActive = (path: string) => {
    return location.pathname === path;
  };

  return (
    <header className="bg-[var(--bg-secondary)] border-b border-[var(--border)] px-6 py-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-8">
          <h1 className="text-xl font-semibold text-[var(--text-primary)]">
            SpaceOS Kanban
          </h1>
          <nav className="flex gap-4">
            <Link
              to="/"
              className={`px-3 py-2 rounded transition ${
                isActive('/')
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              Dashboard
            </Link>
            <Link
              to="/kanban"
              className={`px-3 py-2 rounded transition ${
                isActive('/kanban')
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              Kanban
            </Link>
            <Link
              to="/planning"
              className={`px-3 py-2 rounded transition ${
                isActive('/planning')
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              Planning
            </Link>
            <Link
              to="/projects"
              className={`px-3 py-2 rounded transition ${
                isActive('/projects')
                  ? 'bg-[var(--accent)] text-white'
                  : 'text-[var(--text-secondary)] hover:text-[var(--text-primary)]'
              }`}
            >
              Projects
            </Link>
          </nav>
        </div>
        <div className="flex items-center gap-2">
          <div
            className={`w-2 h-2 rounded-full ${
              isConnected ? 'bg-[var(--accent-green)]' : 'bg-[var(--accent-red)]'
            }`}
          />
          <span className="text-sm text-[var(--text-secondary)]">
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
      </div>
    </header>
  );
}
