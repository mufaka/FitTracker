# Workout Domain Model Implementation Plan

This document defines the phased implementation plan for the workout domain model — templates, plans, workouts,
exercise status, canonical units and the built-in template catalog — based on
[WorkoutDomainModelSpecification.md](WorkoutDomainModelSpecification.md). Each phase builds on the previous one
and is broken into work items suitable for issue tracking. Check off items as they are completed.

---

## Progress Summary

| Phase | Description | Status |
|-------|------------|--------|
| 1 | Measurement Units Foundation | Complete |
| 2 | Template Layer Widening | Complete |
| 3 | Plan Layer — Data and Service | Complete |
| 4 | Plan Builder Surfaces | Complete |
| 5 | Guided Workout Execution | Complete |
| 6 | Built-In Template Catalog | Complete |
| 7 | Cross-Cutting Validation and Documentation | Complete |

---

## Current State

- `Workout`, `WorkoutExercise` and `Set` already model performed training and are consumed by 8 services and
  ~15 page files. This plan adds to them but does not restructure them.
- `WorkoutTemplate` / `WorkoutTemplateExercise` exist as user-owned entities with `DefaultSets` and
  `DefaultReps` that `StartWorkoutFromTemplateAsync` currently discards.
- `PreferredUnits` is a display label only. Weights are stored exactly as typed, so switching preference
  relabels history rather than converting it. `Set.Weight` is `HasPrecision(10, 2)`.
- `Set.Duration` exists but is never written; there is no `Distance` field anywhere.
- There is no plan concept, no exercise status, and no built-in template.
- Seeding runs at startup behind whole-table `AnyAsync()` guards, so new seed rows never reach an existing
  database.
- The application has never been deployed. There is no production data, and the reset path is deleting
  `FitTracker.db` and restarting.

### Dependency Notes

Three ordering constraints are not obvious from the specification and drive the phase order:

- **Units come first.** Every new numeric field (`DefaultDistance`, `TargetDistance`, `Set.Distance`) is
  canonical by definition. Introducing them before the conversion boundary exists would mean writing those
  surfaces twice.
- **`GetActiveTemplatesAsync` cannot be removed until Phase 5.** `WorkoutSuggestionService` depends on it, and
  its replacement (`GetActivePlansAsync`) is only wired into suggestions once plans can guide workouts.
- **`StartWorkoutFromTemplateAsync` cannot be removed until Phase 5.** It is the only current path from a
  template into a workout, and removing it before start-from-plan exists would leave no way to begin a
  templated workout.

---

## Guiding Principles

1. Page models stay thin: resolve `userId`, call a service, assign view properties. All EF queries live in
   `Services/`.
2. Every user-facing service method filters on `userId` inside the query and returns `null`/`false` when nothing
   matches. Pages translate that into `NotFound()`.
3. Tests accompany every phase, following the existing `TestDbContextFactory` pattern against real SQLite.
4. Reuse existing patterns — Tailwind component classes in `site.css`, DataAnnotations projected by
   `Html5ValidationTagHelper`, string-constant classes for closed value sets.
5. Migrations are added per phase and applied automatically at startup. Because there is no deployed data, a
   phase may also be validated by deleting `FitTracker.db` and letting migrations and seeding rebuild it.
6. The plan follows the specification; it does not introduce scope the specification does not define.

---

## Phase 1: Measurement Units Foundation

Establishes canonical storage and a single conversion boundary before any new numeric field exists. Delivers no
new user-facing feature, but corrects the existing defect in which changing unit preference relabels history
instead of converting it.

### 1.1 Conversion Component

- [x] Create the unit conversion component with `ToCanonicalWeight`, `ToDisplayWeight`, `ToCanonicalDistance`,
      `ToDisplayDistance` and `WeightIncrement` (WDM-30 – WDM-32, §5.4)
- [x] Define display-unit derivation: `lbs` implies miles, `kg` implies kilometres; no new preference field
- [x] Keep the component pure and free of database access so it is directly testable (WDM-NF-06)
- [x] Register the component in `Program.cs` if it is resolved by dependency injection — not needed:
      `UnitConverter` is a pure static class, following `OneRepMaxCalculator`

