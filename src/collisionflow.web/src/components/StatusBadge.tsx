import type { RepairStatus } from '../types'

/**
 * Decorative glyphs. Every badge also carries its text label, because
 * WCAG 1.4.1 forbids using color as the only way to convey information -
 * and on a status board, the status IS the information.
 */
const GLYPH: Record<RepairStatus, string> = {
  Received: '▣',
  InProgress: '⚙',
  WaitingOnParts: '⏸',
  QualityCheck: '✔',
  ReadyForPickup: '★',
  Completed: '●',
}

interface Props {
  status: RepairStatus
  label: string
}

export function StatusBadge({ status, label }: Props) {
  return (
    <span className={`badge badge--${status}`}>
      <span className="badge__glyph" aria-hidden="true">
        {GLYPH[status]}
      </span>
      {label}
    </span>
  )
}
