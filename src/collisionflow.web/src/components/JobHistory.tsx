import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { StatusChange } from '../types'

interface Props {
  jobId: string
}

/**
 * The audit trail for one repair order.
 *
 * Loaded on demand rather than with the board: twenty-four histories nobody has asked for
 * is twenty-four round trips wasted, and the panel is closed by default.
 */
export function JobHistory({ jobId }: Props) {
  const [changes, setChanges] = useState<StatusChange[] | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false

    api.getHistory(jobId).then(
      (loaded) => {
        if (!cancelled) {
          setChanges(loaded)
        }
      },
      () => {
        if (!cancelled) {
          setFailed(true)
        }
      },
    )

    return () => {
      cancelled = true
    }
  }, [jobId])

  if (failed) {
    return <p className="history__empty">The history could not be loaded.</p>
  }

  if (changes === null) {
    return <p className="history__empty">Loading history&hellip;</p>
  }

  if (changes.length === 0) {
    return (
      <p className="history__empty">
        No changes recorded yet. Move this order and it will appear here.
      </p>
    )
  }

  return (
    <ol className="history">
      {changes.map((change) => (
        <li key={`${change.changedUtc}-${change.to}`} className="history__item">
          <span className="history__move">
            {change.fromDisplayName} <span aria-hidden="true">&rarr;</span>{' '}
            <strong>{change.toDisplayName}</strong>
          </span>
          <span className="history__meta">
            {new Date(change.changedUtc).toLocaleString()} &middot; {change.changedBy}
            {change.note ? ` — ${change.note}` : ''}
          </span>
        </li>
      ))}
    </ol>
  )
}
