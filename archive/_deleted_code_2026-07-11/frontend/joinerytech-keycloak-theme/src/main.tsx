import { createRoot } from 'react-dom/client'
import { lazy, StrictMode, Suspense } from 'react'
import type { KcContext } from 'keycloakify/login/KcContext'

declare global {
  interface Window {
    kcContext?: KcContext
  }
}

const KcPage = lazy(() => import('./keycloak-theme/login/KcPage'))

// Minimal mock context for local dev preview
const mockKcContext: KcContext = {
  pageId: 'login.ftl',
  locale: { currentLanguageTag: 'hu', supported: [] },
  realm: {
    loginWithEmailAllowed: false,
    registrationAllowed: false,
    resetPasswordAllowed: true,
    password: true,
    internationalizationEnabled: false,
    name: 'spaceos',
    displayName: 'SpaceOS',
    displayNameHtml: 'SpaceOS',
    registrationEmailAsUsername: false,
    rememberMe: true,
    duplicationDetected: false,
  },
  url: {
    loginAction: '#',
    loginResetCredentialsUrl: '#',
    registrationUrl: '#',
    resourcesCommonPath: '',
    resourcesPath: '',
    ssoLoginInOtherTabsUrl: '#',
  },
  auth: { showUsername: false, showResetCredentials: false, showTryAnotherWayLink: false },
  social: { displayInfo: false, providers: [] },
  login: { username: '' },
  messagesPerField: {
    existsError: () => false,
    get: () => '',
    exists: () => false,
    printIfExists: () => undefined,
  },
  scripts: [],
  authenticationSession: undefined,
} as unknown as KcContext

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Suspense fallback={<div style={{ minHeight: '100vh', background: '#fafaf9' }} />}>
      <KcPage kcContext={window.kcContext ?? mockKcContext} />
    </Suspense>
  </StrictMode>
)
