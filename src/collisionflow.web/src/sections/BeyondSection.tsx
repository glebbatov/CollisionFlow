import { Reveal } from '../components/Reveal'
import type { SystemStatus } from '../types'

interface Item {
  title: string
  body: string
  detail: string
}

const ITEMS: Item[] = [
  {
    title: 'The workflow lives in the database',
    body: 'Legal transitions are rows in dbo.StatusTransition, not if-statements. The stored procedure validates against them, the API reports them, and the dropdowns above are built from them — one definition, four consumers, no drift.',
    detail: 'Adding a status becomes a data change, not a deployment.',
  },
  {
    title: 'Five stored procedures',
    body: 'Every read and write goes through a procedure. The status change takes an update lock on the read, verifies the transition, writes the audit row and commits — all in one transaction, so a job can never move without a record of why.',
    detail: 'usp_RepairJob_UpdateStatus',
  },
  {
    title: 'It degrades honestly',
    body: 'The database is Azure SQL’s free tier and pauses when idle. A circuit breaker falls back to an in-memory store and the banner says so, rather than showing sample data as though it were real.',
    detail: 'Try it: the pill in the header is live.',
  },
  {
    title: 'Errors that explain themselves',
    body: 'Reject a status change and the 422 comes back with the transitions that would have been legal. An error that only says no forces the client to guess, or to ship its own copy of a rule it does not own.',
    detail: 'RFC 7807 problem details, on every failure path',
  },
  {
    title: 'Deployed by pushing',
    body: 'Every commit to main builds, runs the tests, compiles the SPA into the API, deploys to Azure and then asks the running site which data source answered. There is no publish profile or client secret in the repository.',
    detail: 'GitHub Actions with OIDC federation',
  },
  {
    title: 'Accessible by construction',
    body: 'Status is never conveyed by color alone. Changes are announced through a live region. Every control is reachable and operable by keyboard, and this page does not animate at all if your system asks it not to.',
    detail: 'WCAG 2.2 AA',
  },
]

interface Props {
  systemStatus: SystemStatus | null
}

export function BeyondSection({ systemStatus }: Props) {
  return (
    <section className="section section--alt" aria-labelledby="beyond-heading">
      <Reveal>
        <p className="section__eyebrow">Beyond the brief</p>
        <h2 id="beyond-heading">What production-ready actually meant here</h2>
        <p className="section__lead">
          Currently serving from{' '}
          <strong>{systemStatus?.dataSource === 'Database' ? 'Azure SQL' : 'the in-memory fallback'}</strong>
          .
        </p>
      </Reveal>

      <ul className="cards cards--two">
        {ITEMS.map((item, index) => (
          <li key={item.title}>
            <Reveal delay={index * 60}>
              <article className="card">
                <h3 className="card__title">{item.title}</h3>
                <p className="card__built">{item.body}</p>
                <p className="card__evidence">{item.detail}</p>
              </article>
            </Reveal>
          </li>
        ))}
      </ul>
    </section>
  )
}
