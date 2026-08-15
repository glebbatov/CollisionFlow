# CollisionFlow

**Repair Status Tracker — take-home project for Crash Champions**
Built by [Gleb Batov](https://batovgleb.com/) · August 2026

### ▶ [collisionflow-gb.azurewebsites.net](https://collisionflow-gb.azurewebsites.net)

A shop-floor board for moving vehicle repair orders through a collision center's workflow.
ASP.NET Core 8 API, React 19 SPA, Azure SQL behind stored procedures — deployed as a single
artifact by GitHub Actions on every push.

crashchampions.com already lets customers track a repair. This is the counterpart a service
advisor would work from: the same information, from the inside.

[batovgleb.com](https://batovgleb.com/) · [LinkedIn](https://www.linkedin.com/in/glebbatov/) ·
[GitHub](https://github.com/glebbatov) · batov.gleb1@gmail.com

---

## What was asked, and where it is

| The brief | Delivered | In the code |
|---|---|---|
| A list of repair jobs using mock or sample data | 24 repair orders seeded by a versioned script | `db/002_Seed.sql` |
| Customer name | ✅ | `dbo.RepairJob.CustomerName` |
| Vehicle year / make / model | ✅ as a value object, validated | `Vehicle.cs` |
| Repair center | ✅ as a related table, not a string | `dbo.RepairCenter` |
| Current status | ✅ with the legal next moves attached | `RepairJobResponse` |
| Ability to update the status | `PUT /api/repair-jobs/{id}/status` | `RepairJobsController` |
| Basic validation — only approved statuses | Enforced **three times**, see below | `usp_RepairJob_UpdateStatus` · `RepairJob.ChangeStatus` |
| C# / .NET | ASP.NET Core 8 | |
| React | React 19 + TypeScript | |
| SQL, JSON or in-memory data | All three: Azure SQL primary, in-memory fallback, JSON over the wire | |

The brief said a production-ready application was not expected. It seemed a better use of the
exercise to build one anyway, and let the code answer questions the requirements did not ask.
The commit tagged `v1.0-assignment-scope` is the literal ask, delivered first; everything after
it is deliberate.

---

## The interesting part: where the rules live

The six approved statuses are not a list — they are a graph, and it is not a straight line.

```
Received ──▶ In Progress ⇄ Waiting on Parts
                 │  ▲
                 ▼  │  (failed QC → rework)
           Quality Check ──▶ Ready for Pickup ──▶ Completed
```

- **Parts holds are reversible.** A job waiting on a back-ordered bumper resumes when the part
  lands. It does not restart.
- **Quality Check can fail.** Work that doesn't pass goes back for rework, not forward to the
  customer. Modeling that loop is the difference between a workflow and a progress bar.
- **Completed is terminal.** Reopening a closed repair order is a supplement in the real
  business process, not an edit to history.
- **`Received → Waiting on Parts` exists on purpose.** Parts are often ordered before teardown.

Those transitions live as **rows in `dbo.StatusTransition`**, not as `if` statements. One
definition, four consumers:

| Consumer | Uses it to |
|---|---|
| `usp_RepairJob_UpdateStatus` | reject an illegal move inside the transaction |
| `RepairJob.ChangeStatus` | guarantee the entity can never hold an illegal state |
| `RepairJobsController` | return a 422 that **lists the legal alternatives** |
| `JobRow.tsx` | render only the options that would succeed |

None owns a private copy, so none can drift. Adding a status becomes a data change rather than
a deployment — and an invalid option is never in the DOM, while the database still refuses one
sent by `curl`.

---

## Architecture

```
src/CollisionFlow.Domain/          business rules — no package references at all
src/CollisionFlow.Infrastructure/  storage; implements interfaces the domain declares
src/CollisionFlow.Api/             HTTP surface + composition root; hosts the built SPA
src/collisionflow.web/             React + TypeScript, builds into the API's wwwroot
tests/CollisionFlow.Domain.Tests/  61 tests
db/                                schema, seed, 5 stored procedures, indexes
```

Dependencies point **inward**. `IRepairJobRepository` is declared in the domain and implemented
in infrastructure, so storage depends on the business rather than the reverse — which is what
lets the same API run against Azure SQL in production and a list in memory in a test, with
neither the controllers nor the domain knowing which.

`CollisionFlow.Domain.csproj` has no `PackageReference` elements. None. That emptiness is the
design: rules that *cannot* reference infrastructure cannot accidentally depend on it.

### It degrades honestly

The deployed database is Azure SQL's free tier, which auto-pauses when idle and takes up to a
minute to resume. Rather than serve a timeout to someone opening the link the next morning, a
Polly circuit breaker falls back to the in-memory store and the UI says so plainly.

Two details make that work rather than merely exist:

- **A rejected status change does not trip the breaker.** Without that exclusion, a user
  repeatedly attempting an illegal transition would look like repeated failures, open the
  circuit, and take the database offline for everyone else — a denial of service shipped by
  accident. A business rule saying no is a correct answer, not an outage.
- **A background probe wakes the paused database, but only while degraded.** The request path
  gives up after five seconds, which can never wake a serverless database — so without the
  probe the app would degrade once and stay degraded forever. And the free tier allows 100,000
  vCore-seconds a month; at the 0.5 vCore floor, keeping the database permanently awake would
  burn a month's allowance in under three days. Waking it on demand costs seconds.

---

## Design decisions

Each says what it **cost**, not just what it bought.

**Dapper with stored procedures, not EF Core.** The brief asks for SQL; using EF over stored
procedures means fighting the abstraction to reach the thing you wanted. Schema ships as
numbered idempotent scripts embedded in the assembly and applied at startup, so the same files
a developer runs through `sqlcmd` are the ones CI and Azure apply. *Cost:* no change tracking,
hand-written result mapping.

**Transitions as data, not a `switch`.** One source of truth, four consumers. *Cost:* the
seeded SQL rows must stay in step with the C# fallback constant — there is a test for it.

**`PUT` on a `status` sub-resource, not `POST .../advance`.** A status is a thing that has a
value, so setting it is idempotent and safe to retry after a dropped connection. Re-sending the
current status returns 200 and writes no audit row. *Cost:* reads less naturally than a verb.

**Illegal transitions rejected twice.** The controller asks first so the caller gets a useful
422; the entity checks again so the rule holds if a future code path forgets to ask. Defense in
depth, not flow control. *Cost:* a dictionary lookup per request.

**Controllers, not minimal APIs.** More legible to a team maintaining MVC-shaped code.
*Cost:* more ceremony per endpoint.

**Shouldly, not FluentAssertions.** FluentAssertions v8 dropped Apache licensing for a
commercial license in January 2025 and is no longer free for commercial use. A dependency's
license is part of its cost. *Cost:* slightly less expressive collection assertions.

**.NET 8, not .NET 10.** Targeting .NET 10 in Visual Studio 2022 17.14 is
[explicitly unsupported](https://github.com/dotnet/sdk/issues/51678). *Cost:* .NET 8 reaches
end of support on 10 November 2026, so an upgrade is due — a target-framework change plus a
review of `TimeProvider` usage, which this codebase already uses in its .NET 8 form.

**Warnings as errors, versions declared centrally.** `Directory.Build.props` and
`Directory.Packages.props`. Neither is decorative — see AI usage below for the two defects they
caught. *Cost:* adding a package is a two-file change.

**One deployable, one origin.** Vite builds the SPA into `wwwroot`; the API serves it. No CORS
configuration to get wrong, and no class of bug that appears only in production because the
origins differ there. *Cost:* the frontend can't be cached independently.

---

## Testing

```bash
dotnet test        # 61 tests
```

The centerpiece is a `[Theory]` over **all 36 ordered status pairs**, checked against a truth
table transcribed by hand from the specification — deliberately *not* derived from
`StatusTransitionPolicy.DefaultTransitions`.

A test that reads its expectations from the code under test proves only that the code equals
itself. This one fails if anyone edits the production workflow, which is the entire point.

Also covered: idempotent no-ops leave `UpdatedUtc` untouched, a completed order cannot be
reopened, `(RepairStatus)99` is refused even though the cast compiles, and the value object
rejects implausible input.

---

## Accessibility

WCAG 2.2 AA, treated as a constraint rather than a claim:

- **Status is never conveyed by color alone** (SC 1.4.1). Every badge carries text plus a
  decorative glyph — on a status board, the status *is* the information. Badge text sits on a
  neutral chip with color only on the rule and glyph, so contrast is identical for all six
  statuses rather than depending on which hue a status drew.
- Changes are announced through an `aria-live` region; errors use `role="alert"`.
- Every control is a real `<button>` / `<a>` / `<select>`. Expanding an audit trail uses
  `aria-expanded` and `aria-controls`, so it is a disclosure a screen reader can describe.
- A 3px focus ring at 3.8:1 against its background (SC 2.4.11), and 24px minimum targets
  (SC 2.5.8).
- **Nothing animates** if the system reports `prefers-reduced-motion` — checked in JavaScript
  before the observer is created, and again in CSS in case scripting fails.

---

## Assumptions

1. **Workflow shape.** The brief listed six statuses but not which transitions are legal. The
   graph above is modeled on how collision repair actually sequences, rather than allowing any
   status to become any other.
2. **No authentication.** Out of scope for the exercise. In production, status changes would be
   authorized per repair center and attributed to a named user — the audit trail is designed
   for it.
3. **Single tenant, single region.** No multi-brand or franchise partitioning.
4. **Timestamps are UTC**, formatted client-side. A network spanning 37 states cannot store
   local time.

---

## AI usage

**What I used.** Claude, as a pair-programming assistant, throughout implementation: writing
implementation code from my direction, drafting test bodies once I had specified what needed
proving, verifying current platform facts I would otherwise have assumed (the Azure SQL
free-tier CLI flags, the FluentAssertions license change, GitHub's April 2026 move to immutable
OIDC subject claims), and first drafts of this document.

**What I reviewed and modified.** I read every file as it landed rather than after the fact, and
built and ran the application at each step — the domain model and status mapping, the REST API,
the infrastructure and repository layer, the React SPA, the tests and sample data, the SQL
schema, procedures and scripts, the data-source reporting, and the Azure and CI/CD setup.

**What I decided.** Every entry under Design decisions. I also specified the workflow graph
itself, including the Quality Check rework loop and the reversible parts hold, from how
collision repair sequences. That is domain judgment, not something a model supplied.

**Three things it got wrong, and how they were caught.**

1. **A static initialization-order bug.** The generated `StatusTransitionPolicy` declared
   `Default` *before* the list it was built from. C# runs static initializers in textual order,
   so `Default` would have been constructed from a null list — every request touching the
   policy, which is every request, throwing `NullReferenceException`. The compiler flagged it as
   `CS8604`; in a build with forty other warnings that scrolls past. Because
   `TreatWarningsAsErrors` was on from the start, it stopped the build instead.
2. **A dependency three majors ahead of the runtime.** `dotnet add package` resolved
   `Microsoft.Extensions.DependencyInjection.Abstractions` to 10.0.11 inside a `net8.0` project.
   It builds — but ASP.NET Core 8's shared framework already ships 8.x of that assembly, so the
   reference would override a runtime component. Pinned back, and the whole solution moved to
   central package management after a related conflict.
3. **A SQL Server trap.** Filtered indexes require `QUOTED_IDENTIFIER ON`, and a stored
   procedure captures that setting permanently at `CREATE` time. `sqlcmd` connects with it OFF
   while SSMS connects with it ON — so the procedures would have worked perfectly when tested by
   hand and failed at runtime once the filtered index existed. Fixed by putting the SET options
   inside the scripts, where they belong.

---

## What I would improve with additional time

I'd add integration tests exercising the stored procedures against a real SQL Server in CI —
the workflow rules are enforced in the database, and only the C# half of that is currently
covered by automated tests. I'd finish the optimistic concurrency the schema already supports:
`RowVersion` is stored and returned by every read but not yet enforced through an `If-Match`
header, so two service advisors editing the same repair order is still last-write-wins. And I'd
add authentication with authorization scoped to repair center, since a technician at one
location shouldn't be able to move another location's work.

---

## Run it locally

Requires the **.NET 8 SDK** and **Node 20+**. No SQL Server needed — with no connection string
the app runs entirely on its in-memory store.

```bash
git clone https://github.com/glebbatov/CollisionFlow.git
cd CollisionFlow

# build the SPA into the API's wwwroot
cd src/collisionflow.web && npm install && npm run build && cd ../..

dotnet run --project src/CollisionFlow.Api      # http://localhost:5210
```

To run against SQL Server, set a connection string in
`src/CollisionFlow.Api/appsettings.Development.json`; the schema, seed data and stored
procedures are applied automatically at startup.

For frontend work, run `npm run dev` in `src/collisionflow.web` alongside the API and use
<http://localhost:5173> for hot reload.
