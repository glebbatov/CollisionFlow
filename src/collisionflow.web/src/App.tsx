import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError, api } from './api/client'
import { DataSourceBanner } from './components/DataSourceBanner'
import { JobRow } from './components/JobRow'
import { Reveal } from './components/Reveal'
import { ArchitectureSection } from './sections/ArchitectureSection'
import { BeyondSection } from './sections/BeyondSection'
import { RequirementsSection } from './sections/RequirementsSection'
import type { RepairJob, RepairStatus, RepairStatusInfo, SystemStatus } from './types'

export default function App() {
  const [jobs, setJobs] = useState<RepairJob[] | null>(null)
  const [statuses, setStatuses] = useState<RepairStatusInfo[]>([])
  const [error, setError] = useState<string | null>(null)
  const [announcement, setAnnouncement] = useState('')
  const [savingId, setSavingId] = useState<string | null>(null)
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const [loadedJobs, loadedStatuses, loadedSystem] = await Promise.all([
          api.getRepairJobs(),
          api.getStatuses(),
          api.getSystemStatus(),
        ])
        if (!cancelled) {
          setJobs(loadedJobs)
          setStatuses(loadedStatuses)
          setSystemStatus(loadedSystem)
        }
      } catch (caught) {
        if (!cancelled) {
          setError(caught instanceof Error ? caught.message : 'Could not load repair orders.')
        }
      }
    }

    void load()

    // The database recovers on its own once it has woken, so the banner has to be able to
    // disappear without the user reloading.
    const poll = setInterval(() => {
      api.getSystemStatus().then(
        (next) => {
          if (!cancelled) {
            setSystemStatus(next)
          }
        },
        () => undefined,
      )
    }, 30_000)

    return () => {
      cancelled = true
      clearInterval(poll)
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
      void api.getSystemStatus().then(setSystemStatus, () => undefined)
    } catch (caught) {
      // The server's 422 carries the transitions that WOULD have been legal, so a
      // rejection can explain itself instead of just saying no.
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

  const live = systemStatus?.dataSource === 'Database'

  return (
    <>
      <a className="skip-link" href="#board">
        Skip to repair orders
      </a>

      <header className="masthead">
        <div className="masthead__inner">
          <div>
            <h1>
              CollisionFlow <span className="masthead__sub">Repair Status Tracker</span>
            </h1>
          </div>
          <p className={`pill ${live ? 'pill--live' : 'pill--degraded'}`}>
            <span className="pill__dot" aria-hidden="true" />
            {live ? 'Live · Azure SQL' : 'Demo · in-memory'}
          </p>
        </div>
      </header>

      <main>
        {/* Status changes are announced to assistive technology. Without this a screen
            reader user gets no feedback that anything happened at all. */}
        <p className="visually-hidden" aria-live="polite">
          {announcement}
        </p>

        <section className="section section--board" id="board" aria-labelledby="board-heading">
          <p className="section__eyebrow">The shop floor</p>
          <h2 id="board-heading">Repair orders</h2>
          <p className="section__lead">
            Every dropdown below offers only the moves this order can legally make next — the
            server decides, the page renders. Try to send an illegal one with curl and the
            database still refuses it.
          </p>

          <DataSourceBanner status={systemStatus} />

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

              <div className="board__scroll">
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
                      <th scope="col">Audit</th>
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
              </div>

              {jobs.length === 0 && <p>All bays clear. Suspiciously quiet.</p>}
            </>
          )}
        </section>

        <RequirementsSection statuses={statuses} />
        <BeyondSection systemStatus={systemStatus} />
        <ArchitectureSection />

        <section className="section section--alt" aria-labelledby="close-heading">
          <Reveal>
            <h2 id="close-heading">Built for Crash Champions</h2>
            <p className="section__lead">
              crashchampions.com already gives customers a way to track a repair. This is the
              counterpart a service advisor would work from — the same information, from the
              inside.
            </p>
          </Reveal>
        </section>
      </main>

      <footer className="footer">
        <p>
          CollisionFlow &middot; take-home project &middot;{' '}
          <a href="https://github.com/glebbatov/CollisionFlow">source on GitHub</a>
        </p>
      </footer>
    </>
  )
}
