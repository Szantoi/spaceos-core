---
id: MSG-FRONTEND-020
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_C_ShopFloor_Integration_v1.md
created: 2026-06-23
content_hash: 0be04af4126464a65f8fdb74d1576dbc3264f1993bc028fb1b7d7d43ed20f0a9
---

# Q3 Track C — ShopFloor Kiosk UI (Frontend)

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 0.5 day
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Build a shop floor kiosk UI for operators to view assigned cutting jobs, start jobs, mark jobs complete or failed, and track progress in real-time.

**Prerequisites:** MSG-BACKEND-032 DONE (ShopFloor API ready)

**Track C Frontend adds:**
1. Kiosk Login (PIN-based authentication)
2. Machine Queue View (assigned jobs list)
3. Job Detail View (cutting list, start/complete buttons)
4. Real-time updates (polling, 5s interval)

---

## Acceptance Criteria

- [ ] **Kiosk Login** (`/shopfloor/login` route)
  - PIN input (4 digits)
  - Machine selector dropdown
  - Login → POST /api/auth/kiosk/login
  - Store session (localStorage or sessionStorage)
- [ ] **Machine Queue View** (`/shopfloor/queue` route)
  - Fetch queue for selected machine
  - Display jobs sorted by priority
  - Job card: priority, panel count, estimated duration, status
  - Tap job → navigate to Job Detail View
  - Auto-refresh every 5 seconds (polling)
- [ ] **Job Detail View** (`/shopfloor/jobs/:jobId` route)
  - Display cutting list (panels, dimensions)
  - Progress bar (if job in progress)
  - Action buttons: "Start Job", "Complete Job", "Report Issue"
  - Success/error toast notifications
- [ ] **Integration Tests**
  - Kiosk login flow (valid PIN, invalid PIN)
  - Queue view fetch and display (mock API)
  - Start job flow (mock API)
  - Complete job flow (mock API)

---

## Technical Implementation

### 1. Kiosk Login

**File:** `src/pages/ShopFloor/KioskLogin.tsx`

```tsx
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

export const KioskLogin = () => {
  const [pin, setPin] = useState('');
  const [machineId, setMachineId] = useState('');
  const { loginKiosk, isLoading, error } = useAuth();
  const navigate = useNavigate();

  const machines = ['HOLZMA-01', 'HOLZMA-02', 'ALTENDORF-01'];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const success = await loginKiosk(pin, machineId);

    if (success) {
      navigate('/shopfloor/queue');
    }
  };

  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center">
      <div className="bg-white rounded-lg shadow-xl p-8 max-w-md w-full">
        <h1 className="text-2xl font-bold mb-6 text-center">ShopFloor Kiosk</h1>

        <form onSubmit={handleSubmit}>
          {/* Machine Selector */}
          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Select Machine</label>
            <select
              value={machineId}
              onChange={(e) => setMachineId(e.target.value)}
              className="w-full border rounded px-3 py-2 text-lg"
              required
            >
              <option value="">Choose machine...</option>
              {machines.map((machine) => (
                <option key={machine} value={machine}>
                  {machine}
                </option>
              ))}
            </select>
          </div>

          {/* PIN Input */}
          <div className="mb-6">
            <label className="block text-sm font-medium mb-2">Operator PIN</label>
            <input
              type="password"
              value={pin}
              onChange={(e) => setPin(e.target.value.slice(0, 4))}
              className="w-full border rounded px-3 py-2 text-center text-2xl tracking-widest"
              placeholder="••••"
              maxLength={4}
              pattern="\d{4}"
              required
            />
          </div>

          {/* Error */}
          {error && (
            <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded text-red-700 text-sm">
              {error}
            </div>
          )}

          {/* Login Button */}
          <button
            type="submit"
            disabled={isLoading || pin.length !== 4}
            className="w-full bg-blue-600 text-white py-3 rounded text-lg font-medium hover:bg-blue-700 disabled:opacity-50"
          >
            {isLoading ? 'Logging in...' : 'Login'}
          </button>
        </form>
      </div>
    </div>
  );
};
```

### 2. Machine Queue View

**File:** `src/pages/ShopFloor/MachineQueueView.tsx`

```tsx
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMachineQueue } from '../../hooks/useMachineQueue';
import { JobCard } from '../../components/ShopFloor/JobCard';

export const MachineQueueView = () => {
  const machineId = localStorage.getItem('shopfloor_machine_id') || '';
  const { queue, fetchQueue, isLoading, error } = useMachineQueue();
  const navigate = useNavigate();

  useEffect(() => {
    // Fetch queue on mount
    fetchQueue(machineId);

    // Auto-refresh every 5 seconds
    const interval = setInterval(() => {
      fetchQueue(machineId);
    }, 5000);

    return () => clearInterval(interval);
  }, [machineId]);

  if (isLoading && !queue) {
    return <div className="text-center py-20 text-white">Loading queue...</div>;
  }

  if (error) {
    return (
      <div className="max-w-4xl mx-auto p-6">
        <div className="p-4 bg-red-100 border border-red-300 rounded text-red-700">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-900 p-6">
      {/* Header */}
      <div className="mb-6 flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-white">{machineId}</h1>
          <p className="text-gray-400">Machine Queue ({queue?.queuedJobs?.length || 0} jobs)</p>
        </div>
        <button
          onClick={() => fetchQueue(machineId)}
          className="bg-gray-700 text-white px-4 py-2 rounded hover:bg-gray-600"
        >
          Refresh
        </button>
      </div>

      {/* Job List */}
      <div className="space-y-4">
        {queue?.queuedJobs?.length === 0 && (
          <div className="bg-gray-800 p-8 rounded-lg text-center text-gray-400">
            No jobs in queue
          </div>
        )}

        {queue?.queuedJobs?.map((job: any) => (
          <JobCard
            key={job.jobId}
            job={job}
            onClick={() => navigate(`/shopfloor/jobs/${job.jobId}`)}
          />
        ))}
      </div>
    </div>
  );
};
```

