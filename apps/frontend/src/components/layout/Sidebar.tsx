import { NavLink, useLocation } from 'react-router-dom';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  Map,
  Bell,
  Cpu,
  Target,
  BrainCircuit,
  Lock,
  ChevronLeft,
  ChevronRight,
  Zap,
} from 'lucide-react';

interface NavItem {
  label: string;
  path: string;
  icon: React.ElementType;
  /** Extra paths that should also highlight this item. */
  aliases?: string[];
  /** Closed feature: still clickable (shows the locked screen) but marked with a lock. */
  locked?: boolean;
}

const navigation: NavItem[] = [
  { label: 'Overview', path: '/', icon: LayoutDashboard },
  { label: 'Alert Map', path: '/alerts/map', icon: Map, aliases: ['/alert-map'] },
  { label: 'Alert Log', path: '/alert-log', icon: Bell, aliases: ['/alerts'] },
  { label: 'Transformers', path: '/transformers', icon: Cpu },
  { label: 'Accuracy', path: '/accuracy', icon: Target },
  { label: 'Decision Center', path: '/decisions', icon: BrainCircuit, locked: true },
];

interface SidebarProps {
  collapsed: boolean;
  onToggleCollapsed: () => void;
  /** Called after a link is clicked (closes the mobile menu). */
  onNavigate?: () => void;
}

export function Sidebar({ collapsed, onToggleCollapsed, onNavigate }: SidebarProps) {
  const location = useLocation();

  return (
    <aside
      className={cn(
        'fixed inset-y-0 left-0 z-30 flex flex-col border-r border-grid-border bg-grid-surface transition-all duration-200',
        collapsed ? 'w-16' : 'w-56',
      )}
    >
      {/* Brand */}
      <div className="flex h-14 items-center gap-2.5 border-b border-grid-border px-4">
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand">
          <Zap className="h-4 w-4 text-white" aria-hidden="true" />
        </div>
        {!collapsed && (
          <span className="text-sm font-semibold tracking-tight text-gray-100 whitespace-nowrap">
            Smart Grid Monitor
          </span>
        )}
      </div>

      {/* Navigation */}
      <nav className="flex-1 space-y-0.5 overflow-y-auto px-2 py-3" aria-label="Main navigation">
        {navigation.map((item) => {
          const Icon = item.icon;
          const isActive =
            location.pathname === item.path ||
            (item.path !== '/' && location.pathname.startsWith(`${item.path}/`)) ||
            (item.aliases ?? []).includes(location.pathname);

          return (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={onNavigate}
              className={cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors duration-150',
                isActive
                  ? 'bg-brand/10 text-brand-light border border-brand/20'
                  : 'text-gray-400 hover:text-gray-200 hover:bg-grid-panel border border-transparent',
                collapsed && 'justify-center px-0',
              )}
              title={collapsed ? item.label : undefined}
              aria-current={isActive ? 'page' : undefined}
            >
              <Icon className="h-4.5 w-4.5 shrink-0" aria-hidden="true" />
              {!collapsed && <span>{item.label}</span>}
              {!collapsed && item.locked && (
                <Lock className="ml-auto h-3.5 w-3.5 text-gray-600" aria-label="Closed feature" />
              )}
            </NavLink>
          );
        })}
      </nav>

      {/* Collapse toggle (desktop only — on mobile the menu button controls the sidebar) */}
      <div className="hidden border-t border-grid-border p-2 lg:block">
        <button
          onClick={onToggleCollapsed}
          className="btn-ghost w-full justify-center"
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          type="button"
        >
          {collapsed ? (
            <ChevronRight className="h-4 w-4" aria-hidden="true" />
          ) : (
            <>
              <ChevronLeft className="h-4 w-4" aria-hidden="true" />
              <span className="text-xs">Collapse</span>
            </>
          )}
        </button>
      </div>
    </aside>
  );
}
