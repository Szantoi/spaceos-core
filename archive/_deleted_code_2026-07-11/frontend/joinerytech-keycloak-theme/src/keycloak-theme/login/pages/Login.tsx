import type { KcContext } from 'keycloakify/login/KcContext'

interface Props {
  kcContext: Extract<KcContext, { pageId: 'login.ftl' }>
}

export function Login({ kcContext }: Props) {
  const { url, realm, auth, login, messagesPerField } = kcContext
  const hasError = messagesPerField.existsError('username', 'password')

  return (
    <div className="min-h-screen flex bg-stone-50" style={{ fontFamily: 'system-ui, sans-serif' }}>

      {/* LEFT — brand panel */}
      <aside
        className="hidden lg:flex flex-col"
        style={{ width: '44%', position: 'relative', overflow: 'hidden', background: '#1c1917', color: '#f5f5f4' }}
      >
        <div style={{
          position: 'absolute', inset: 0, opacity: 0.18,
          backgroundImage: 'repeating-linear-gradient(115deg, transparent 0 22px, rgba(255,255,255,0.07) 22px 23px)',
        }} />
        <div style={{
          position: 'absolute', inset: 0,
          background: 'radial-gradient(900px 600px at 80% 10%, rgba(13,148,136,0.35), transparent 60%), radial-gradient(700px 500px at 10% 90%, rgba(180,83,9,0.25), transparent 60%)',
        }} />

        <div style={{ position: 'relative', padding: '40px 48px 0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{ width: 36, height: 36, borderRadius: 8, background: '#0d9488', display: 'grid', placeItems: 'center' }}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>
              </svg>
            </div>
            <div style={{ fontSize: 16, fontWeight: 600, letterSpacing: '-0.02em' }}>
              joinery<span style={{ color: '#2dd4bf' }}>/</span>tech
            </div>
          </div>
        </div>

        <div style={{ position: 'relative', flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center', padding: '48px 48px 48px' }}>
          <div style={{ fontSize: 11, textTransform: 'uppercase', letterSpacing: '0.22em', color: 'rgba(94,234,212,0.8)', marginBottom: 16 }}>
            Bútor- és nyílászárógyártóknak
          </div>
          <h1 style={{ fontSize: 44, fontWeight: 600, lineHeight: 1.05, letterSpacing: '-0.02em', margin: 0 }}>
            Egy rendszer<br />
            a tervezéstől<br />
            <span style={{ color: '#2dd4bf' }}>a kiszállításig.</span>
          </h1>
          <p style={{ fontSize: 14, color: 'rgba(231,229,228,0.9)', marginTop: 20, maxWidth: 380, lineHeight: 1.65 }}>
            Parametrikus sablonok, automatikus szabászat, élő gépterhelés és raktárkészlet —
            egy helyen, magyar nyelven.
          </p>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, marginTop: 40, maxWidth: 360 }}>
            {[
              { v: '− 31%', l: 'anyaghulladék' },
              { v: '+ 18%', l: 'kapacitás' },
              { v: '4.6 ★', l: 'felhasználói' },
            ].map((s) => (
              <div key={s.l} style={{ borderLeft: '2px solid rgba(45,212,191,0.6)', paddingLeft: 12 }}>
                <div style={{ fontSize: 20, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>{s.v}</div>
                <div style={{ fontSize: 10.5, color: '#a8a29e', marginTop: 2 }}>{s.l}</div>
              </div>
            ))}
          </div>
        </div>

        <div style={{ position: 'relative', padding: '0 48px 40px', display: 'flex', justifyContent: 'space-between', fontSize: 11, color: '#78716c' }}>
          <div style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
            <span>v4.2.1</span>
            <span style={{ width: 4, height: 4, borderRadius: '50%', background: '#44403c', display: 'inline-block' }} />
            <span>Magyarország</span>
            <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
              <span style={{ width: 6, height: 6, borderRadius: '50%', background: '#34d399', display: 'inline-block' }} />
              Minden rendszer üzemel
            </span>
          </div>
        </div>
      </aside>

      {/* RIGHT — auth form */}
      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* mobile brand */}
        <div style={{ display: 'flex', alignItems: 'center', padding: '16px 20px', borderBottom: '1px solid #e7e5e4', background: 'white', gap: 8 }}
          className="lg:hidden">
          <div style={{ width: 32, height: 32, borderRadius: 8, background: '#0d9488', display: 'grid', placeItems: 'center' }}>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>
            </svg>
          </div>
          <div style={{ fontSize: 14, fontWeight: 600, letterSpacing: '-0.02em' }}>
            joinery<span style={{ color: '#0d9488' }}>/</span>tech
          </div>
        </div>

        <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '40px 20px' }}>
          <div style={{ width: '100%', maxWidth: 400 }}>
            <div style={{ marginBottom: 28 }}>
              <h2 style={{ fontSize: 26, fontWeight: 600, letterSpacing: '-0.02em', color: '#1c1917', margin: 0 }}>Üdv újra!</h2>
              <p style={{ fontSize: 13, color: '#78716c', marginTop: 4 }}>
                Lépj be a JoineryTech portálra a folytatáshoz.
              </p>
            </div>

            {hasError && (
              <div style={{ marginBottom: 16, padding: 12, borderRadius: 8, background: '#fff1f2', border: '1px solid #fecdd3', fontSize: 12.5, color: '#be123c' }}>
                Hibás felhasználónév vagy jelszó.
              </div>
            )}

            <form action={url.loginAction} method="post">
              {/* Username */}
              <label style={{ display: 'block', marginBottom: 12 }}>
                <span style={{ fontSize: 11, fontWeight: 500, color: '#44403c' }}>
                  {realm.loginWithEmailAllowed ? 'Email vagy felhasználónév' : 'Felhasználónév'}
                </span>
                <input
                  id="username"
                  name="username"
                  type="text"
                  defaultValue={auth.showUsername ? (login.username ?? '') : ''}
                  autoComplete="username"
                  autoFocus
                  style={{
                    display: 'block', marginTop: 4, width: '100%', height: 40,
                    padding: '0 12px', borderRadius: 8, border: '1px solid #e7e5e4',
                    background: 'white', fontSize: 13, outline: 'none', boxSizing: 'border-box',
                  }}
                />
              </label>

              {/* Password */}
              <label style={{ display: 'block', marginBottom: 16 }}>
                <span style={{ fontSize: 11, fontWeight: 500, color: '#44403c' }}>Jelszó</span>
                <input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  style={{
                    display: 'block', marginTop: 4, width: '100%', height: 40,
                    padding: '0 12px', borderRadius: 8, border: '1px solid #e7e5e4',
                    background: 'white', fontSize: 13, outline: 'none', boxSizing: 'border-box',
                  }}
                />
              </label>

              {/* Remember me */}
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 20, fontSize: 12, color: '#57534e', cursor: 'pointer', userSelect: 'none' }}>
                <input type="checkbox" name="rememberMe" style={{ width: 16, height: 16 }} />
                Emlékezz rám 30 napig
              </label>

              <button
                type="submit"
                name="login"
                style={{
                  width: '100%', height: 44, borderRadius: 8,
                  background: '#0d9488', border: 'none', color: 'white',
                  fontSize: 13.5, fontWeight: 600, cursor: 'pointer',
                  display: 'inline-flex', alignItems: 'center', justifyContent: 'center', gap: 8,
                  transition: 'background 0.15s',
                }}
                onMouseOver={(e) => (e.currentTarget.style.background = '#0f766e')}
                onMouseOut={(e) => (e.currentTarget.style.background = '#0d9488')}
              >
                Bejelentkezés
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M9 18l6-6-6-6"/>
                </svg>
              </button>

              {realm.resetPasswordAllowed && (
                <div style={{ marginTop: 16, textAlign: 'center' }}>
                  <a href={url.loginResetCredentialsUrl} style={{ fontSize: 12, color: '#0f766e', textDecoration: 'none' }}>
                    Elfelejtetted a jelszavad?
                  </a>
                </div>
              )}

              {realm.registrationAllowed && (
                <div style={{ marginTop: 24, paddingTop: 20, borderTop: '1px solid #f5f5f4', textAlign: 'center', fontSize: 12, color: '#78716c' }}>
                  Még nincs fiókod?{' '}
                  <a href={url.registrationUrl} style={{ color: '#0f766e', fontWeight: 500, textDecoration: 'none' }}>
                    Regisztráció
                  </a>
                </div>
              )}

              <div style={{ marginTop: 20, padding: 12, borderRadius: 8, background: '#fafaf9', border: '1px solid #e7e5e4', fontSize: 11, color: '#78716c', lineHeight: 1.6 }}>
                Üzem (Shop Floor) belépéshez a portál Home oldalán az „Üzem" csempét használd.
              </div>
            </form>
          </div>
        </div>

        <footer style={{ padding: '16px 20px', borderTop: '1px solid #e7e5e4', fontSize: 11, color: '#a8a29e', display: 'flex', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8 }}>
          <span>© 2026 JoineryTech Kft. · Minden jog fenntartva</span>
          <div style={{ display: 'flex', gap: 16 }}>
            <span style={{ cursor: 'pointer' }}>Súgó</span>
            <span style={{ cursor: 'pointer' }}>Kapcsolat</span>
          </div>
        </footer>
      </main>
    </div>
  )
}
