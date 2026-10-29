import { BrainCircuit, Building2, ClipboardCheck, Gauge, Lock, Settings2, ShieldCheck, Users, Workflow } from 'lucide-react';
import { EventTypeBadge, PageHeader, StatusBadge } from '@/components/ui';

/**
 * Decision Center — CLOSED feature.
 *
 * Turns readings and alerts into ranked, recommended actions (who to send, where, and why).
 * The decision rules depend on the utility company that operates the system — its thresholds,
 * crews, costs, and approval process — so the screen ships locked and is enabled and
 * configured per deployment. Everything below the lock is a static, illustrative preview:
 * no API calls are made and no real decisions are produced.
 */

const STEPS = [
  { icon: Gauge, title: 'Read', text: 'Live transformer readings and detected alerts from the warehouse.' },
  { icon: Settings2, title: 'Apply company rules', text: "The owner's thresholds, priorities, costs and regulations." },
  { icon: BrainCircuit, title: 'Recommend', text: 'Rank actions by impact: energy saved, risk, customers affected.' },
  { icon: ClipboardCheck, title: 'Act & track', text: 'Assign crews, approve, and measure the result on the next readings.' },
];

const CUSTOMISATION = [
  { icon: Gauge, label: 'Thresholds & priorities', text: 'Overload %, voltage limits, loss tolerance per district' },
  { icon: Users, label: 'Crews & zones', text: 'Field teams, shifts and coverage areas' },
  { icon: Building2, label: 'Costs & tariffs', text: 'Price per kWh, penalties, repair costs' },
  { icon: Workflow, label: 'Approval workflow', text: 'Who reviews and signs off each action' },
  { icon: ShieldCheck, label: 'Regulations', text: 'Regulator rules and reporting deadlines' },
];

/** Illustrative rows only — shown blurred behind the lock. */
const PREVIEW_ROWS = [
  { priority: 'High', type: 'theft', target: 'T-0199 · Nasr City', action: 'Send inspection team to meters on feeder F-10', impact: '≈ 1.2 MWh/day recovered' },
  { priority: 'High', type: 'overload', target: 'T-0177 · Nasr City', action: 'Shift evening load to T-0178; plan capacity upgrade', impact: 'Peak 110% → 82%' },
  { priority: 'Medium', type: 'line_drop', target: 'T-0446 · Shubra', action: 'Adjust tap changer; inspect line joints', impact: 'Voltage back above 207 V' },
  { priority: 'Low', type: 'overload', target: 'T-0020 · Dokki', action: 'Monitor next 48 h before action', impact: 'No customer impact yet' },
];

export function DecisionCenterPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        icon={BrainCircuit}
        title="Decision Center"
        description="Recommended actions based on the readings — customised for the company that operates the grid."
      >
        <StatusBadge variant="neutral" className="gap-1">
          <Lock className="h-3 w-3" aria-hidden="true" />
          Closed feature
        </StatusBadge>
      </PageHeader>

      {/* Lock notice */}
      <div className="card flex flex-col gap-4 border-brand/40 bg-brand/5 p-5 sm:flex-row sm:items-center" role="status">
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-brand/20">
          <Lock className="h-6 w-6 text-sky-300" aria-hidden="true" />
        </div>
        <div className="space-y-1">
          <h2 className="text-sm font-semibold text-gray-100">This screen is locked</h2>
          <p className="text-sm text-gray-400">
            Decisions depend on the company that owns the system — its rules, crews and costs. The Decision Center is
            enabled and configured for each operator during deployment, so it is closed in this version.
          </p>
        </div>
      </div>

      {/* How it will work */}
      <div>
        <h2 className="mb-3 text-xs font-medium uppercase tracking-wider text-gray-500">How it will work</h2>
        <ol className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
          {STEPS.map((step, i) => {
            const Icon = step.icon;
            return (
              <li key={step.title} className="card p-4">
                <div className="flex items-center gap-2">
                  <span className="flex h-6 w-6 items-center justify-center rounded-full bg-grid-surface font-mono text-xs text-gray-300">{i + 1}</span>
                  <Icon className="h-4 w-4 text-sky-400" aria-hidden="true" />
                  <span className="text-sm font-semibold text-gray-100">{step.title}</span>
                </div>
                <p className="mt-2 text-xs text-gray-400">{step.text}</p>
              </li>
            );
          })}
        </ol>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        {/* Locked preview */}
        <div className="card relative overflow-hidden xl:col-span-2">
          <div className="flex items-center justify-between border-b border-grid-border px-4 py-3">
            <h2 className="text-sm font-semibold text-gray-200">Recommended actions</h2>
            <span className="text-[11px] text-gray-500">Preview · sample data</span>
          </div>
          <div className="pointer-events-none select-none blur-[3px] opacity-60" aria-hidden="true">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-grid-border text-left text-[11px] uppercase tracking-wider text-gray-500">
                  <th className="px-4 py-2.5 font-medium">Priority</th>
                  <th className="px-4 py-2.5 font-medium">Issue</th>
                  <th className="px-4 py-2.5 font-medium">Transformer</th>
                  <th className="px-4 py-2.5 font-medium">Recommended action</th>
                  <th className="px-4 py-2.5 font-medium">Expected impact</th>
                </tr>
              </thead>
              <tbody>
                {PREVIEW_ROWS.map((r) => (
                  <tr key={r.target} className="border-b border-grid-border/60">
                    <td className="px-4 py-3">
                      <StatusBadge variant={r.priority === 'High' ? 'theft' : r.priority === 'Medium' ? 'overload' : 'neutral'}>{r.priority}</StatusBadge>
                    </td>
                    <td className="px-4 py-3"><EventTypeBadge type={r.type} /></td>
                    <td className="px-4 py-3 font-mono text-gray-300">{r.target}</td>
                    <td className="px-4 py-3 text-gray-200">{r.action}</td>
                    <td className="px-4 py-3 text-gray-400">{r.impact}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="absolute inset-0 top-11 flex flex-col items-center justify-center gap-2 bg-grid-bg/70">
            <div className="flex h-11 w-11 items-center justify-center rounded-full border border-grid-border-light bg-grid-panel">
              <Lock className="h-5 w-5 text-gray-300" aria-hidden="true" />
            </div>
            <p className="text-sm font-medium text-gray-200">Available after deployment</p>
            <p className="text-xs text-gray-500">Configured for the operating company</p>
          </div>
        </div>

        {/* What gets customised */}
        <div className="card p-4">
          <h2 className="text-sm font-semibold text-gray-200">Customised per company</h2>
          <ul className="mt-3 space-y-3">
            {CUSTOMISATION.map((c) => {
              const Icon = c.icon;
              return (
                <li key={c.label} className="flex gap-3">
                  <Icon className="mt-0.5 h-4 w-4 shrink-0 text-sky-400" aria-hidden="true" />
                  <div>
                    <p className="text-sm text-gray-200">{c.label}</p>
                    <p className="text-xs text-gray-500">{c.text}</p>
                  </div>
                </li>
              );
            })}
          </ul>
          <button type="button" disabled className="btn-primary mt-4 w-full" title="Enabled during deployment">
            <Lock className="h-3.5 w-3.5" aria-hidden="true" />
            Enable Decision Center
          </button>
        </div>
      </div>
    </div>
  );
}
