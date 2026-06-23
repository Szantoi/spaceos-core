import { useState, useEffect, useCallback } from 'react';

const AUTH_TOKEN_KEY = 'datahaven_token';

export function useAuth() {
  const [authToken, setAuthToken] = useState<string | null>(() => {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  });
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const logout = useCallback(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    setAuthToken(null);
    setIsAuthenticated(false);
  }, []);

  const verifyToken = useCallback(async (token: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch('/api/auth/verify', {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (res.ok) {
        setIsAuthenticated(true);
      } else {
        setIsAuthenticated(false);
        logout();
        setError('Invalid or expired token');
      }
    } catch {
      setIsAuthenticated(false);
      setError('Connection error');
    } finally {
      setIsLoading(false);
    }
  }, [logout]);

  // Verify token on mount
  useEffect(() => {
    if (authToken) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      verifyToken(authToken);
    }
  }, [authToken, verifyToken]);

  const login = async (token: string): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      const res = await fetch('/api/auth/verify', {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (res.ok) {
        localStorage.setItem(AUTH_TOKEN_KEY, token);
        setAuthToken(token);
        setIsAuthenticated(true);
        return true;
      } else {
        setError('Invalid token');
        return false;
      }
    } catch {
      setError('Connection error');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    authToken,
    isAuthenticated,
    isLoading,
    error,
    login,
    logout,
  };
}
