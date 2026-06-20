import type { ReactNode } from 'react';
import { Header } from './Header';

interface LayoutProps {
  children: ReactNode;
  isConnected: boolean;
}

export function Layout({ children, isConnected }: LayoutProps) {
  return (
    <div className="min-h-screen bg-[var(--bg-primary)]">
      <Header isConnected={isConnected} />
      <main className="p-6">
        {children}
      </main>
    </div>
  );
}
