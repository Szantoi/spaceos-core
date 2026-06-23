import { useState } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './hooks/useAuth';
import { useSSE } from './hooks/useSSE';
import { IndustrialLayout } from './components/Industrial/IndustrialLayout';
import { AuthOverlay } from './components/Auth/AuthOverlay';
import { IndustrialDashboardPage } from './pages/IndustrialDashboardPage';
import { IndustrialKanbanPage } from './pages/IndustrialKanbanPage';
import { IndustrialPlanningPage } from './pages/IndustrialPlanningPage';
import { IndustrialProjectsPage } from './pages/IndustrialProjectsPage';
import { IndustrialAutonomousPage } from './pages/IndustrialAutonomousPage';
import { IndustrialFlowEditorPage } from './pages/IndustrialFlowEditorPage';

function App() {
  const { isAuthenticated, error: authError, login } = useAuth();
  const [showEmpty, setShowEmpty] = useState(false);

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
      <IndustrialLayout
        isConnected={isConnected}
        showEmpty={showEmpty}
        onToggleEmpty={() => setShowEmpty(!showEmpty)}
      >
        <AuthOverlay
          isVisible={!isAuthenticated}
          onLogin={handleLogin}
          error={authError}
        />
        <Routes>
          <Route path="/" element={<IndustrialDashboardPage />} />
          <Route path="/autonomous" element={<IndustrialAutonomousPage />} />
          <Route path="/kanban" element={<IndustrialKanbanPage />} />
          <Route path="/planning" element={<IndustrialPlanningPage />} />
          <Route path="/projects" element={<IndustrialProjectsPage />} />
          <Route path="/flow" element={<IndustrialFlowEditorPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </IndustrialLayout>
    </Router>
  );
}

export default App;
