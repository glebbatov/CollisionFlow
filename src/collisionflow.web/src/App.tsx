import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError, api } from './api/client'
import { JobRow } from './components/JobRow'
import type { RepairJob, RepairStatus, RepairStatusInfo } from './types'

export default function App() {
  const [jobs, setJobs] = useState<RepairJob[] | null>(null)
  const [statuses, setStatuses] = useState<RepairStatusInfo[]>([])
  const [error, setError] = useState<string | null>(null)
  const [announcement, setAnnouncement] = useState('')
  const [savingId, setSavingId] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const [loadedJobs, loadedStatuses] = await Promise.all([
          api.getRepairJobs(),
          api.getStatuses(),
        ])
        if (!cancelled) {
          setJobs(loadedJobs)
          setStatuses(loadedStatuses)
        }
      } catch (caught) {
        if (!cancelled) {
          setError(caught instanceof Error ? caught.message : 'Could not load repair orders.')
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [])

  const labelFor = useCallback(
    (status: RepairStatus) => statuses.find((s) => s.status === status)?.displayName ?? status,
    [statuses],
  )

  const counts = useMemo(() => {
    const tally = new Map<RepairStatus, number>()
    for (const job of jobs ?? []) {
      tally.set(job.status, (tally.get(job.status) ?? 0) + 1)
    }
    return tally
  }, [jobs])

  const handleUpdate = useCallback(async (job: RepairJob, next: RepairStatus) => {
    setSavingId(job.id)
    setError(null)

    try {
      const updated = await api.updateStatus(job.id, next)
      setJobs((current) => current?.map((j) => (j.id === updated.id ? updated : j)) ?? null)
      setAnnouncement(`${updated.jobNumber} moved to ${updated.statusDisplayName}.`)
    } catch (caught) {
      // The server's 422 carries the transitions that WOULD have been legal,
      // so a rejection can explain itself instead of just saying no.
      const message =
        caught instanceof ApiError
          ? caught.message
          : 'The update could not be saved. Please try again.'
      setError(message)
      setAnnouncement(`Update failed. ${message}`)
    } finally {
      setSavingId(null)
    }
  }, [])

  return (
    <>
      <a className="skip-link" href="#main">
        Skip to repair orders
      </a>

      <header className="masthead">
        <h1>
          CollisionFlow <span className="masthead__sub">Repair Status Tracker</span>
        </h1>
      </header>

      <main id="main">
        {/* Status changes are announced to assistive technology. Without this a
            screen reader user gets no feedback that anything happened at all. */}
        <p className="visually-hidden" aria-live="polite">
          {announcement}
        </p>

        {error && (
          <p className="alert" role="alert">
            {error}
          </p>
        )}

        {jobs === null && !error && <p>Warming up the lift&hellip;</p>}

        {jobs !== null && (
          <>
            <ul className="tally">
              {statuses.map((s) => (
                <li key={s.status} className={`tally__item tally__item--${s.status}`}>
                  <span className="tally__count">{counts.get(s.status) ?? 0}</span>
                  <span className="tally__label">{s.displayName}</span>
                </li>
              ))}
            </ul>

            <table className="board">
              <caption className="visually-hidden">
                Repair orders and their current status, most recently updated first.
              </caption>
              <thead>
                <tr>
                  <th scope="col">RO #</th>
                  <th scope="col">Customer</th>
                  <th scope="col">Vehicle</th>
                  <th scope="col">Repair center</th>
                  <th scope="col">Status</th>
                  <th scope="col">Move</th>
                </tr>
              </thead>
              <tbody>
                {jobs.map((job) => (
                  <JobRow
                    key={job.id}
                    job={job}
                    labelFor={labelFor}
                    isSaving={savingId === job.id}
                    onUpdate={handleUpdate}
                  />
                ))}
              </tbody>
            </table>

            {jobs.length === 0 && <p>All bays clear. Suspiciously quiet.</p>}
          </>
        )}
      </main>
    </>
  )
}