### 1.2 Schema Precision

- [x] Change `Set.Weight` precision from `(10,2)` to `(10,4)` in `ApplicationDbContext`
- [x] Set `BodyMeasurement.Weight` precision to `(10,4)`
- [x] Add migration `CanonicalMeasurementPrecision`

### 1.3 Write Path

- [x] Convert weight from display unit to canonical in `WorkoutService.LogSetAsync`
- [x] Convert body weight from display unit to canonical when measurements are saved
- [x] Verify no other write path persists a user-entered weight

### 1.4 Read Path

- [x] Convert weights for display on `Workouts/Start`, `Workouts/Details`, `Workouts/History` and
      `Pages/Shared/Components/_WorkoutCard.cshtml` — the latter two render no measurement
- [x] Convert weights for display across `Analytics`, `PRs`, `Progress`, `OneRepMax` and `Measurements`
- [x] Convert weights in `ExportService` and `AnalyticsPdfExportService` output
- [x] Audit for any remaining site that formats a stored measurement directly (WDM-33)

### 1.5 Progressive Overload

- [x] Replace `GetSuggestedWeightIncrement(decimal weight)` with unit-aware increment selection, computed in the
      display unit and converted back to canonical (WDM-35)
- [x] Define increment thresholds per display unit rather than against the raw stored value

### 1.6 Tests

- [x] Conversion round-trip tests in both directions, asserting display-precision stability (WDM-TEST-10)
- [x] Increment tests asserting different results per display unit (WDM-TEST-11)
- [x] Tests asserting volume, personal records and one-rep-max results are unaffected by a change of display
      preference (WDM-TEST-12)

---

## Phase 2: Template Layer Widening

Widens templates from "a full workout" to "a reusable grouping", adds their duration and distance prescription,
and introduces ownerless built-in templates at the schema and service level. The catalog content itself arrives
in Phase 6.

### 2.1 Model and Schema

- [x] Make `WorkoutTemplate.UserId` nullable and its relationship optional (WDM-04)
- [x] Add `WorkoutTemplate.CatalogKey`, max 100, unique where not null (WDM-41)
- [x] Make `WorkoutTemplateExercise.DefaultSets` and `DefaultReps` nullable (WDM-03)
- [x] Add `WorkoutTemplateExercise.DefaultDurationSeconds` and `DefaultDistance` at precision `(10,4)`
- [x] Add migration `WidenWorkoutTemplates`

### 2.2 Service Layer

- [x] Widen `GetTemplatesAsync` to `t.UserId == userId || t.UserId == null` and add an ownership filter
      parameter (WDM-05, WDM-09)
- [x] Widen `GetTemplateEditorAsync` so built-ins can be previewed
- [x] Add `GetCatalogAsync` returning built-in templates without requiring a user (WDM-45)
- [x] Add `CopyTemplateAsync` producing a user-owned copy of any visible template (WDM-07)
- [x] Confirm `SaveTemplateAsync` and `DeleteTemplateAsync` retain the strict ownership predicate (WDM-06) —
      `SaveTemplateAsync` additionally now returns `null` instead of throwing when the template is not
      owned, which is what WDM-06 asks for and what the rest of the service layer does
- [x] Leave `GetActiveTemplatesAsync` in place — it is removed in Phase 5

### 2.3 Template Surfaces

- [x] Add the ownership filter to `/Templates/Index` (WDM-UI-05)
- [x] Suppress edit and delete affordances for built-in templates
- [x] Add a copy action to template cards
- [x] Show the exercises a template contains on its card, not only name and count (WDM-UI-06)
- [x] Surface duration and distance in the template builder prescription inputs

### 2.4 Tests

- [x] Template reads include built-ins and exclude other users' templates (WDM-TEST-08)
- [x] Template writes and deletes reject built-ins (WDM-TEST-08)
- [x] `CopyTemplateAsync` produces an owned, independently editable copy

---

## Phase 3: Plan Layer — Data and Service

