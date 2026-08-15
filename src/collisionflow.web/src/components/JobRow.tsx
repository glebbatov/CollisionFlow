import { useState } from 'react'
import type { RepairJob, RepairStatus } from '../types'
import { StatusBadge } from './StatusBadge'

interface Props {
  job: RepairJob
  labelFor: (status: RepairStatus) => string
  isSaving: boolean
  onUpdate: (job: RepairJob, next: RepairStatus) => void
}

export function JobRow({ job, labelFor, isSaving, onUpdate }: Props) {
  const [next, setNext] = useState<RepairStatus | ''>('')

  const selectId = `status-${job.id}`
  const terminal = job.allowedTransitions.length === 0

  return (
    <tr>
      <td className="cell--mono">{job.jobNumber}</td>
      <td>{job.customerName}</td>
      <td>{job.vehicleDescription}</td>
      <td>{job.repairCenter}</td>
      <td>
        <StatusBadge status={job.status} label={job.statusDisplayName} />
      </td>
      <td>
        {terminal ? (
          <span className="cell--muted">No further steps</span>
        ) : (
          <div className="move">
            {/* The label is visually hidden rather than absent: every row needs
                its own accessible name, but six visible "Move to" labels would
                be noise on screen. */}
            <label className="visually-hidden" htmlFor={selectId}>
              Move {job.jobNumber} to
            </label>
            <select
              id={selectId}
              value={next}
              disabled={isSaving}
              onChange={(e) => setNext(e.target.value as RepairStatus | '')}
            >
              <option value="">Move to&hellip;</option>
              {/* Only what the server says is legal. An invalid option is never
                  rendered, so it can never be chosen. */}
              {job.allowedTransitions.map((status) => (
                <option key={status} value={status}>
                  {labelFor(status)}
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={next === '' || isSaving}
              onClick={() => {
                if (next !== '') {
                  onUpdate(job, next)
                  setNext('')
                }
              }}
            >
              {isSaving ? 'Saving…' : 'Update'}
            </button>
          </div>
        )}
      </td>
    </tr>
  )
}
