# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet run                       # http://localhost:5184 (only profile; no HTTPS profile)
dotnet build                     # also runs npm install + Tailwind build (see MSBuild targets below)
npm run watch:css                # Tailwind watcher for development

dotnet test .\FitTracker.Tests\FitTracker.Tests.csproj -p:SkipTailwindBuild=true
dotnet test .\FitTracker.Tests\FitTracker.Tests.csproj -p:SkipTailwindBuild=true --filter "FullyQualifiedName~ChallengeServiceTests.GetChallengeOverviewAsync_SumsVolumeInsideTheWindow"

dotnet ef migrations add <Name>  # applied automatically at next startup
```

`SkipTailwindBuild=true` matters for tests: the test project references the web project, whose build otherwise shells out to npm. Nothing in the tests needs the CSS.

To reset the database, delete `FitTracker.db` and start the app — migrations and seeding both run at startup.

## Architecture

**Razor Pages + service layer.** Page models stay thin: resolve `userId` from `UserManager`, call a service, assign view properties. All business logic and every EF query live in `Services/`, registered as scoped in `Program.cs`.

**User scoping is the services' job.** Every user-facing service method takes a `userId` and filters on it inside the query (`w.UserId == userId`, `we.Workout.UserId == userId`), returning `null`/`false` rather than throwing when nothing matches. Pages translate that into `NotFound()`. There is no global query filter and no ownership check in the page models — a new service method that forgets the `userId` predicate is a data leak, not a compile error.

**Authorization is per-page.** `AddRazorPages()` is registered without a global filter, so each page model carries `[Authorize]`. The deliberate exceptions are `Pages/Error`, `Pages/Exercises/*` (the exercise library is reference data) and `Pages/OneRepMax` (a plain calculator).

**Startup owns the schema.** `Program.cs` runs `Database.MigrateAsync()` then `DbInitializer.SeedAsync()` in every environment, and rethrows on failure so a bad schema stops the app instead of failing every request. `DbInitializer` seeds the exercise library, achievement definitions, and challenge definitions; each block is guarded by an `AnyAsync()` check so re-running is a no-op.

**Progress is derived, never stored.** Achievements and challenges recompute from workout data on every read (`ChallengeService`, `AchievementService`), so editing or deleting a past workout is reflected immediately with no counters to reconcile. Their criteria are string constants — `AchievementCriteria` (end of `AchievementService.cs`) and `ChallengeGoalTypes` (end of `ChallengeService.cs`) — encoded into `Achievement.Criteria` as `"{type}:{threshold}"`. Adding a goal type means extending both the constants and the evaluation switch.

**Time is a parameter.** Methods that depend on "now" take an optional `DateTime? asOf`/`unlockedDate` defaulting to `DateTime.UtcNow`, which is how the tests pin windows to fixed dates. Persisted dates are UTC.

**Writable paths are configuration.** `StorageOptions` (bound from the `Storage` section) resolves absolute paths as given and relative ones against the content root, accepting either separator. Progress photo files live outside the database; only metadata is persisted.

## Frontend

No jQuery, no build step beyond Tailwind, no CDN.

**Tailwind 4 is configured entirely in CSS.** There is no `tailwind.config.js`. `wwwroot/css/site.css` (tracked) holds the `@theme` tokens, the class-based `dark` variant, and the `ui`-style component classes; it compiles to `wwwroot/css/output.css` (generated, git-ignored). Prefer `@utility` over `@apply` — see `Specifications/TailwindGuidelines.md`. Recurring looks belong in `site.css` as a named class (`card`, `btn btn-primary`, `input`, `page-header`, `empty-state`, `validation-summary`, …); one-offs stay as utilities in markup.

**Dark mode is class-based**, not `prefers-color-scheme`. An inline script in `_Layout.cshtml` applies the stored preference before first paint; the `layoutShell()` Alpine component owns the toggle and keeps `<html class="dark">`, `color-scheme`, and the `theme-color` meta tag (which the installed web app reads) in step.

**Validation is declared once, in C#.** DataAnnotations on the page models are projected onto native HTML5 constraint attributes by `TagHelpers/Html5ValidationTagHelper` (registered via `@addTagHelper *, FitTracker`). `wwwroot/js/site.js` adds the `validatedForm` Alpine component only for what the browser cannot do alone: `[Compare]` via `setCustomValidity()`, and rendering messages into the existing `asp-validation-for` spans instead of native bubbles — reading the message text from the `data-val-*` attributes ASP.NET already emits. Do not restate a rule in markup or JavaScript.

**Client libraries are vendored.** Alpine, htmx, and Chart.js are committed under `wwwroot/lib/` and refreshed from `node_modules` by the `CopyClientLibs` MSBuild target on each build, so a clean clone runs without npm. Upgrading means bumping `package.json`, `npm install`, rebuild. htmx is loaded globally but used in only one place today (`Pages/Exercises/Index.cshtml`); most interactivity is post-redirect-get plus Alpine.

## Tests

xUnit, in `FitTracker.Tests/Services/`. `TestDbContextFactory` opens one shared `Data Source=:memory:` SQLite connection and calls `EnsureCreated()` — real SQLite, not the InMemory provider, so relational behaviour and precision hold. Tests construct services directly against a context (`new ChallengeService(context)`); `WorkoutService` has a convenience constructor that wires its own collaborators for exactly this. Services are only covered here — there are no page or integration tests.

## Specifications

`Specifications/Implementation.md` is the authoritative feature checklist and records what was deliberately left undone and why (e.g. the no-service-worker decision). `Progress.md`, `ManualTestChecklist.md`, `UserGuide.md`, and `Idea.md` cover status, manual validation, end-user behaviour, and the original concept. Update the checklist as features land.

The entity classes in `Models/` plus the EF configuration in `Data/ApplicationDbContext.cs` are the source of truth for the schema — there is no schema document to keep in step.
