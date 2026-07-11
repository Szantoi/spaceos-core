import { Suspense, lazy } from 'react'
import type { KcContext } from 'keycloakify/login/KcContext'
import { useInitialize } from 'keycloakify/login/Template.useInitialize'

const Login = lazy(() => import('./pages/Login').then((m) => ({ default: m.Login })))

interface KcPageProps {
  kcContext: KcContext
}

export default function KcPage({ kcContext }: KcPageProps) {
  const { isReadyToRender } = useInitialize({ kcContext, doUseDefaultCss: false })

  if (!isReadyToRender) return null

  return (
    <Suspense>
      {kcContext.pageId === 'login.ftl' ? (
        <Login kcContext={kcContext as Extract<KcContext, { pageId: 'login.ftl' }>} />
      ) : (
        <div className="min-h-screen flex items-center justify-center bg-stone-50">
          <p className="text-stone-400 text-sm">Page: {kcContext.pageId}</p>
        </div>
      )}
    </Suspense>
  )
}
