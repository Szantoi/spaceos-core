import { useState, useEffect } from 'react';

const AUTH_TOKEN_KEY = 'datahaven_token';

export function useAuth() {
  const [authToken, setAuthToken] = useState<string | null>(() => {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  });
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Verify token on mount
  useEffect(() => {
    if (authToken) {
      verifyToken(authToken);
    }
  }, [authToken]);

  const verifyToken = async (token: string) => {
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
    } catch (err) {
      setIsAuthenticated(false);
      setError('Connection error');
    } finally {
      setIsLoading(false);
    }
  };

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
    } catch (err) {
      setError('Connection error');
      return false;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    setAuthToken(null);
    setIsAuthenticated(false);
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
