# CLAUDE.md

Project conventions for AI-assisted work in this repository. Read before making changes.

## What this is

A take-home project for a Full Stack Developer role at **Crash Champions** (collision repair,
650+ locations, HQ Westmont IL). The brief asked for a 1–2 hour repair status tracker; this
deliberately goes further, targeted at the specific bullets in the job description.

**It is a portfolio piece.** Every file may be read by an interviewer, and the candidate has
to defend every decision out loud in a walkthrough. Optimize for *explicability*, not
cleverness. If a choice can't be justified in two sentences, make a simpler choice.

## Environment — read this first

| | |
|---|---|
| **Claude can** | Write/edit any file · run `git` and `npm` on the device |
| **Claude cannot** | Run `dotnet`, `docker` or `az` — not installed in the bridge VM, and the cloud sandbox has no outbound network |
| **User must** | Build, run, debug (VS 2022) · `git push` · all Azure work · `npm install` |

**Working loop:** Claude writes a slice → user runs `dotnet build` / `dotnet test` → user
pastes errors → Claude fixes. Never claim something builds; it hasn't been compiled here.

Repo root is `C:\repos\CollisionFlow\CollisionFlow\` (nested by design — do not move).
Planning docs live one level up, outside the repo. Retired files go to `_to_delete/`, also
outside the repo — the bridge cannot delete files, only move them.

## Commands (the user runs these)

```bash
dotnet build
dotnet test
dotnet run --project src/CollisionFlow.Api          # http://localhost:5210
cd src/collisionflow.web && npm run dev             # http://localhost:5173
cd src/collisionflow.web && npm run build           # builds into the API's wwwroot
```

If a build fails with a file lock, an old `CollisionFlow.Api` process is still running.

## Structure

```
src/CollisionFlow.Domain/          business rules — NO package references, ever
src/CollisionFlow.Infrastructure/  storage; implements domain-declared interfaces
src/CollisionFlow.Api/             HTTP surface + composition root; hosts the SPA
src/collisionflow.web/             React 19 + TypeScript + Vite
tests/CollisionFlow.Domain.Tests/  xUnit + Shouldly
db/                                numbered idempotent SQL scripts
```

## Non-negotiables

- **The domain project takes no dependencies.** Not one NuGet package. If something seems to
  require one, it belongs in Infrastructure.
- **`IRepairJobRepository` lives in the domain.** The dependency points inward.
- **Transitions are data, not `if` statements.** `StatusTransitionPolicy` is built from an
  edge set. `db/002_Seed.sql` must hold exactly the same rows as `DefaultTransitions`.
- **`TreatWarningsAsErrors` stays on.** It has already caught a real static-initialization bug.
- **Target `net8.0`.** VS 2022 17.14 cannot target .NET 10. Do not "helpfully" upgrade.
- **Shouldly, not FluentAssertions** (v8 dropped its Apache license).
- **Pin package versions to `8.0.x`** for `Microsoft.Extensions.*` — `dotnet add package`
  defaults to 10.x, which overrides the shared framework.

## The workflow graph — get this right

```
Received      → In Progress, Waiting on Parts
In Progress   → Waiting on Parts, Quality Check
WaitingOnParts→ In Progress
Quality Check → In Progress (rework), Ready for Pickup
ReadyForPickup→ Completed
Completed     → (terminal)
```

Parts holds are reversible; QC can fail and send work back; Completed is terminal because
reopening is a supplement, not an edit to history.

## Code conventions

**C#** — file-scoped namespaces · `_camelCase` private fields · explicit constructors with
readonly fields (not primary constructors, for consistency with the naming rule) · `sealed`
by default · XML doc comments on public members · guard clauses via
`ArgumentException.ThrowIfNullOrWhiteSpace` and friends · `TimeProvider`, never
`DateTime.UtcNow`.

**TypeScript** — `strict` plus `noUncheckedIndexedAccess` · no default exports except page
components · types in `types.ts` mirroring the API contract exactly.

**Comments explain *why*, never *what*.** These comments are part of the deliverable — an
interviewer reads them. A comment that restates the line above it is noise; one that explains
a non-obvious constraint is the reason this project stands out.

**Spelling: American English.** US company. `color`, `defense`, `modeled`, `authorized`,
`initialization`, `judgment`.

**No emoji anywhere** — code, comments, docs, UI, or commit messages.

## Documentation policy

**One `README.md`. That is the whole documentation set.** Scattered docs read as padding on a
project this size, and a reviewer with fifteen minutes will not click into a `docs/` folder.
New material goes into the existing README section it belongs to. Do not create
`ARCHITECTURE.md`, `DECISIONS.md`, `CODE-TOUR.md` or similar.

The README's Roadmap section is marked with an HTML comment and **must be deleted before
submission**.

## Accessibility

Target **WCAG 2.2 AA**, and treat it as a build gate rather than a claim. Status is never
conveyed by color alone. Every interactive element is a real `<button>`/`<a>`/`<select>`.
Changes are announced through `aria-live`. Humor may appear in microcopy and empty states —
never in an error message, a form label, or an `aria-label`.

## Commit style

Conventional commits (`feat:`, `fix:`, `docs:`, `chore:`, `test:`). Present tense, no emoji,
no AI co-author trailers — the AI usage disclosure lives in the README where a reviewer will
actually read it.
