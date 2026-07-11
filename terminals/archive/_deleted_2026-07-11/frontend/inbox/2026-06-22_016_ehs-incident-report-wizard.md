---
id: MSG-FRONTEND-016
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_1620_consensus.md
created: 2026-06-22
content_hash: 83eb48b74f009a2425c9552f2dd9e05ffdeab037144f56489c23d3bc06adb392
---

# EHS Incident Report Wizard — Sprint 1 Frontend

## Context

**Planning Consensus:** 2026-06-22_1620 (EHS Baleset-Bejelentés prioritás #1)

SpaceOS első compliance-kritikus feature: munkahelyi balesetek/near-miss incidensek jelentése mobil eszközökről. Offline-first, photo upload, 3-step wizard, mobile takeover UX.

**Backend dependency:** MSG-BACKEND-024 (parallel development OK mock API-val)

**Üzleti érték:**
- Gyors incidens jelentés (30 sec alatt)
- Offline működés (gyárudvar, nincs WiFi)
- Photo evidence (automatikus EXIF strip backend-en)

## Component Spec

### `<IncidentReportWizard />`

**Location:** `src/components/EHS/IncidentReportWizard.tsx`

**Props:**
```tsx
interface IncidentReportWizardProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: (eventId: string) => void;
}
```

**3-step wizard flow:**
1. **Step 1: Incident Type** — Radio buttons (near-miss, injury, property)
2. **Step 2: Details** — Location dropdown, timestamp, description textarea, photo upload
3. **Step 3: Review** — Summary + Submit button

**State management:** Zustand `incidentDraftStore` + localForage persistence

### State Store: `incidentDraftStore`

**Location:** `src/stores/incidentDraftStore.ts`

```tsx
import create from 'zustand';
import { persist } from 'zustand/middleware';

interface IncidentDraft {
  id: string; // UUID v4 (idempotency)
  incidentType: 'near-miss' | 'injury' | 'property' | null;
  locationId: string | null;
  timestamp: string; // ISO8601
  description: string;
  photoFile: File | null;
  photoS3Key: string | null;
  step: 1 | 2 | 3;
  status: 'draft' | 'uploading' | 'submitted' | 'failed';
  retryCount: number;
}

interface IncidentDraftStore {
  drafts: IncidentDraft[];
  currentDraft: IncidentDraft | null;
  startNewDraft: () => void;
  updateDraft: (updates: Partial<IncidentDraft>) => void;
  submitDraft: () => Promise<void>;
  retryFailed: (draftId: string) => Promise<void>;
  deleteDraft: (draftId: string) => void;
}

export const useIncidentDraftStore = create<IncidentDraftStore>(
  persist(
    (set, get) => ({
      drafts: [],
      currentDraft: null,
      startNewDraft: () => {
        const draft: IncidentDraft = {
          id: crypto.randomUUID(),
          incidentType: null,
          locationId: null,
          timestamp: new Date().toISOString(),
          description: '',
          photoFile: null,
          photoS3Key: null,
          step: 1,
          status: 'draft',
          retryCount: 0
        };
        set({ currentDraft: draft, drafts: [...get().drafts, draft] });
      },
      updateDraft: (updates) => {
        const current = get().currentDraft;
        if (!current) return;
        const updated = { ...current, ...updates };
        set({
          currentDraft: updated,
          drafts: get().drafts.map(d => d.id === current.id ? updated : d)
        });
      },
      submitDraft: async () => {
        const draft = get().currentDraft;
        if (!draft) return;

        set({ currentDraft: { ...draft, status: 'uploading' } });

        try {
          // 1. Upload photo if exists
          if (draft.photoFile) {
            const presignedUrl = await getPresignedUrl(draft.photoFile);
            await uploadToS3(presignedUrl, draft.photoFile);
            set({ currentDraft: { ...draft, photoS3Key: presignedUrl.s3Key } });
          }

          // 2. Submit event
          const response = await fetch('/api/ehs/events', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              type: 'INCIDENT_REPORTED',
              payload: {
                reporterId: getUserId(), // from auth context
                incidentType: draft.incidentType,
                locationId: draft.locationId,
                timestamp: draft.timestamp,
                photoS3Key: draft.photoS3Key,
                description: draft.description
              },
              meta: {
                deviceId: getDeviceId(),
                clientTimestamp: new Date().toISOString()
              }
            })
          });

          if (!response.ok) throw new Error('Submit failed');

          const result = await response.json();

          set({
            currentDraft: { ...draft, status: 'submitted' },
            drafts: get().drafts.filter(d => d.id !== draft.id)
          });

          onSuccess?.(result.eventId);
        } catch (error) {
          set({
            currentDraft: { ...draft, status: 'failed', retryCount: draft.retryCount + 1 }
          });
        }
      },
      retryFailed: async (draftId) => {
        const draft = get().drafts.find(d => d.id === draftId);
        if (!draft) return;
        set({ currentDraft: draft });
        await get().submitDraft();
      },
      deleteDraft: (draftId) => {
        set({ drafts: get().drafts.filter(d => d.id !== draftId) });
      }
    }),
    {
      name: 'incident-drafts',
      storage: localForage // NOT localStorage (5MB limit too small for photos)
    }
  )
);
```

## Photo Upload Integration

### Client-side photo compression

**Location:** `src/utils/imageCompression.ts`

```tsx
import imageCompression from 'browser-image-compression';

export async function compressPhoto(file: File): Promise<File> {
  const options = {
    maxWidthOrHeight: 800,
    useWebWorker: true,
    fileType: 'image/jpeg'
  };

  try {
    return await imageCompression(file, options);
  } catch (error) {
    console.error('Compression failed:', error);
    return file; // fallback to original
  }
}
```

### S3 Presigned URL flow

**Location:** `src/services/ehsPhotoService.ts`

```tsx
export async function getPresignedUrl(file: File) {
  const response = await fetch('/api/ehs/photos/presigned-url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      filename: file.name,
      size: file.size,
      mime: file.type
    })
  });

  if (!response.ok) throw new Error('Failed to get presigned URL');
  return await response.json(); // { uploadUrl, s3Key, expiresAt }
}

export async function uploadToS3(presigned: any, file: File) {
  const compressed = await compressPhoto(file);

  const response = await fetch(presigned.uploadUrl, {
    method: 'PUT',
    body: compressed,
    headers: { 'Content-Type': file.type }
  });

  if (!response.ok) throw new Error('S3 upload failed');
}
```

## Mobile UX Details

### FAB (Floating Action Button)

**Location:** Bottom-right corner (fixed position)

```tsx
<Fab
  color="error"
  aria-label="report incident"
  sx={{
    position: 'fixed',
    bottom: { xs: 72, md: 24 }, // iOS safe-area-inset
    right: 24,
    zIndex: 1000
  }}
  onClick={() => {
    startNewDraft();
    setIsWizardOpen(true);
  }}
>
  <WarningIcon />
</Fab>
```

### Fullscreen Mobile Takeover

**Wizard modal:** Desktop = centered modal (600px wide), Mobile = fullscreen

```tsx
<Dialog
  open={isOpen}
  onClose={onClose}
  fullScreen={isMobile}
  PaperProps={{
    sx: {
      minHeight: isMobile ? '100vh' : '600px',
      ...(isMobile && {
        margin: 0,
        maxHeight: '100vh',
        borderRadius: 0
      })
    }
  }}
>
  <DialogTitle>
    {isMobile && <IconButton onClick={onClose}><CloseIcon /></IconButton>}
    Report Incident — Step {currentDraft?.step}/3
  </DialogTitle>

  <DialogContent>
    {currentDraft?.step === 1 && <StepIncidentType />}
    {currentDraft?.step === 2 && <StepDetails />}
    {currentDraft?.step === 3 && <StepReview />}
  </DialogContent>

  <DialogActions>
    {currentDraft?.step > 1 && <Button onClick={goBack}>Back</Button>}
    <Button
      variant="contained"
      onClick={currentDraft?.step === 3 ? submitDraft : goNext}
      disabled={!isStepValid(currentDraft?.step)}
    >
      {currentDraft?.step === 3 ? 'Submit' : 'Next'}
    </Button>
  </DialogActions>
</Dialog>
```

## Offline Queue & Retry Service

**Background retry:** 5 min polling (NOT Background Sync API, iOS Safari nem támogatja)

**Location:** `src/services/offlineRetryService.ts`

```tsx
export function startRetryService() {
  setInterval(async () => {
    const drafts = useIncidentDraftStore.getState().drafts;
    const failedDrafts = drafts.filter(d => d.status === 'failed' && d.retryCount < 3);

    for (const draft of failedDrafts) {
      try {
        await useIncidentDraftStore.getState().retryFailed(draft.id);
      } catch {
        // silent fail, retry next cycle
      }
    }
  }, 5 * 60 * 1000); // 5 min
}
```

**Start service:** `src/App.tsx` root component

```tsx
useEffect(() => {
  startRetryService();
}, []);
```

## Testing Requirements

### Unit Tests (Jest + React Testing Library)

- ✅ IncidentReportWizard renders (3 steps)
- ✅ Step navigation (Next/Back buttons)
- ✅ Form validation (incident type required, location required)
- ✅ Photo upload (file input, compression)
- ✅ Zustand store (startNewDraft, updateDraft, submitDraft)
- ✅ localForage persistence (draft saved after page refresh)

### Integration Tests (Mock API)

- ✅ Full wizard flow (Step 1 → 2 → 3 → Submit)
- ✅ Photo presigned URL fetch + S3 upload mock
- ✅ Offline draft saved (localStorage)
- ✅ Retry failed draft (3 attempts max)

### E2E Tests (Playwright - optional Sprint 1)

- ✅ Mobile viewport (360x640) fullscreen modal
- ✅ Photo capture (Camera API mock)
- ✅ Offline → online sync (Network throttling)

## Mock API (Development)

**MSW (Mock Service Worker) handler:**

```tsx
// src/mocks/handlers/ehsHandlers.ts
import { rest } from 'msw';

export const ehsHandlers = [
  rest.post('/api/ehs/photos/presigned-url', (req, res, ctx) => {
    return res(
      ctx.status(200),
      ctx.json({
        uploadUrl: 'https://mock-s3.amazonaws.com/upload',
        s3Key: `ehs/photos/${crypto.randomUUID()}.jpg`,
        expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString()
      })
    );
  }),

  rest.put('https://mock-s3.amazonaws.com/upload', (req, res, ctx) => {
    return res(ctx.status(200));
  }),

  rest.post('/api/ehs/events', async (req, res, ctx) => {
    const body = await req.json();
    return res(
      ctx.status(201),
      ctx.json({
        eventId: crypto.randomUUID(),
        sequence: 42,
        status: 'accepted',
        serverTimestamp: new Date().toISOString()
      })
    );
  })
];
```

## Dependencies

- ✅ `browser-image-compression` — Photo compression
- ✅ `localforage` — Offline storage (>5MB photo support)
- ✅ `zustand` — State management
- ✅ `@mui/material` — UI components (Dialog, FAB, Stepper)
- ✅ `msw` — Mock API (development)

## Open Questions (Conductor válasz kell)

1. **Camera API:** Capacitor Camera plugin vagy native `<input type="file" capture="environment">`? (Desktop vs. Mobile decision)
2. **Retry UI:** Failed drafts badge count megjelenítése FAB-on? (pl. ⚠️ 2 unsent)
3. **Location dropdown:** Statikus lista vagy API endpoint? (Backend MSG-024 scope?)
4. **User notification:** Toast notification "Incident submitted" vagy modal success screen?

## Definition of Done

- ✅ `<IncidentReportWizard />` komponens implementálva (3-step flow)
- ✅ `incidentDraftStore` Zustand store + localForage persistence
- ✅ Photo upload (compression + S3 presigned URL)
- ✅ Mobile fullscreen UX + FAB
- ✅ Offline retry service (5 min polling)
- ✅ Mock API (MSW handlers)
- ✅ Unit tests (8+) + Integration tests (4+)
- ✅ Responsive (mobile 360px + desktop 1280px)
- ✅ Accessibility (keyboard navigation, ARIA labels)
- ✅ Test coverage ≥85% (új kód)

## Estimated Effort

**Frontend:** 4-6 dev days (1 developer)
- Day 1: Wizard komponens + 3-step flow + Zustand store
- Day 2: Photo upload (compression + presigned URL)
- Day 3: Mobile UX (FAB, fullscreen modal, iOS safe-area)
- Day 4: Offline retry service + localForage persistence
- Day 5-6: Testing (unit + integration) + DoD checklist

## Next Steps (Sprint Planning)

**Sprint 1 scope:**
- Frontend wizard (MSG-016)
- Backend API (MSG-BACKEND-024) — parallel development OK
- Mock API integration

**Sprint 2 scope:**
- Backend integration (swap MSW → real API)
- E2E testing
- Camera API decision (Capacitor vs. native)

---

**Conductor note:** Frontend development can start immediately with Mock API. Backend MSG-024 parallel track. Integration Sprint 2.
