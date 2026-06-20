import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import { useSSE } from './hooks/useSSE';
import { Layout } from './components/Layout/Layout';
import { AuthOverlay } from './components/Auth/AuthOverlay';
import { DashboardPage } from './pages/DashboardPage';
import { KanbanPage } from './pages/KanbanPage';
import { PlanningPage } from './pages/PlanningPage';
import { ProjectsPage } from './pages/ProjectsPage';

function App() {
  const { authToken, isAuthenticated, error: authError, login } = useAuth();

  const { isConnected, reconnect } = useSSE((updates) => {
    console.log('Board update received:', updates);
    // Updates will be handled by individual pages
  });

  const handleLogin = async (token: string): Promise<boolean> => {
    const success = await login(token);
    if (success) {
      reconnect();
    }
    return success;
  };

  return (
    <Router>
      <Layout isConnected={isConnected}>
        <AuthOverlay
          isVisible={!isAuthenticated}
          onLogin={handleLogin}
          error={authError}
        />
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route
            path="/kanban"
            element={
              <KanbanPage
                authToken={authToken}
                updateBoard={(updates) => console.log('Update board:', updates)}
              />
            }
          />
          <Route path="/planning" element={<PlanningPage />} />
          <Route path="/projects" element={<ProjectsPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </Router>
  );
}

export default App;
