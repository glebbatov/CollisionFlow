import { Eyebrow } from '../components/Eyebrow'
import { Reveal } from '../components/Reveal'
import { StatusBadge } from '../components/StatusBadge'
import type { RepairStatusInfo } from '../types'

interface Requirement {
  /** Quoted verbatim from the brief, so the mapping is checkable rather than asserted. */
  asked: string
  built: string
  evidence: string
}

const REQUIREMENTS: Requirement[] = [
  {
    asked: 'A list of repair jobs using mock or sample data',
    built: 'Twenty-four repair orders, seeded into SQL Server by a versioned script.',
    evidence: 'db/002_Seed.sql · GET /api/repair-jobs',
  },
  {
    asked: 'Customer name, vehicle year/make/model, repair center, current status',
    built: 'Every field on the board above, with repair centers as a related table rather than a string.',
    evidence: 'dbo.RepairJob · dbo.RepairCenter',
  },
  {
    asked: 'Ability to update the status of a repair job',
    built: 'A PUT against the job’s status sub-resource, so re-sending the current status is a safe no-op.',
    evidence: 'PUT /api/repair-jobs/{id}/status',
  },
  {
    asked: 'Basic validation so only approved statuses can be used',
    built: 'Enforced three times over: the stored procedure checks the transition inside its transaction, the domain entity refuses to hold an illegal state, and the API returns 422 listing what would have been legal.',
    evidence: 'usp_RepairJob_UpdateStatus · RepairJob.ChangeStatus · RepairJobsController',
  },
  {
    asked: 'Use the technology stack you are most comfortable with — C# / .NET, React, SQL',
    built: 'ASP.NET Core 8 and React 19 with TypeScript, over SQL Server via stored procedures. All three, not one of them.',
    evidence: '.NET 8 · React 19 · Azure SQL',
  },
]

interface Props {
  statuses: RepairStatusInfo[]
}

export function RequirementsSection({ statuses }: Props) {
  return (
    <section className="section" id="requirements" aria-labelledby="requirements-heading">
      <Reveal>
        <div className="section__head">
          <Eyebrow>The brief</Eyebrow>
          <h2 id="requirements-heading">Every requirement, and where it lives</h2>
          <p className="section__lead">
            The brief said a production-ready application was not expected. This is what it looks
            like when one is built anyway — each line of the original request, and the code that
            answers it.
          </p>
        </div>
      </Reveal>

      <ul className="cards cards--brief">
        {REQUIREMENTS.map((requirement, index) => (
          <li key={requirement.asked}>
            <Reveal delay={index * 70}>
              <article className="card">
                <span className="card__index" aria-hidden="true">
                  {String(index + 1).padStart(2, '0')} / {String(REQUIREMENTS.length).padStart(2, '0')}
                </span>
                <p className="card__asked">
                  <span className="card__check" aria-hidden="true">
                    &#10003;
                  </span>
                  <span className="visually-hidden">Satisfied: </span>
                  &ldquo;{requirement.asked}&rdquo;
                </p>
                <p className="card__built">{requirement.built}</p>
                <p className="card__evidence">{requirement.evidence}</p>
              </article>
            </Reveal>
          </li>
        ))}
      </ul>

      <Reveal>
        <div className="card card--wide">
          <p className="card__asked">
            <span className="card__check" aria-hidden="true">
              &#10003;
            </span>
            <span className="visually-hidden">Satisfied: </span>
            &ldquo;Approved statuses: Received, In Progress, Waiting on Parts, Quality Check, Ready
            for Pickup, Completed&rdquo;
          </p>
          <p className="card__built">
            These are not hard-coded in this page. They were fetched from{' '}
            <code>/api/statuses</code>, which reads them from <code>dbo.RepairStatus</code>.
          </p>
          <ul className="chips">
            {statuses.map((status) => (
              <li key={status.status}>
                <StatusBadge status={status.status} label={status.displayName} />
              </li>
            ))}
          </ul>
        </div>
      </Reveal>
    </section>
  )
}
