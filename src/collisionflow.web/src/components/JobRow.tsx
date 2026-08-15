import { useId, useState } from 'react'
import type { RepairJob, RepairStatus } from '../types'
import { JobHistory } from './JobHistory'
import { StatusBadge } from './StatusBadge'

interface Props {
  job: RepairJob
  labelFor: (status: RepairStatus) => string
  isSaving: boolean
  onUpdate: (job: RepairJob, next: RepairStatus) => void
}

export function JobRow({ job, labelFor, isSaving, onUpdate }: Props) {
  const [next, setNext] = useState<RepairStatus | ''>('')
  const [historyOpen, setHistoryOpen] = useState(false)

  const selectId = useId()
  const historyId = useId()
  const terminal = job.allowedTransitions.length === 0

  return (
    <>
      {/*
        Each cell repeats its column heading in data-label. Below 60rem the
        table collapses to a stack of blocks and the visual header row is
        gone, so CSS draws the label from this attribute instead — the
        heading and its value stay adjacent at every width, and the real
        <thead> stays in the accessibility tree either way.
      */}
      <tr>
        <td className="cell--mono" data-label="RO #">
          {job.jobNumber}
        </td>
        <td className="cell--name" data-label="Customer">
          {job.customerName}
        </td>
        <td data-label="Vehicle">{job.vehicleDescription}</td>
        <td data-label="Center">{job.repairCenter}</td>
        <td data-label="Status">
          <StatusBadge status={job.status} label={job.statusDisplayName} />
        </td>
        <td data-label="Move">
          {terminal ? (
            <span className="cell--muted">No further steps</span>
          ) : (
            <div className="move">
              {/* Visually hidden rather than absent: every control needs its own accessible
                  name, but two dozen visible "Move to" labels would be screen noise. */}
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
                {/* Only what the server says is legal, so an invalid option is never in the DOM. */}
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
        <td data-label="Audit">
          {/* aria-expanded and aria-controls are what turn a styled button into a
              disclosure a screen reader can describe and navigate. */}
          <button
            type="button"
            className="button--quiet"
            aria-expanded={historyOpen}
            aria-controls={historyId}
            onClick={() => setHistoryOpen((open) => !open)}
          >
            {historyOpen ? 'Hide' : 'History'}
            <span className="visually-hidden"> for {job.jobNumber}</span>
          </button>
        </td>
      </tr>

      {historyOpen && (
        <tr id={historyId} className="row--history">
          <td colSpan={7}>
            <JobHistory jobId={job.id} />
          </td>
        </tr>
      )}
    </>
  )
}
