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
    </section>
  )
}