### 3. Job Card Component

**File:** `src/components/ShopFloor/JobCard.tsx`

```tsx
interface Props {
  job: any;
  onClick: () => void;
}

export const JobCard = ({ job, onClick }: Props) => {
  const statusColors = {
    Queued: 'bg-yellow-900 text-yellow-200',
    Assigned: 'bg-blue-900 text-blue-200',
    InProgress: 'bg-green-900 text-green-200',
    Completed: 'bg-gray-700 text-gray-300',
    Failed: 'bg-red-900 text-red-200'
  };

  return (
    <div
      onClick={onClick}
      className="bg-gray-800 p-6 rounded-lg cursor-pointer hover:bg-gray-700 transition"
    >
      <div className="flex justify-between items-start mb-4">
        <div>
          <span
            className={`px-3 py-1 rounded text-sm font-medium ${
              statusColors[job.status as keyof typeof statusColors]
            }`}
          >
            {job.status}
          </span>
          <p className="text-white text-lg font-medium mt-2">
            Priority: {job.priority}
          </p>
        </div>
        <p className="text-gray-400 text-sm">
          Position #{job.queuePosition}
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="text-gray-400">Panel Count</p>
          <p className="text-white font-medium">{job.panelCount} pieces</p>
        </div>
        <div>
          <p className="text-gray-400">Estimated Duration</p>
          <p className="text-white font-medium">{job.estimatedDuration}</p>
        </div>
      </div>
    </div>
  );
};
```

### 4. Job Detail View

**File:** `src/pages/ShopFloor/JobDetailView.tsx`

```tsx
import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMachineQueue } from '../../hooks/useMachineQueue';

export const JobDetailView = () => {
  const { jobId } = useParams<{ jobId: string }>();
  const { startJob, completeJob, failJob, isLoading } = useMachineQueue();
  const navigate = useNavigate();
  const [showFailDialog, setShowFailDialog] = useState(false);
  const [failureReason, setFailureReason] = useState('');

  // Mock job data (in real impl, fetch from API)
  const job = {
    jobId,
    status: 'Assigned',
    panelCount: 25,
    cuttingList: [
      { width: 2500, height: 1250, material: 'MDF', quantity: 10 },
      { width: 1800, height: 900, material: 'MDF', quantity: 15 }
    ]
  };

  const handleStart = async () => {
    const operatorId = localStorage.getItem('shopfloor_operator_id') || '';
    const machineId = localStorage.getItem('shopfloor_machine_id') || '';

    await startJob(jobId!, operatorId, machineId);
    // Navigate back to queue
    navigate('/shopfloor/queue');
  };

  const handleComplete = async () => {
    const operatorId = localStorage.getItem('shopfloor_operator_id') || '';

    await completeJob(jobId!, operatorId, job.panelCount, 0);
    navigate('/shopfloor/queue');
  };

  const handleFail = async () => {
    const operatorId = localStorage.getItem('shopfloor_operator_id') || '';

    await failJob(jobId!, operatorId, failureReason);
    setShowFailDialog(false);
    navigate('/shopfloor/queue');
  };

  return (
    <div className="min-h-screen bg-gray-900 p-6">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <button
            onClick={() => navigate('/shopfloor/queue')}
            className="text-gray-400 hover:text-white mb-4"
          >
            ← Back to Queue
          </button>
          <h1 className="text-3xl font-bold text-white">Job #{jobId?.slice(0, 8)}</h1>
          <p className="text-gray-400">Status: {job.status}</p>
        </div>

        {/* Cutting List */}
        <div className="bg-gray-800 p-6 rounded-lg mb-6">
          <h2 className="text-xl font-medium text-white mb-4">Cutting List</h2>
          <table className="w-full text-white">
            <thead className="text-left text-gray-400 text-sm">
              <tr>
                <th className="pb-2">Width (mm)</th>
                <th className="pb-2">Height (mm)</th>
                <th className="pb-2">Material</th>
                <th className="pb-2">Quantity</th>
              </tr>
            </thead>
            <tbody>
              {job.cuttingList.map((panel, i) => (
                <tr key={i} className="border-t border-gray-700">
                  <td className="py-2">{panel.width}</td>
                  <td className="py-2">{panel.height}</td>
                  <td className="py-2">{panel.material}</td>
                  <td className="py-2">{panel.quantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Action Buttons */}
        <div className="space-y-3">
          {job.status === 'Assigned' && (
            <button
              onClick={handleStart}
              disabled={isLoading}
              className="w-full bg-green-600 text-white py-4 rounded text-lg font-medium hover:bg-green-700 disabled:opacity-50"
            >
              Start Job
            </button>
          )}

          {job.status === 'InProgress' && (
            <>
              <button
                onClick={handleComplete}
                disabled={isLoading}
                className="w-full bg-blue-600 text-white py-4 rounded text-lg font-medium hover:bg-blue-700 disabled:opacity-50"
              >
                Complete Job
              </button>

              <button
                onClick={() => setShowFailDialog(true)}
                className="w-full bg-red-600 text-white py-4 rounded text-lg font-medium hover:bg-red-700"
              >
                Report Issue
              </button>
            </>
          )}
        </div>

        {/* Failure Dialog */}
        {showFailDialog && (
          <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center z-50">
            <div className="bg-gray-800 p-6 rounded-lg max-w-md w-full">
              <h2 className="text-xl font-bold text-white mb-4">Report Issue</h2>

              <textarea
                value={failureReason}
                onChange={(e) => setFailureReason(e.target.value)}
                className="w-full border rounded px-3 py-2 mb-4"
                rows={4}
                placeholder="Describe the issue (e.g., machine malfunction, incorrect dimensions)"
              />

              <div className="flex space-x-3">
                <button
                  onClick={handleFail}
                  className="flex-1 bg-red-600 text-white py-2 rounded hover:bg-red-700"
                >
                  Submit
                </button>
                <button
                  onClick={() => setShowFailDialog(false)}
                  className="flex-1 border border-gray-600 text-white py-2 rounded hover:bg-gray-700"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
```

