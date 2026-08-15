import { Eyebrow } from '../components/Eyebrow'
import { Reveal } from '../components/Reveal'

const LAYERS = [
  {
    name: 'React SPA',
    note: 'TypeScript, built into the API’s wwwroot so the whole application deploys as one artifact from one origin.',
  },
  {
    name: 'ASP.NET Core API',
    note: 'Controllers, DTOs and the composition root. The only project that knows about both the domain and the infrastructure serving it.',
  },
  {
    name: 'Domain',
    note: 'Zero dependencies — not one NuGet package. Business rules that cannot reference infrastructure cannot accidentally depend on it.',
  },
  {
    name: 'Infrastructure',
    note: 'Dapper over stored procedures, with an in-memory fallback behind a circuit breaker. Implements interfaces the domain declares.',
  },
  {
    name: 'Azure SQL',
    note: 'Schema, seed data, indexes and the workflow itself, applied by idempotent scripts embedded in the assembly.',
  },
]

interface Group {
  area: string
  points: string[]
}

/* Short facts, not sentences: each row reads as one line of specification. */
const STACK: Group[] = [
  {
    area: 'Backend',
    points: [
      'C# / .NET 8, ASP.NET Core Web API',
      'Controllers rather than minimal APIs',
      'RFC 7807 problem responses on every failure path',
      'TimeProvider injected instead of DateTime.UtcNow',
    ],
  },
  {
    area: 'Data',
    points: [
      'Azure SQL with Dapper over stored procedures — no EF',
      'Schema, seed, procedures and indexes as numbered idempotent scripts',
      'Applied at startup from inside the assembly',
      'Polly circuit breaker with in-memory fallback',
    ],
  },
  {
    area: 'Frontend',
    points: [
      'React 19, TypeScript, Vite',
      'Hand-written CSS design system, no UI framework',
      'WCAG 2.2 AA — aria-live announcements, no color-only status',
      '4.5:1 contrast throughout',
    ],
  },
  {
    area: 'Tests',
    points: [
      'xUnit with Shouldly, 61 domain tests',
      'A theory over all 36 status pairs',
      'Checked against a hand-transcribed truth table',
      'Not derived from the production code it verifies',
    ],
  },
  {
    area: 'Build & deploy',
    points: [
      'Central package management',
      'TreatWarningsAsErrors across the solution',
      'GitHub Actions CI/CD with OIDC — no stored secrets',
      'Deployed to Azure App Service on every push',
    ],
  },
]

/**
 * The layers first, then what each is built from. One section rather than two: the
 * masthead's "Stack" link lands on a single answer to "how is this put together", and the
 * two lists share one ruled column so they read as one thing.
 */
export function ArchitectureSection() {
  return (
    <section className="section" id="architecture" aria-labelledby="architecture-heading">
      <Reveal>
        <div className="section__head">
          <Eyebrow>Architecture</Eyebrow>
          <h2 id="architecture-heading">Dependencies point inward</h2>
          <p className="section__lead">
            Storage depends on the business rules. The business rules do not depend on storage —
            which is what lets the same API run against stored procedures in Azure and against a
            list in memory, with neither the controllers nor the domain knowing which.
          </p>
        </div>
      </Reveal>

      <ol className="stack">
        {LAYERS.map((layer, index) => (
          <li key={layer.name}>
            <Reveal delay={index * 80}>
              <div className="stack__layer">
                <span className="stack__index" aria-hidden="true">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <h3 className="stack__name">{layer.name}</h3>
                <p className="stack__note">{layer.note}</p>
              </div>
            </Reveal>
          </li>
        ))}
      </ol>

      <Reveal>
        <div className="spec">
          <Eyebrow quiet>The stack</Eyebrow>
          <h3 className="spec__title" id="stack-heading">
            What this is built from
          </h3>

          <dl className="spec__list" aria-labelledby="stack-heading">
            {STACK.map((group) => (
              <div key={group.area} className="spec__row">
                <dt>{group.area}</dt>
                <dd>
                  <ul className="spec__points">
                    {group.points.map((point) => (
                      <li key={point}>{point}</li>
                    ))}
                  </ul>
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </Reveal>
    </section>
  )
}
