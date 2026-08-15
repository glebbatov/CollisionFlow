import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { ApiError, api } from './api/client'
import { BracketLink } from './components/BracketLink'
import { Byline } from './components/Byline'
import { DataSourceBanner } from './components/DataSourceBanner'
import { Eyebrow } from './components/Eyebrow'
import { JobRow } from './components/JobRow'
import { Reveal } from './components/Reveal'
import { ArchitectureSection } from './sections/ArchitectureSection'
import { BeyondSection } from './sections/BeyondSection'
import { RequirementsSection } from './sections/RequirementsSection'
import type { RepairJob, RepairStatus, RepairStatusInfo, SystemStatus } from './types'

const NAV = [
  { href: '#board', label: 'Board' },
  { href: '#requirements', label: 'Brief' },
  { href: '#beyond', label: 'Beyond' },
  { href: '#architecture', label: 'Stack' },
]

export default function App() {
  const [jobs, setJobs] = useState<RepairJob[] | null>(null)
  const [statuses, setStatuses] = useState<RepairStatusInfo[]>([])
  const [error, setError] = useState<string | null>(null)
  const [announcement, setAnnouncement] = useState('')
  const [savingId, setSavingId] = useState<string | null>(null)
  const [systemStatus, setSystemStatus] = useState<SystemStatus | null>(null)
  const masthead = useRef<HTMLElement>(null)

  /*
   * Publishes the masthead's real height as --masthead-h.
   *
   * The bar is sticky, so every in-page anchor would otherwise land with its
   * heading hidden underneath it. The offset cannot be a constant: the bar is
   * built from fluid type, so its height moves with the viewport, and it moves
   * again when the web font finishes loading and the metrics change. Measuring
   * it is the only version that is right at every width and at both stages of
   * the font swap.
   */
  useEffect(() => {
    const element = masthead.current
    if (!element || typeof ResizeObserver === 'undefined') {
      return
    }

    const observer = new ResizeObserver(() => {
      document.documentElement.style.setProperty(
        '--masthead-h',
        `${element.getBoundingClientRect().height}px`,
      )
    })

    observer.observe(element)
    return () => observer.disconnect()
  }, [])

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

  const openOrders = useMemo(
    () =>
      (jobs ?? []).filter((job) => !statuses.find((s) => s.status === job.status)?.isTerminal)
        .length,
    [jobs, statuses],
  )

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

      <header className="masthead" ref={masthead}>
        <div className="masthead__inner">
          <h1 className="wordmark">
            CollisionFlow <span className="wordmark__for">for Crash Champions</span>
            <span className="wordmark__sub">Repair status tracker</span>
          </h1>

          {/* In-page anchors, in the bracket style the labels use everywhere else.
              Hidden below 60rem rather than folded into a hamburger: four links to
              four sections of one page do not justify a menu a keyboard user has
              to open. */}
          <nav className="masthead__nav" aria-label="Sections">
            {NAV.map((item) => (
              <BracketLink key={item.href} href={item.href} variant="nav">
                {item.label}
              </BracketLink>
            ))}
          </nav>

          {/* The source name is a separate span so the narrowest phones can hide it
              from view while the accessible name stays "Live · Azure SQL". */}
          <p className={`pill ${live ? 'pill--live' : 'pill--degraded'}`}>
            <span className="pill__dot" aria-hidden="true" />
            {live ? 'Live' : 'Demo'}
            <span className="pill__source"> · {live ? 'Azure SQL' : 'in-memory'}</span>
          </p>
        </div>
      </header>

      <main>
        {/* Status changes are announced to assistive technology. Without this a screen
            reader user gets no feedback that anything happened at all. */}
        <p className="visually-hidden" aria-live="polite">
          {announcement}
        </p>

        <section className="hero" aria-labelledby="hero-heading">
          <Eyebrow>Collision repair · shop floor</Eyebrow>
          <h2 className="hero__headline" id="hero-heading">
            Every repair order, <em>from the inside.</em>
          </h2>
          <p className="hero__lead">
            The status board a service advisor works from. The workflow is not written in this
            page, or in the API — it is rows in a table, and the database refuses an illegal
            move even when the request never touches this code.
          </p>

          <Byline />

          <ul className="hero__meta">
            <li>
              <span className="hero__meta-label">Open orders</span>
              <span className="hero__meta-value">
                {jobs === null ? '—' : String(openOrders).padStart(2, '0')}
              </span>
            </li>
            <li>
              <span className="hero__meta-label">In the shop</span>
              <span className="hero__meta-value">
                {jobs === null ? '—' : String(jobs.length).padStart(2, '0')}
              </span>
            </li>
            <li>
              <span className="hero__meta-label">Workflow states</span>
              <span className="hero__meta-value">
                {statuses.length === 0 ? '—' : String(statuses.length).padStart(2, '0')}
              </span>
            </li>
            <li>
              <span className="hero__meta-label">Serving from</span>
              <span className="hero__meta-value">{live ? 'Azure SQL' : 'In-memory'}</span>
            </li>
          </ul>
        </section>

        <section className="section section--board" id="board" aria-labelledby="board-heading">
          <div className="section__head">
            <Eyebrow>The shop floor</Eyebrow>
            <h2 id="board-heading">Repair orders</h2>
            <p className="section__lead">
              Every dropdown below offers only the moves this order can legally make next — the
              server decides, the page renders. Try to send an illegal one with curl and the
              database still refuses it.
            </p>
          </div>

          <DataSourceBanner status={systemStatus} />

          {error && (
            <p className="alert" role="alert">
              <strong>Error</strong>
              <span>{error}</span>
            </p>
          )}

          {jobs === null && !error && <p className="loading">Warming up the lift&hellip;</p>}

          {jobs !== null && (
            <>
              <ul className="tally">
                {statuses.map((s) => (
                  <li key={s.status} className={`tally__item tally__item--${s.status}`}>
                    <span className="tally__count">
                      {String(counts.get(s.status) ?? 0).padStart(2, '0')}
                    </span>
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
                      <th scope="col" className="col--vehicle">
                        Vehicle
                      </th>
                      <th scope="col" className="col--center">
                        Center
                      </th>
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

              {jobs.length === 0 && <p className="loading">All bays clear. Suspiciously quiet.</p>}
            </>
          )}
        </section>

        <RequirementsSection statuses={statuses} />
        <BeyondSection systemStatus={systemStatus} />
        <ArchitectureSection />

        <section className="section section--alt" aria-labelledby="close-heading">
          <Reveal>
            <div className="section__head">
              <Eyebrow>Context</Eyebrow>
              <h2 id="close-heading">Built for Crash Champions</h2>
              <p className="section__lead">
                crashchampions.com already gives customers a way to track a repair. This is the
                counterpart a service advisor would work from — the same information, from the
                inside.
              </p>
            </div>
          </Reveal>
        </section>
      </main>

      <footer className="footer">
        <div className="footer__inner">
          <p>
            CollisionFlow · take-home project by <a href="https://batovgleb.com/">Gleb Batov</a>
          </p>
          <p>
            <a href="https://github.com/glebbatov/CollisionFlow">Source on GitHub</a>
          </p>
        </div>
      </footer>
    </>
  )
}