### 5. Custom Hook (API Integration)

**File:** `src/hooks/useMachineQueue.ts`

```ts
import { useState } from 'react';

export const useMachineQueue = () => {
  const [queue, setQueue] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchQueue = async (machineId: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(`/api/cutting/shopfloor/queue?machineId=${machineId}`);

      if (!response.ok) {
        throw new Error('Failed to fetch queue');
      }

      const data = await response.json();
      setQueue(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  };

  const startJob = async (jobId: string, operatorId: string, machineId: string) => {
    try {
      const response = await fetch(`/api/cutting/shopfloor/jobs/${jobId}/start`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ operatorId, machineId })
      });

      if (!response.ok) {
        throw new Error('Failed to start job');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  const completeJob = async (jobId: string, operatorId: string, actualPanelCount: number, offcutCount: number) => {
    try {
      const response = await fetch(`/api/cutting/shopfloor/jobs/${jobId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ operatorId, actualPanelCount, offcutCount })
      });

      if (!response.ok) {
        throw new Error('Failed to complete job');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  const failJob = async (jobId: string, operatorId: string, failureReason: string) => {
    try {
      const response = await fetch(`/api/cutting/shopfloor/jobs/${jobId}/fail`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ operatorId, failureReason })
      });

      if (!response.ok) {
        throw new Error('Failed to mark job as failed');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  return { queue, fetchQueue, startJob, completeJob, failJob, isLoading, error };
};
```

### 6. Routing

**File:** `src/App.tsx`

```tsx
// Add routes
<Route path="/shopfloor/login" element={<KioskLogin />} />
<Route path="/shopfloor/queue" element={<MachineQueueView />} />
<Route path="/shopfloor/jobs/:jobId" element={<JobDetailView />} />
```

---

## Files to Create

1. `src/pages/ShopFloor/KioskLogin.tsx`
2. `src/pages/ShopFloor/MachineQueueView.tsx`
3. `src/pages/ShopFloor/JobDetailView.tsx`
4. `src/components/ShopFloor/JobCard.tsx`
5. `src/hooks/useMachineQueue.ts`
6. `src/pages/ShopFloor/MachineQueueView.test.tsx`

---

## Files to Modify

1. `src/App.tsx` (add `/shopfloor/*` routes)

---

## Testing Requirements

### Integration Tests

```tsx
describe('ShopFloor Kiosk', () => {
  it('logs in with valid PIN and machine', async () => {
    // Mock API
    // Enter PIN and machine
    // Submit
    // Assert navigation to /shopfloor/queue
  });

  it('fetches and displays queue', async () => {
    // Mock API
    // Render MachineQueueView
    // Assert job cards displayed
  });

  it('starts job', async () => {
    // Mock API
    // Render JobDetailView
    // Click "Start Job"
    // Assert API called
  });

  it('completes job', async () => {
    // Mock API (job status = InProgress)
    // Render JobDetailView
    // Click "Complete Job"
    // Assert API called
  });
});
```

---

## Build & Test Gate

```bash
cd /opt/spaceos/frontend/joinerytech-portal

# Build
npm run build

# Run tests
npm test -- --coverage --watchAll=false
```

**Expected:** All tests pass, TypeScript 0 errors.

---

**Estimated effort:** 0.5 day (4 hours)
**Model:** sonnet
**Priority:** HIGH
