# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Skills

For documentation and code inspection, consult your skill library first.

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

**Templates group, plans prescribe, workouts record.** A `WorkoutTemplate` is a reusable grouping of exercises — a warm-up, a push block, a run — and starts nothing. A `WorkoutPlan` is assembled from any number of templates (appended whole, in order, duplicates kept) plus individually chosen exercises, and a plan is what a workout is started from: `WorkoutService.StartWorkoutFromPlanAsync` materializes one `WorkoutExercise` per plan line, identity and order only. The prescription stays on `WorkoutPlanExercise` and is read live for guidance (`GetPlanForGuidanceAsync`), never copied onto the workout — so editing a plan changes what past workouts show they were aiming at and can never alter a recorded set, which is why the builder makes the user confirm before saving a plan that workouts already reference. Plans are soft-deleted (`IsDeleted`, and the FK is `Restrict`) so `Workout.WorkoutPlanId` always resolves; a workout with no plan stays first-class and behaves exactly as workouts always have.

**A `WorkoutExercise` row is no longer evidence anything was done.** Starting from a plan creates a row per planned exercise before the user has touched it, so `Any()`/`Count` over `WorkoutExercises` counts intentions. An exercise counts as performed when it has at least one `Set` or a `Status` of `Easy`/`Medium`/`Hard` — `WorkoutExercise.IsPerformed` for collections already in memory, `WorkoutExerciseStatuses.PerformedPredicate` for EF queries. One rule expressed twice; the two must not drift. Everything derived from a workout filters on it (completing a workout, the daily summary, muscle-group distribution, exercise usage stats, the workout card's exercise count), and a new aggregate that forgets counts exercises nobody did. The statuses are string constants like the criteria below — the codebase contains no enums — with `Pending` and `Skipped` the two that mean not performed; logging a set against a skipped exercise clears the mark back to `Pending`. An effort rating also supplies the RPE of any set logged without one (`WorkoutExerciseStatuses.ImpliedRpe`: 5/7/9), in both directions — rating fills the sets already there, and a set logged against an already-rated exercise takes the same value. `Set.IsRpeDerived` records which values came that way, and only those are ever overwritten or cleared when the rating changes, so what the user typed always stands (WDM-58).

**Weight is stored in kilograms, distance in kilometres.** Every persisted measurement is canonical — `Set.Weight`, `Set.Distance`, `PersonalRecord`, `BodyMeasurement.Weight`, plan and template targets — at `HasPrecision(10, 4)`, because a two-decimal round trip through lbs drifts (45 → 20.41 kg → 44.99). `Services/UnitConverter.cs` is the only place allowed to convert or format a stored measurement; it also owns the unit labels, the plate-sized weight increments and duration formatting. Services return canonical values and views convert once at the point of render (`UnitConverter.Format*`/`ToDisplay*` against a `UserUnits` property the page model resolves); posted values are converted on the way in, inside the service (`LogSetAsync`, `SavePlanAsync`, `SaveTemplateAsync`), so no caller can forget. `PlanEditorModel` and `TemplateEditorModel` are the one shape that is display-unit in both directions — they are form state, not query results. The exceptions are the services that emit prose or a file and so have no view to convert for them — `ProgressiveOverloadSuggestion.Recommendation`, `ExportService`, `AnalyticsPdfExportService` — which convert internally, resolving the preference through `Services/DisplayUnits.cs` (kept out of `UnitConverter` so the converter stays database-free; the PDF service takes the unit as a parameter). Distance units are derived from the weight preference rather than stored (`lbs` implies miles), and `ApplicationUser.PreferredUnits` is free text put through `NormalizeWeightUnit` wherever it is read or written. Body circumference fields are the deliberate exception: no preference exists for them, so they stay `(10, 2)` and unconverted. A missed conversion throws nothing and no test catches it — services are the only tested layer and every measurement is a bare `decimal` — it ships as a plausible-looking number in the wrong unit.

**User scoping is the services' job.** Every user-facing service method takes a `userId` and filters on it inside the query (`w.UserId == userId`, `we.Workout.UserId == userId`), returning `null`/`false` rather than throwing when nothing matches. Pages translate that into `NotFound()`. There is no global query filter and no ownership check in the page models — a new service method that forgets the `userId` predicate is a data leak, not a compile error. `WorkoutTemplate` is the one exception, and it widens rather than narrows: `UserId` is nullable and null means a built-in visible to everyone, which makes it the only entity with two legitimate read predicates. A read that means to include built-ins says so through the `TemplateOwnership` filter (`All`/`Personal`/`BuiltIn`, top of `TemplateService.cs`); a write never does — `SaveTemplateAsync` and `DeleteTemplateAsync` keep the strict `t.UserId == userId`, which is what makes the seeded catalog immutable through every user-reachable route. Copying a built-in is how you change one.

**Authorization is per-page.** `AddRazorPages()` is registered without a global filter, so each page model carries `[Authorize]`. The deliberate exceptions are `Pages/Error`, `Pages/Exercises/*` (the exercise library is reference data), `Pages/OneRepMax` (a plain calculator) and `Pages/Templates/Catalog` (the built-in templates, reference data too — a separate page precisely so `/Templates/Index`, which shows a user's own, can stay protected).

**Startup owns the schema.** `Program.cs` runs `Database.MigrateAsync()` then `DbInitializer.SeedAsync()` in every environment, and rethrows on failure so a bad schema stops the app instead of failing every request. `DbInitializer` seeds the original exercise library, achievement definitions and challenge definitions behind whole-table `AnyAsync()` guards. Everything added since is seeded per entry instead — the extra warm-up, mobility and cardio movements by `Exercise.Name`, and the 25 built-in templates of `Data/TemplateCatalog.cs` by `WorkoutTemplate.CatalogKey` — because a whole-table guard would skip them forever on a database that already has rows. Seed new data that way or existing installs will never see it. Catalog entries name their exercises rather than identify them, and a name that does not resolve throws: failing startup is the point, a built-in with a hole in it being worse than a stopped app.

**Exercise `Category` is behaviour, not a label.** It picks which set inputs a workout shows (`SetInputModel.For`: `Cardio`/`Core`/`Mobility` get a duration, `Cardio` swaps weight and reps for distance), it decides cardio minutes in `AnalyticsService`, and with `Equipment` it seeds `Exercise.TracksOneRepMax` (`DbInitializer.IsWeightLoaded`, a starting point that is then editable per exercise). `Mobility` is new, for warm-up and activation work; the filter dropdown in `Pages/Exercises/Index.cshtml` hardcodes its own category list and does not yet offer it.

**Progress is derived, never stored.** Achievements and challenges recompute from workout data on every read (`ChallengeService`, `AchievementService`), so editing or deleting a past workout is reflected immediately with no counters to reconcile. Their criteria are string constants — `AchievementCriteria` (end of `AchievementService.cs`) and `ChallengeGoalTypes` (end of `ChallengeService.cs`) — encoded into `Achievement.Criteria` as `"{type}:{threshold}"`. Adding a goal type means extending both the constants and the evaluation switch.

**Time is a parameter.** Methods that depend on "now" take an optional `DateTime? asOf`/`unlockedDate` defaulting to `DateTime.UtcNow`, which is how the tests pin windows to fixed dates. Persisted dates are UTC.

**Writable paths are configuration.** `StorageOptions` (bound from the `Storage` section) resolves absolute paths as given and relative ones against the content root, accepting either separator. Progress photo files live outside the database; only metadata is persisted.

## Frontend

No jQuery, no build step beyond Tailwind, no CDN.

**The workout logger is the density constraint.** `card`/`surface-card` drop to 16px padding below 640px, and the exercise card is deliberately a single surface — `_SetInput.cshtml` renders recorded sets and the rows still on offer as one `set-table`, never a box inside a box. That table is `table-layout: fixed` so a set stays one line at any width with no media query, and its column count varies with the exercise, so nothing about it may be hard-coded. `input-cell` is standalone rather than `input` plus an override: both set padding, and `input`'s side padding is most of the width a four-column row has (WDM-UI-21).

**Tailwind 4 is configured entirely in CSS.** There is no `tailwind.config.js`. `wwwroot/css/site.css` (tracked) holds the `@theme` tokens, the class-based `dark` variant, and the `ui`-style component classes; it compiles to `wwwroot/css/output.css` (generated, git-ignored). Prefer `@utility` over `@apply` — see `Specifications/TailwindGuidelines.md`. Recurring looks belong in `site.css` as a named class (`card`, `btn btn-primary`, `input`, `page-header`, `empty-state`, `validation-summary`, …); one-offs stay as utilities in markup.

**Dark mode is class-based**, not `prefers-color-scheme`. An inline script in `_Layout.cshtml` applies the stored preference before first paint; the `layoutShell()` Alpine component owns the toggle and keeps `<html class="dark">`, `color-scheme`, and the `theme-color` meta tag (which the installed web app reads) in step.

**Validation is declared once, in C#.** DataAnnotations on the page models are projected onto native HTML5 constraint attributes by `TagHelpers/Html5ValidationTagHelper` (registered via `@addTagHelper *, FitTracker`). `wwwroot/js/site.js` adds the `validatedForm` Alpine component only for what the browser cannot do alone: `[Compare]` via `setCustomValidity()`, and rendering messages into the existing `asp-validation-for` spans instead of native bubbles — reading the message text from the `data-val-*` attributes ASP.NET already emits. Do not restate a rule in markup or JavaScript.

**Client libraries are vendored.** Alpine, htmx, and Chart.js are committed under `wwwroot/lib/` and refreshed from `node_modules` by the `CopyClientLibs` MSBuild target on each build, so a clean clone runs without npm. Upgrading means bumping `package.json`, `npm install`, rebuild.

**htmx swaps the workout logger; everything else is post-redirect-get plus Alpine.** The exercise library filters with it (`Pages/Exercises/Index.cshtml`), and `/Workouts/Start` uses it for every action taken while training — logging sets, answering for an exercise, stepping between them, adding or removing one. `Pages/Workouts/_WorkoutForm.cshtml` is the whole swap unit: the form declares `hx-target="#workout-form"` and `hx-swap="outerHTML"`, descendants inherit both and add only their own `hx-post`/`hx-get`, and the handlers return `Partial("_WorkoutForm", this)` when `HX-Request` is set and redirect exactly as before when it is not. A reload would restart the rest timer's Alpine state and throw the scroll position away mid-set, which is the whole reason. Three rules keep it working: every `hx-post` carries `hx-include="closest form"` so the antiforgery token travels with it (htmx does not include an enclosing form on its own); actions that leave the page — completing and cancelling — deliberately have no `hx-*` and stay full navigations; and `RespondAsync` sets `HX-Push-Url`, without which a reload would replay the `?planId=` the workout was opened with. The plain `asp-page-handler` submit under every `hx-post` is the no-JavaScript path and still works.

## Tests

xUnit, in `FitTracker.Tests/Services/`. `TestDbContextFactory` opens one shared `Data Source=:memory:` SQLite connection and calls `EnsureCreated()` — real SQLite, not the InMemory provider, so relational behaviour and precision hold. Tests construct services directly against a context (`new ChallengeService(context)`); `WorkoutService` has a convenience constructor that wires its own collaborators for exactly this. Services are only covered here — there are no page or integration tests.

## Specifications

`Specifications/Implementation.md` is the authoritative feature checklist and records what was deliberately left undone and why (e.g. the no-service-worker decision). `Progress.md`, `ManualTestChecklist.md`, `UserGuide.md`, and `Idea.md` cover status, manual validation, end-user behaviour, and the original concept. Update the checklist as features land. Code comments cite requirement ids (`WDM-54`, `WDM-SEC-03`, `D2`) — those are defined in `WorkoutDomainModelSpecification.md`, with `WorkoutDomainModelImplementationPlan.md` recording how it was built.

The entity classes in `Models/` plus the EF configuration in `Data/ApplicationDbContext.cs` are the source of truth for the schema — there is no schema document to keep in step.