Introduces the plan entities and their service with no user-facing surface, so the data contract can be settled
and tested before the builder is written.

### 3.1 Entities and Configuration

- [x] Create `Models/WorkoutPlan.cs` with `Name`, `Description`, `IsActive`, `IsDeleted`, `CreatedAt`, `UserId`
      and `Exercises` (§4.1)
- [x] Create `Models/WorkoutPlanExercise.cs` with `Order`, `TargetSets`, `TargetReps`,
      `TargetDurationSeconds`, `TargetDistance` and `Notes`
- [x] Add `ApplicationUser.WorkoutPlans` and `Exercise.WorkoutPlanExercises` navigations
- [x] Configure relationships, cascade behaviour, `Restrict` on the exercise FK, string lengths, decimal
      precision and the `IX_WorkoutPlans_UserId` index
- [x] Add `DbSet<WorkoutPlan>` and `DbSet<WorkoutPlanExercise>` to `ApplicationDbContext`
- [x] Add migration `AddWorkoutPlans` — it also adds `Workout.WorkoutPlanId`, brought forward from
      Phase 5.1: `IsPlanReferencedAsync` and its test (3.3) cannot exist without the foreign key

### 3.2 Service

- [x] Create `Services/WorkoutPlanService.cs` with `IWorkoutPlanService` (§5.1)
- [x] Implement `GetPlansAsync` and `GetActivePlansAsync`, excluding soft-deleted plans
- [x] Implement `GetPlanEditorAsync` and `SavePlanAsync`, validating exercise ids as `SaveTemplateAsync` does
- [x] Implement `ApplyTemplateAsync`, appending template exercises in order with their prescription and
      retaining duplicates (WDM-12, WDM-13)
- [x] Implement `SetPlanActiveAsync`, `DeletePlanAsync` as a soft delete, and `IsPlanReferencedAsync`
- [x] Register the service as scoped in `Program.cs`

### 3.3 Tests

- [x] Plan reads exclude other users' plans and soft-deleted plans (WDM-TEST-01)
- [x] Applying a template appends in order with prescription and retains overlapping exercises (WDM-TEST-02)
- [x] Update and delete are rejected for a plan the caller does not own (WDM-TEST-03)
- [x] Soft delete hides the plan while leaving it retrievable in the database (WDM-TEST-04)
- [x] `IsPlanReferencedAsync` returns true once a workout references the plan

---

## Phase 4: Plan Builder Surfaces

Delivers the user-facing plan experience. At the end of this phase a user can build, edit, retire and delete
plans, though nothing can yet be performed from one.

### 4.1 Plans List

- [x] Create `Pages/Plans/Index.cshtml` and its page model with `[Authorize]`
- [x] List plans, visually distinguishing active from inactive, with reactivation for inactive ones (WDM-UI-03)
- [x] Wire soft delete through `DeletePlanAsync`
- [x] Present plans and templates so their distinct purposes are evident (WDM-UI-11)

### 4.2 Plan Builder

- [x] Create `Pages/Plans/Create.cshtml` and its page model with `[Authorize]`
- [x] Support applying one or more templates, choosing from personal and built-in (WDM-UI-01)
- [x] Support adding individual exercises, reordering, editing the prescription and removing exercises
- [x] Display a visual indicator on duplicated entries without merging or removing them (WDM-UI-02)
- [x] Present target sets, reps, duration and distance inputs in the user's display units
- [x] Declare validation via DataAnnotations only, projected by the existing tag helper (WDM-UI-13)

### 4.3 Edit Warning

- [x] Call `IsPlanReferencedAsync` before saving an edit
- [x] Present a confirmation identifying that the plan has been used, then proceed on confirmation (WDM-18,
      WDM-UI-04)

### 4.4 Tests

- [x] Page-level behaviour is covered indirectly through service tests, consistent with the repository's
      service-only test scope
- [x] Add service tests for any query added to support the builder

---

## Phase 5: Guided Workout Execution

The central vertical slice. Connects plans to workouts, introduces exercise status, adds duration and distance to
set logging, and retires the template-to-workout path.

