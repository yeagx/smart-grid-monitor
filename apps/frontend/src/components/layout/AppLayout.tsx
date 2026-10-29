import { type ReactNode, useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { cn } from '@/lib/utils';
import { Menu, X } from 'lucide-react';

interface AppLayoutProps {
  children: ReactNode;
}

const COLLAPSED_KEY = 'sgm.sidebarCollapsed';

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}

export function AppLayout({ children }: AppLayoutProps) {
  const [mobileOpen, setMobileOpen] = useState(false);
  // Collapsed state lives here so the content padding follows the sidebar width.
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const location = useLocation();

  // Close the mobile menu whenever the page changes.
  useEffect(() => setMobileOpen(false), [location.pathname]);

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      try {
        localStorage.setItem(COLLAPSED_KEY, String(next));
      } catch {
        /* storage unavailable — keep in memory only */
      }
      return next;
    });
  };

  return (
    <div className="min-h-screen bg-grid-bg">
      {/* Mobile hamburger */}
      <button
        className="fixed top-3 left-3 z-50 rounded-lg bg-grid-surface p-2 text-gray-400 lg:hidden border border-grid-border"
        onClick={() => setMobileOpen(!mobileOpen)}
        aria-label={mobileOpen ? 'Close menu' : 'Open menu'}
        type="button"
      >
        {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
      </button>

      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-20 bg-black/60 lg:hidden"
          onClick={() => setMobileOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar — hidden on mobile unless toggled; always expanded on mobile */}
      <div className={cn('lg:block', mobileOpen ? 'block' : 'hidden')}>
        <Sidebar
          collapsed={collapsed && !mobileOpen}
          onToggleCollapsed={toggleCollapsed}
          onNavigate={() => setMobileOpen(false)}
        />
      </div>

      {/* Main content — offset by the current sidebar width; top padding clears the mobile menu button */}
      <main className={cn('min-h-screen pt-12 lg:pt-0 transition-[padding] duration-200', collapsed ? 'lg:pl-16' : 'lg:pl-56')}>
        <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
          {children}
        </div>
      </main>
    </div>
  );
}
