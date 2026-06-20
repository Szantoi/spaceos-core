import { useState } from 'react';

interface AuthOverlayProps {
  isVisible: boolean;
  onLogin: (token: string) => Promise<boolean>;
  error: string | null;
}

export function AuthOverlay({ isVisible, onLogin, error }: AuthOverlayProps) {
  const [token, setToken] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  if (!isVisible) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token.trim()) return;

    setIsLoading(true);
    await onLogin(token.trim());
    setIsLoading(false);
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      handleSubmit(e);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-[var(--bg-secondary)] border border-[var(--border)] rounded-lg p-8 max-w-md w-full">
        <h2 className="text-2xl font-semibold text-[var(--text-primary)] mb-4">
          Authentication Required
        </h2>
        <form onSubmit={handleSubmit}>
          <input
            type="password"
            value={token}
            onChange={(e) => setToken(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="Enter token..."
            className="w-full px-4 py-2 bg-[var(--bg-tertiary)] border border-[var(--border)] rounded text-[var(--text-primary)] placeholder-[var(--text-muted)] focus:outline-none focus:border-[var(--accent)]"
            autoFocus
          />
          <button
            type="submit"
            disabled={isLoading || !token.trim()}
            className="w-full mt-4 px-4 py-2 bg-[var(--accent)] text-white rounded hover:bg-[var(--accent-hover)] disabled:opacity-50 disabled:cursor-not-allowed transition"
          >
            {isLoading ? 'Verifying...' : 'Login'}
          </button>
          {error && (
            <p className="mt-3 text-sm text-[var(--accent-red)]">{error}</p>
          )}
        </form>
      </div>
    </div>
  );
}