### 5.1 Schema

- [x] Add `Workout.WorkoutPlanId` as a nullable FK with `DeleteBehavior.Restrict` (WDM-20) — landed in
      Phase 3, which needed it for `IsPlanReferencedAsync`
- [x] Add `WorkoutExercise.Status`, required, max 20, default `Pending` (WDM-50)
- [x] Create the `WorkoutExerciseStatuses` constants class alongside `WorkoutExercise` (§4.2.1)
- [x] Add `Set.Distance` at precision `(10,4)` (WDM-27)
- [x] Add migration `AddPlanReferenceAndExerciseStatus`

### 5.2 Starting From a Plan

- [x] Implement `StartWorkoutFromPlanAsync`, verifying ownership, active state and not-deleted (WDM-22)
- [x] Materialize one `Pending` `WorkoutExercise` per plan exercise, in plan order, copying identity and order
      only — never the prescription (WDM-21, WDM-24)
- [x] Remove `StartWorkoutFromTemplateAsync` (WDM-02)
- [x] Replace the `templateId` route input on `/Workouts/Start` with `planId`
- [x] Update the dashboard entry point from "Start from Template" to "Start from Plan"

### 5.3 Guidance and Status

- [x] Read the plan's prescription live for display; do not snapshot it (WDM-26)
- [x] Show each planned exercise with its prescription alongside the logging inputs, visually distinguishing
      pending, skipped and performed (WDM-UI-07)
- [x] Add `SetExerciseStatusAsync`, rejecting `Skipped` when sets exist (WDM-52, WDM-53)
- [x] Add the status control to each exercise in the logging flow (WDM-UI-14, WDM-UI-15)
- [x] Reset a `Skipped` status to `Pending` when a set is recorded against it (WDM-53)
- [x] Render status on completed workout views so skipped exercises remain visible in history (WDM-UI-16)
- [x] Add the focused one-exercise-at-a-time presentation, the default for an in-progress workout and
      reversible, advancing on an answer (WDM-UI-17)
- [x] Present an input row per outstanding prescribed set, pre-filled and submitted together (WDM-UI-18)
- [x] Withhold the effort rating in the flow until a set is recorded (WDM-UI-19)
- [x] Swap the workout form in place with htmx for every action taken while training, keeping the plain
      form post as the fallback (WDM-UI-20)
- [x] Flatten the exercise card to one surface, with recorded and outstanding sets sharing a single
      table, one line per set down to 320px (WDM-UI-21)
- [x] Supply a blank set's RPE from the exercise's effort rating, tracking which values were supplied
      that way so re-rating and clearing move only those (WDM-58)
- [x] Replace the `confirm()` on completing and cancelling with a centred modal (WDM-UI-22)

### 5.4 Set Logging

- [x] Extend `LogSetAsync` with optional duration and distance
- [x] Add duration and distance inputs to the logging UI, showing only the inputs relevant to the exercise where
      determinable (WDM-UI-09)
- [x] Label every measurement input and rendered value with its display unit (WDM-UI-10)

### 5.5 Derived Statistics Correction

- [x] Change the `CompleteWorkoutAsync` guard to require at least one performed exercise (WDM-54, WDM-56)
- [x] Apply the performed rule to muscle-group focus, recently-performed detection and last-performed lookups in
      `WorkoutSuggestionService` (WDM-55)
- [x] Apply the performed rule to any `AnalyticsService` metric that counts or groups `WorkoutExercise` rows
- [x] Point workout suggestions at plans instead of templates and remove
      `TemplateService.GetActiveTemplatesAsync` (WDM-28)

### 5.6 Tests

- [x] Starting from a plan materializes `Pending` rows in order with no prescription copied (WDM-TEST-06)
- [x] Starting from an inactive, soft-deleted or unowned plan returns `null` (WDM-TEST-05)
- [x] Recorded sets are unchanged after the plan is edited (WDM-TEST-07)
- [x] `Skipped` is rejected when sets exist, and logging a set resets a skipped status (WDM-TEST-14)
- [x] Skipped and untouched exercises are excluded from focus, recently-performed and last-performed results
      (WDM-TEST-15)
- [x] A workout of only skipped or pending exercises cannot be completed; one performed exercise suffices
      (WDM-TEST-16)

---

## Phase 6: Built-In Template Catalog

Seeds the content that makes the feature usable from a cold start, plus the exercises that content requires.

### 6.1 Exercise Library Additions

- [x] Seed the additional exercises listed in §4.3.1, including the `Mobility` category entries
- [x] Confirm `IsWeightLoaded` yields `TracksOneRepMax == false` for every new entry (WDM-44)
- [x] Add the `Resistance Band` equipment value used by band movements

### 6.2 Catalog Seeding

- [x] Replace the whole-table `AnyAsync()` guard for templates with a per-entry check by `CatalogKey` (WDM-42)
- [x] Seed the 25 catalog entries defined in §4.3.2, resolving exercises by name
- [x] Fail startup on an unresolvable exercise name, consistent with existing seeding behaviour (WDM-43,
      WDM-NF-03)
- [x] Express seeded distance prescriptions in canonical kilometres
- [x] Confirm existing entries are left unmodified on re-seed

### 6.3 Anonymous Catalog Page

- [x] Create `Pages/Templates/Catalog.cshtml` without `[Authorize]` (WDM-45, D5)
- [x] Render built-in templates only, disclosing no user-owned data (WDM-SEC-05)
- [x] Link the catalog from the templates list and from the plan builder's template picker

### 6.4 Tests

- [x] Seeding inserts missing entries, leaves existing ones unmodified, and is a no-op when fully seeded
      (WDM-TEST-09)
- [x] Every catalog entry resolves to seeded exercises
- [x] The catalog query returns only ownerless templates

---

## Phase 7: Cross-Cutting Validation and Documentation

### 7.1 Authorization and Scoping Sweep

- [x] Verify every plan query filters on `userId` inside the query expression (WDM-SEC-01, WDM-NF-01)
- [x] Verify start-from-plan checks ownership before creating or modifying a workout (WDM-SEC-02)
- [x] Verify template reads that intentionally include built-ins express that intent explicitly rather than
      repeating an inline null check (WDM-SEC-03)
- [x] Verify page models translate `null`/`false` into `NotFound()` and perform no ownership checks themselves
      (WDM-SEC-06)
- [x] Verify the anonymous catalog page exposes no user data

### 7.2 Presentation Consistency

- [x] Verify new surfaces use existing `site.css` component classes and support light and dark themes
      (WDM-UI-12)
- [x] Verify responsive behaviour and touch target sizing on the plan builder and status controls
- [x] Verify every measurement rendered carries its display unit (WDM-UI-10)

### 7.3 Full Validation

- [x] Run the full test suite with `-p:SkipTailwindBuild=true`
- [x] Delete `FitTracker.db`, restart, and confirm migrations and seeding rebuild a working database
- [x] Walk the end-to-end path: browse catalog → copy a template → build a plan → perform a guided workout with
      a skip and an effort rating → complete → confirm history, PRs and analytics

### 7.4 Documentation

- [x] Update `Implementation.md`: revise the Workout Templates section and add Plans, exercise status and
      canonical units
- [x] Update `UserGuide.md` for plans, guided workouts, exercise status and the built-in catalog
- [x] Update `ManualTestChecklist.md` with plan, status and unit-conversion checks
- [x] Update `CLAUDE.md` where the architecture notes describe templates starting workouts, and record the
      canonical-units convention

---

## References

- [WorkoutDomainModelSpecification.md](WorkoutDomainModelSpecification.md) — Formal specification and
  requirement IDs
- [WorkoutDomainModelIdea.md](WorkoutDomainModelIdea.md) — Idea document, decision record and the full
  25-template catalog appendix
- [Implementation.md](Implementation.md) — Authoritative project-wide feature checklist
- [TailwindGuidelines.md](TailwindGuidelines.md) — Styling conventions for new surfaces
- `CLAUDE.md` — Repository architecture, scoping, seeding and testing conventions
