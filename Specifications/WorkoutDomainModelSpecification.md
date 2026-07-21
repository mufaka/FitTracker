# Workout Domain Model Specification — Templates, Plans, and Workouts

## 1. Introduction

### 1.1 Purpose

This specification defines the separation of *planned* training from *performed* training in FitTracker, and the
supporting capabilities that separation requires: composable workout templates, a new workout plan layer, guided
workout execution that records actual work, canonical measurement units, and a seeded catalog of built-in
templates.

It derives from `WorkoutDomainModelIdea.md` and formalizes the decisions recorded there (A–U and catalog
decisions 1–10).

### 1.2 Scope

**In scope**

- Widening `WorkoutTemplate` semantics from "a full workout" to "a reusable grouping of exercises".
- A new `WorkoutPlan` layer assembled from templates and individually chosen exercises.
- Guided workout execution driven by a plan, recording only work actually performed.
- Duration and distance on templates, plans and sets.
- Canonical storage of weight and distance with conversion at the presentation boundary.
- Built-in (ownerless) templates and a seeded catalog of 25, plus the exercise-library additions they require.

**Out of scope**

- Scheduling in any form — placing plans on dates, recurrence, reminders, iCal, and calendar integration. These
  are deferred to a separate idea and specification (idea decision M).
- Periodization and multi-week programming.
- Sharing templates or plans between users.
- Prescribing starting weights.
- Renaming or restructuring `Workout`, `WorkoutExercise` or `Set` beyond the additions specified here.

### 1.3 Definitions and Acronyms

| Term | Definition |
|------|-----------|
| Template | A reusable grouping of exercises (`WorkoutTemplate`). May be a warm-up, an accessory block, or a complete session. Exists only to help build plans. |
| Plan | A reusable recipe (`WorkoutPlan`) assembled from one or more templates plus individually chosen exercises. Creating a plan does not start training. |
| Workout | The record of training actually performed (`Workout`, existing entity). Unchanged in meaning by this specification. |
| Built-in template | A template with no owner (`UserId IS NULL`), seeded by the application and visible to every user. |
| Personal template | A template owned by a user (`UserId` set). |
| Prescription | The target sets, reps, duration and distance declared by a template or plan for an exercise. |
| Canonical unit | The single unit in which a measurement is persisted, independent of any user's display preference. |
| Display unit | The unit a measurement is rendered in and entered in, derived from the user's preference. |
| Materialize | Create a `WorkoutExercise` row at the moment the user first records work for that exercise. |

### 1.4 Design Principles

- **The workout records reality.** A `Workout` contains what the user actually did — including an explicit record
  that a planned exercise was skipped. Intent lives on the plan; outcome lives on the workout.
- **Plans guide; they do not dictate.** A plan is presented during a workout as reference; deviation is normal.
- **Planned work can never be counted as performed work.** Plans hold no set data, so no aggregate can read them
  by mistake.
- **Templates are ingredients, plans are recipes, workouts are meals eaten.** Each layer has one job.
- **User scoping lives in the query.** Every user-facing service method filters on `userId` inside the query and
  returns `null`/`false` when nothing matches, per repository convention.
- **Units are a presentation concern.** Stored values are canonical; conversion happens at the boundary and
  nowhere else.

### 1.5 Specification-Level Decisions

The idea document deferred the following details to this specification. Each is resolved here, with rationale, so
that the decision is visible and reversible rather than implicit.

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | **Canonical units are metric**: kilograms for weight, kilometres for distance. | Conversion runs in one direction from a single base; metric avoids compounding fractional constants. |
| D2 | **Canonical weight and distance are stored with 4 decimal places.** `Set.Weight` precision changes from `(10,2)` to `(10,4)`. | At 2 decimals a lbs→kg→lbs round trip drifts (45 lbs → 20.41 kg → 44.99 lbs). 4 decimals keeps round trips stable at the 2-decimal display precision. |
| D3 | **Soft delete is a flag distinct from `IsActive`.** `WorkoutPlan` carries both `IsActive` and `IsDeleted`. | The two states behave differently: a retired plan must stay visible to be reactivated (decision Q), a deleted one must disappear. |
| D4 | **`BodyMeasurement.Weight` is included in the canonical-unit change; circumference fields are not.** | Body weight shares the existing lbs/kg preference and the same latent defect. Circumferences would require a new length preference, which no decision authorizes; deferred to §12. |
| D5 | **The anonymous catalog is a separate page** (`/Templates/Catalog`) rather than relaxed authorization on `/Templates/Index`. | Repository convention is per-page authorization; `/Templates/Index` shows personal data and must stay `[Authorize]`. |
| D6 | **Built-in templates are identified by a stable `CatalogKey`**, and seeding inserts missing entries only. | Satisfies decision 9 (new templates reach existing databases) without overwriting anything. |
| D7 | **Warm-up and activation movements are seeded under a new `Mobility` category.** | The existing `Strength`/`Core`/`Cardio` categories have no fit; a distinct category also keeps `TracksOneRepMax` false via the existing `IsWeightLoaded` rule. |
| D8 | **The status vocabulary is `Pending`, `Skipped`, `Easy`, `Medium`, `Hard`**, defaulting to `Pending`. | Rows are created before the user has touched them, so a fifth value is required to distinguish "not yet addressed" from "deliberately skipped". The remaining four are as specified by the human. |
| D9 | **Status is persisted as a string against a `WorkoutExerciseStatuses` constants class**, not as a CLR enum. | The codebase contains no enums; `AchievementCriteria` and `ChallengeGoalTypes` establish string constants as the convention for closed value sets. |
| D10 | **Exercise status is distinct from `Set.RPE` and does not replace it.** | RPE is per-set, numeric and optional; status is per-exercise, coarse, and additionally carries completion state. Both may be recorded. |

---

## 2. Technology Additions

None. This specification introduces no new packages, frameworks or client libraries. All work uses the existing
.NET 10 / EF Core / Razor Pages / Tailwind / Alpine stack.

---

## 3. Functional Requirements

### 3.1 Workout Templates

| ID | Requirement |
|----|------------|
| WDM-01 | A template shall be a reusable, ordered grouping of one or more exercises, of any size, and shall not be required to constitute a complete workout. |
| WDM-02 | A template shall exist solely to assist in building plans. The system shall provide no path from a template directly to a workout. |
| WDM-03 | A template exercise shall carry a prescription of default sets, default reps, default duration and default distance, each independently optional except sets and reps which retain their existing defaults. |
| WDM-04 | A template with `UserId IS NULL` shall be a built-in template, visible to every user. |
| WDM-05 | Template read operations shall return the calling user's own templates together with all built-in templates. |
| WDM-06 | Template create, update and delete operations shall apply only to templates owned by the calling user, and shall return `null`/`false` for a built-in template or one owned by another user. |
| WDM-07 | A user shall be able to copy any visible template, built-in or personal, into a new template that they own and may then modify. |
| WDM-08 | Deleting a personal template shall remove it permanently; templates are not soft-deleted, as nothing references a template once a plan has been built. |
| WDM-09 | The template list shall be filterable by ownership: personal only, built-in only, or all. |

### 3.2 Workout Plans

| ID | Requirement |
|----|------------|
| WDM-10 | A plan shall be a reusable, user-owned, ordered list of exercises with a name, an optional description, and a prescription per exercise. |
| WDM-11 | A plan shall be assembled from any number of templates, any number of individually selected exercises, or both. |
| WDM-12 | Applying a template to a plan shall append every exercise the template contains, in the template's order, copying its prescription. |
| WDM-13 | Applying a template shall not merge, deduplicate or drop an exercise that already exists in the plan. Duplicate entries shall be retained exactly as contributed. |
| WDM-14 | The system shall record no association between a plan and the templates it was built from. |
| WDM-15 | A plan shall be editable after creation, including adding, removing, reordering and re-prescribing exercises. |
| WDM-16 | A plan shall have an active state. An inactive plan shall not be selectable to guide a workout, and shall remain visible so that it can be reactivated. |
| WDM-17 | A plan shall be deletable only by soft delete. A soft-deleted plan shall be excluded from every read operation and shall not be reactivatable through the plan list. |
| WDM-18 | Saving an edit to a plan that one or more workouts already reference shall warn the user before the edit is applied. The edit shall proceed on confirmation, and no version or copy shall be retained. |
| WDM-19 | A plan shall hold no set, weight or performance data of any kind. |

### 3.3 Performing a Workout

| ID | Requirement |
|----|------------|
| WDM-20 | A workout shall optionally reference the plan that guided it. A workout with no plan reference shall be a valid ad-hoc workout, and shall behave exactly as workouts do today. |
| WDM-21 | Starting a workout from a plan shall create the workout with a reference to that plan and shall materialize a `WorkoutExercise` row for every exercise the plan contains, in the plan's order, each with status `Pending`. |
| WDM-22 | Starting a workout from a plan shall be permitted only when the plan belongs to the calling user, is active, and is not soft-deleted; otherwise the operation shall return `null`. |
| WDM-23 | During a workout guided by a plan, the system shall present the plan's exercises in the plan's order, together with their prescription, as guidance. |
| WDM-24 | Only the exercise identity and its order shall be copied from the plan to the workout. The prescription shall remain on the plan and shall not be duplicated onto `WorkoutExercise`. |
| WDM-25 | The user shall be able to deviate freely from the plan during a workout: performing more or fewer sets than prescribed, different weights or reps, skipping a prescribed exercise, removing it from the workout entirely, or adding an exercise the plan does not contain. |
| WDM-26 | The prescription displayed during a workout shall be read from the plan at the time of display. Editing the plan later shall change what past workouts display as guidance; it shall never alter recorded sets. |
| WDM-27 | A set shall record duration and distance in addition to weight, reps and RPE, each independently optional. |
| WDM-28 | Workout suggestions shall reference plans rather than templates, since templates can no longer start a workout. |
| WDM-29 | Completion, personal-record detection, achievement evaluation and challenge evaluation shall continue to operate on workouts alone, unchanged by the introduction of plans. |

### 3.4 Exercise Status and Effort

| ID | Requirement |
|----|------------|
| WDM-50 | Every `WorkoutExercise` shall carry a status drawn from `Pending`, `Skipped`, `Easy`, `Medium` and `Hard`, defaulting to `Pending` on creation. |
| WDM-51 | `Pending` shall mean the exercise has not yet been addressed; `Skipped` shall mean the user deliberately did not perform it; `Easy`, `Medium` and `Hard` shall mean the user performed it and shall record their perceived effort. |
| WDM-52 | The user shall be able to set and change the status of any exercise in an in-progress workout, and the status shall be independent of whether sets have been recorded. |
| WDM-53 | `Skipped` shall not be selectable for an exercise that has recorded sets. Recording a set against an exercise whose status is `Skipped` shall reset that status to `Pending`. |
| WDM-54 | An exercise shall be treated as performed, for the purpose of any derived statistic, when it has at least one recorded set or a status of `Easy`, `Medium` or `Hard`. Exercises that are `Skipped`, or `Pending` with no recorded sets, shall be excluded. |
| WDM-55 | Muscle-group focus, recently-performed exercise detection and last-performed lookups shall apply the WDM-54 rule, so that a skipped or untouched exercise is never counted as trained. |
| WDM-56 | A workout shall be completable only when at least one of its exercises satisfies WDM-54. |
| WDM-58 | A set recorded without an RPE shall take the RPE its exercise's effort rating implies — `Easy` 5, `Medium` 7, `Hard` 9 — whether the rating is given before or after the set. A value the user entered shall never be overwritten. Changing the rating shall move only the values it supplied, and a status implying no effort shall clear them; each set shall therefore record whether its RPE was supplied this way. |

### 3.5 Measurement Units

| ID | Requirement |
|----|------------|
| WDM-30 | Weight shall be persisted in kilograms and distance in kilometres, regardless of the user's display preference. |
| WDM-31 | Every value entered by a user in a display unit shall be converted to the canonical unit before persistence. |
| WDM-32 | Every persisted value rendered to a user shall be converted from the canonical unit to that user's display unit. |
| WDM-33 | Conversion shall be implemented in a single dedicated component. No page model, view or service other than that component shall convert or format a stored measurement. |
| WDM-34 | A weight or distance value displayed, then re-submitted unmodified, shall not alter the persisted value at display precision. |
| WDM-35 | Progressive-overload weight increments shall be computed in the user's display unit and converted to canonical before use. The increment thresholds shall be defined per display unit rather than against the raw stored value. |
| WDM-36 | `BodyMeasurement.Weight` shall be subject to the same canonical storage and conversion rules as set weight. |
| WDM-37 | Duration shall be persisted in whole seconds and is unit-independent; no conversion applies. |

### 3.6 Built-In Template Catalog

| ID | Requirement |
|----|------------|
| WDM-40 | The application shall seed a catalog of 25 built-in templates: 9 gym, 6 home, 5 outdoor and 5 warm-up. |
| WDM-41 | Each built-in template shall carry a stable catalog key, unique across templates, that is null for personal templates. |
| WDM-42 | Seeding shall insert each catalog entry that is absent by catalog key, on every application start, and shall leave existing entries unmodified. |
| WDM-43 | Catalog seed data shall resolve exercises by name. An entry referencing an unknown exercise name shall cause startup to fail, consistent with the existing seeding philosophy of failing loudly. |
| WDM-44 | The application shall seed the additional exercises the catalog requires, including mobility and activation movements under a `Mobility` category. |
| WDM-45 | The built-in catalog shall be readable without authentication. |
| WDM-46 | No user shall be able to modify, deactivate or delete a built-in template. |

---

## 4. Data Model

### 4.1 New Entities

#### WorkoutPlan

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK | Primary key |
| UserId | string | Required, FK → `ApplicationUser` | Owner; plans are always user-owned |
| Name | string | Required, max 100 | Plan name |
| Description | string? | Max 500 | Optional description |
| IsActive | bool | Default `true` | Inactive plans cannot guide a workout (WDM-16) |
| IsDeleted | bool | Default `false` | Soft delete (WDM-17, D3) |
| CreatedAt | DateTime | Default `UtcNow` | Creation timestamp, UTC |
| Exercises | `ICollection<WorkoutPlanExercise>` | | Ordered exercise list |

**Indexes**
- `IX_WorkoutPlans_UserId` on `UserId`

**Relationships**
- `WorkoutPlan.User` → `ApplicationUser.WorkoutPlans`, `DeleteBehavior.Cascade`
- `ApplicationUser` gains a `WorkoutPlans` collection

#### WorkoutPlanExercise

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK | Primary key |
| WorkoutPlanId | int | Required, FK → `WorkoutPlan` | Owning plan |
| ExerciseId | int | Required, FK → `Exercise` | Prescribed exercise |
| Order | int | | Position within the plan, 1-based |
| TargetSets | int? | Range 1–20 | Prescribed set count |
| TargetReps | int? | Range 1–50 | Prescribed rep count |
| TargetDurationSeconds | int? | Range 1–86400 | Prescribed duration, seconds |
| TargetDistance | decimal? | Precision (10,4) | Prescribed distance, canonical km |
| Notes | string? | Max 300 | Free-text guidance |

**Relationships**
- `WorkoutPlanExercise.Plan` → `WorkoutPlan.Exercises`, `DeleteBehavior.Cascade`
- `WorkoutPlanExercise.Exercise` → `Exercise.WorkoutPlanExercises`, `DeleteBehavior.Restrict`

### 4.2 Modified Entities

#### WorkoutTemplate

| Property | Type | Description |
|----------|------|-------------|
| UserId | string? | Becomes nullable; `null` denotes a built-in template (WDM-04). The FK relationship becomes optional. |
| CatalogKey | string? | New. Max 100, unique where not null. Stable identity for seeded entries (WDM-41, D6). |

#### WorkoutTemplateExercise

| Property | Type | Description |
|----------|------|-------------|
| DefaultSets | int? | Becomes nullable; a template need not prescribe sets |
| DefaultReps | int? | Becomes nullable |
| DefaultDurationSeconds | int? | New. Prescribed duration in seconds |
| DefaultDistance | decimal? | New. Prescribed distance, canonical km, precision (10,4) |

#### Workout

| Property | Type | Description |
|----------|------|-------------|
| WorkoutPlanId | int? | New. Optional reference to the guiding plan (WDM-20). `DeleteBehavior.Restrict`; plans are never physically deleted, so the reference stays valid. |

#### WorkoutExercise

| Property | Type | Description |
|----------|------|-------------|
| Status | string | New. Required, max 20, default `Pending`. One of the `WorkoutExerciseStatuses` values (WDM-50, D8, D9) |

#### Set

| Property | Type | Description |
|----------|------|-------------|
| Weight | decimal? | Precision changes from `(10,2)` to `(10,4)`; now canonical kilograms (D1, D2) |
| Distance | decimal? | New. Canonical kilometres, precision (10,4) |
| Duration | int? | Existing field, previously never written; now populated by set logging (WDM-27) |

#### BodyMeasurement

| Property | Type | Description |
|----------|------|-------------|
| Weight | decimal? | Now canonical kilograms (WDM-36, D4). Precision (10,4). |

#### Exercise

| Property | Type | Description |
|----------|------|-------------|
| WorkoutPlanExercises | `ICollection<WorkoutPlanExercise>` | New inverse navigation |

### 4.3 Seeded Configuration

#### 4.2.1 Status Constants

`WorkoutExerciseStatuses`, defined alongside `WorkoutExercise` and following the convention established by
`AchievementCriteria` and `ChallengeGoalTypes`:

| Constant | Value | Meaning |
|----------|-------|---------|
| Pending | `"Pending"` | Not yet addressed; the default on creation |
| Skipped | `"Skipped"` | Deliberately not performed |
| Easy | `"Easy"` | Performed, low perceived effort |
| Medium | `"Medium"` | Performed, moderate perceived effort |
| Hard | `"Hard"` | Performed, high perceived effort |

#### 4.3.1 Additional Exercises

Seeded alongside the existing library. `TracksOneRepMax` is derived by the existing `IsWeightLoaded` rule, which
yields `false` for every entry below.

| Name | Category | Equipment |
|------|----------|-----------|
| Bodyweight Squat | Strength | Bodyweight |
| Glute Bridge | Strength | Bodyweight |
| Walking | Cardio | None |
| Sprint Intervals | Cardio | None |
| Jumping Jacks | Cardio | Bodyweight |
| High Knees | Cardio | Bodyweight |
| Butt Kicks | Cardio | Bodyweight |
| Arm Circles | Mobility | Bodyweight |
| Leg Swings | Mobility | Bodyweight |
| Hip Circles | Mobility | Bodyweight |
| Ankle Circles | Mobility | Bodyweight |
| World's Greatest Stretch | Mobility | Bodyweight |
| Cat-Cow | Mobility | Bodyweight |
| Scapular Push-Ups | Mobility | Bodyweight |
| Scapular Pull-Ups | Mobility | Bodyweight |
| Wall Slides | Mobility | Bodyweight |
| Dead Hang | Mobility | Bodyweight |
| Band Pull-Aparts | Mobility | Resistance Band |

#### 4.3.2 Built-In Template Catalog

25 entries, each with a stable `CatalogKey`. Composition is defined in `WorkoutDomainModelIdea.md` § Appendix.

| Group | Count | Catalog keys |
|-------|-------|--------------|
| Gym | 9 | `gym-full-body-a`, `gym-full-body-b`, `gym-push`, `gym-pull`, `gym-legs`, `gym-upper`, `gym-lower`, `gym-arms-shoulders`, `gym-machine-circuit` |
| Home | 6 | `home-bodyweight-full-body`, `home-core-express`, `home-dumbbell-full-body`, `home-dumbbell-upper`, `home-rack-full-body`, `home-rack-lower` |
| Outdoor | 5 | `outdoor-easy-run`, `outdoor-run-intervals`, `outdoor-park-calisthenics`, `outdoor-conditioning-circuit`, `outdoor-long-ride` |
| Warm-up | 5 | `warmup-general-dynamic`, `warmup-upper-push`, `warmup-lower-squat-hinge`, `warmup-pull`, `warmup-run` |

Distance prescriptions in seed data are expressed in canonical kilometres.

---

## 5. Service Layer Design

### 5.1 IWorkoutPlanService (new)

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| GetPlansAsync | `string userId` | `List<WorkoutPlan>` | All non-deleted plans for the user, active and inactive |
| GetActivePlansAsync | `string userId, int count` | `List<WorkoutPlan>` | Active, non-deleted plans; replaces template-based suggestion input |
| GetPlanEditorAsync | `int planId, string userId` | `PlanEditorModel?` | Editor projection; `null` when not found or not owned |
| SavePlanAsync | `string userId, PlanEditorModel model` | `int?` | Creates or updates; `null` when an exercise id is unknown or the plan is not owned |
| ApplyTemplateAsync | `int templateId, string userId, PlanEditorModel model` | `PlanEditorModel?` | Appends a visible template's exercises to the in-progress editor model (WDM-12, WDM-13) |
| SetPlanActiveAsync | `int planId, string userId, bool isActive` | `bool` | Retires or reactivates a plan |
| DeletePlanAsync | `int planId, string userId` | `bool` | Soft delete |
| IsPlanReferencedAsync | `int planId, string userId` | `bool` | Whether any workout references the plan; drives the WDM-18 warning |

### 5.2 ITemplateService (modified)

| Method | Change |
|--------|--------|
| GetTemplatesAsync | Read predicate widens to `t.UserId == userId \|\| t.UserId == null`; gains an ownership filter parameter (WDM-05, WDM-09) |
| GetCatalogAsync | New. Returns built-in templates only; requires no user (WDM-45) |
| GetTemplateEditorAsync | Read widens to include built-ins so they can be previewed and copied |
| SaveTemplateAsync | Unchanged predicate — remains `t.UserId == userId`, rejecting built-ins (WDM-06) |
| DeleteTemplateAsync | Unchanged predicate; built-ins are unreachable (WDM-06, WDM-46) |
| CopyTemplateAsync | New. Copies any visible template into a new template owned by the caller (WDM-07) |
| GetActiveTemplatesAsync | Removed. Superseded by `IWorkoutPlanService.GetActivePlansAsync` (WDM-28) |

### 5.3 IWorkoutService (modified)

| Method | Change |
|--------|--------|
| StartWorkoutFromTemplateAsync | Removed (WDM-02) |
| StartWorkoutFromPlanAsync | New. `(int planId, string userId)` → `Workout?`. Creates or returns the day's workout, sets `WorkoutPlanId`, and materializes a `Pending` `WorkoutExercise` per plan exercise (WDM-21, WDM-22) |
| LogSetAsync | Gains optional duration and distance parameters; resets a `Skipped` status to `Pending` (WDM-27, WDM-53) |
| SetExerciseStatusAsync | New. `(int workoutExerciseId, string userId, string status)` → `bool`. Sets the status, rejecting `Skipped` when sets exist (WDM-52, WDM-53) |
| CompleteWorkoutAsync | Completion guard changes from "has any exercise" to "has at least one performed exercise" per WDM-54 and WDM-56 |
| GetProgressiveOverloadSuggestionsAsync | Increment computation becomes unit-aware (WDM-35) |
| StartWorkoutAsync, CancelWorkoutAsync, AddExerciseToWorkoutAsync, RemoveSetAsync, RemoveExerciseAsync, CalculateWorkoutVolumeAsync | Unchanged |

### 5.4 Unit Conversion (new)

A single component owns all conversion between canonical and display units (WDM-33).

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| ToCanonicalWeight | `decimal value, string displayUnit` | `decimal` | Display unit → kilograms |
| ToDisplayWeight | `decimal value, string displayUnit` | `decimal` | Kilograms → display unit |
| ToCanonicalDistance | `decimal value, string displayUnit` | `decimal` | Display unit → kilometres |
| ToDisplayDistance | `decimal value, string displayUnit` | `decimal` | Kilometres → display unit |
| WeightIncrement | `decimal displayWeight, string displayUnit` | `decimal` | Progressive-overload step in the display unit (WDM-35) |

Distance display unit is derived from the existing weight preference: `lbs` implies miles, `kg` implies
kilometres. No new user preference field is introduced.

### 5.5 Existing Service Reuse

| Service | Relationship to this specification |
|---------|-----------------------------------|
| `IPersonalRecordService` | Unchanged. Continues to read workouts and sets. Benefits from canonical units, which make comparisons unit-safe. |
| `IAchievementService`, `IChallengeService` | Unchanged. Operate on workouts only. |
| `IAnalyticsService` | Aggregates derived from sets are unaffected. Any metric that counts or groups `WorkoutExercise` rows shall apply the WDM-54 performed rule, since rows now exist for exercises that were never performed. |
| `IExportService`, `IOneRepMaxService` | Logic unchanged; presentation converts to display units. |
| `IWorkoutSuggestionService` | Modified to suggest plans instead of templates (WDM-28), and to apply the WDM-54 rule when deriving recent muscle-group focus, recently-performed exercises and last-performed dates (WDM-55). |
| `IExerciseService` | Unchanged. |

---

## 6. Page and Route Design

| Route | Authorization | Purpose |
|-------|---------------|---------|
| `/Plans/Index` | `[Authorize]` | List the user's plans; activate, deactivate, delete |
| `/Plans/Create` | `[Authorize]` | Build and edit a plan: apply templates, add exercises, reorder, prescribe |
| `/Templates/Index` | `[Authorize]` | List personal and built-in templates with the ownership filter (WDM-09) |
| `/Templates/Catalog` | Anonymous | Read-only built-in catalog (WDM-45, D5) |
| `/Templates/Create` | `[Authorize]` | Existing template builder, unchanged in authorization |
| `/Workouts/Start` | `[Authorize]` | Performing a workout; accepts an optional `planId` in place of the former `templateId` |

---

## 7. Changes to Existing Requirements

| Area | Change |
|------|--------|
| Workout Templates (`Implementation.md` § Workout Templates) | "Start from Template" is withdrawn. Templates build plans; plans start workouts (WDM-02). |
| Dashboard | The "Start from Template" entry point becomes "Start from Plan". |
| Workout start | `templateId` route input is replaced by `planId`. |
| Set logging | Gains duration and distance inputs; `Set.Duration` ceases to be unused. |
| Workout completion | The guard in `CompleteWorkoutAsync` changes from "the workout has at least one exercise" to "the workout has at least one performed exercise". Eager materialization would otherwise satisfy the existing guard trivially, allowing a workout in which nothing was done to be completed. |
| Exercise-derived metrics | `AnalyticsService` and `WorkoutSuggestionService` currently treat the presence of a `WorkoutExercise` row as evidence the exercise was performed. That inference is no longer valid and both shall apply the WDM-54 rule (WDM-55). |
| Units | `PreferredUnits` changes from a display label appended to raw stored values into a genuine conversion preference. This corrects existing behaviour in which switching preference relabels historical data rather than converting it. |
| Template ownership | `WorkoutTemplate.UserId` becomes nullable, introducing a second legitimate read predicate for that entity only. |
| Seeding | Template seeding uses per-entry checks by catalog key rather than the whole-table `AnyAsync()` guard used by other seed blocks (WDM-42). |

No previously specified behaviour of `Workout`, `WorkoutExercise`, personal records, achievements, challenges or
analytics is altered by this specification.

---

## 8. Non-Functional Requirements

| ID | Requirement |
|----|------------|
| WDM-NF-01 | Template, plan and workout queries shall filter on `userId` within the query expression, consistent with the repository's service-layer convention. |
| WDM-NF-02 | Catalog seeding shall be idempotent and shall complete without error when run repeatedly against an already-seeded database. |
| WDM-NF-03 | Seeding failure shall propagate and prevent application start, consistent with existing startup behaviour. |
| WDM-NF-04 | Plan reads shall use `AsNoTracking()` where the result is not subsequently modified, consistent with existing service implementations. |
| WDM-NF-05 | Persisted dates shall be UTC, and any method depending on the current time shall accept an optional `asOf` parameter defaulting to `DateTime.UtcNow`. |
| WDM-NF-06 | Unit conversion shall be pure and side-effect free, so that it is directly testable without a database. |

---

## 9. UI Requirements

| ID | Requirement |
|----|------------|
| WDM-UI-01 | The plan builder shall allow applying one or more templates, adding individual exercises, reordering, editing the prescription per exercise, and removing exercises. |
| WDM-UI-02 | When a template contributes an exercise the plan already contains, the builder shall display a visual indicator on the duplicated entries. It shall not merge or remove them (WDM-13). |
| WDM-UI-03 | The plan list shall visually distinguish active from inactive plans and shall offer reactivation for inactive ones. |
| WDM-UI-04 | Saving a plan that workouts already reference shall present a confirmation identifying that the plan has been used, before the save proceeds (WDM-18). |
| WDM-UI-05 | The templates list shall present the ownership filter and shall not offer edit or delete affordances for built-in templates. |
| WDM-UI-06 | Template cards shall display the exercises a template contains, not only its name and exercise count, so that built-in templates can be judged before use. |
| WDM-UI-07 | The workout screen, when guided by a plan, shall show each planned exercise with its prescription alongside the logging inputs, and shall visually distinguish pending, skipped and performed exercises. |
| WDM-UI-08 | The workout screen shall allow adding an exercise not present in the plan, and shall not require the user to perform planned exercises in order. |
| WDM-UI-14 | Each exercise in an in-progress workout shall offer a status control for skipping it or recording effort as easy, medium or hard, without requiring the user to leave the logging flow. |
| WDM-UI-15 | The skip option shall be unavailable for an exercise with recorded sets (WDM-53). |
| WDM-UI-16 | Completed workout views shall render each exercise's status, so that a skipped exercise is visible in history rather than silently absent. |
| WDM-UI-17 | An in-progress workout shall present one exercise at a time by default — the next not yet performed or skipped — with its status control as the primary action, a readout of how far through the workout is, and direct access to any other exercise (WDM-UI-08). The full lineup shall stay one action away, and shall be what a completed workout renders. |
| WDM-UI-18 | Where a plan prescribes a number of sets, the logger shall present an input row per set still outstanding, pre-filled with the prescribed reps, duration and distance, submitted together. A row the user empties shall record nothing, and clearing one shall take a single action. |
| WDM-UI-19 | In the focused presentation an effort rating shall be unavailable until the exercise has at least one recorded set, so that advancing cannot record an opinion in place of the work. Skipping shall stay available throughout (WDM-52 is unchanged: the service still accepts a rating on its own). |
| WDM-UI-22 | Confirming an action that ends a workout shall use an in-page modal centred in the viewport, not a browser `confirm()`. It shall state what the action does, be dismissable by Escape and by an explicit decline, and shall not block the action where scripting is unavailable. |
| WDM-UI-21 | The exercise being logged shall be presented as a single surface, not surfaces nested inside one another. Recorded sets and the rows still on offer shall share one table, one line per set at any width, with no line wrapping and no horizontal scrolling of the page. This shall be achieved responsively — one presentation that adapts, not a separate small-screen layout. |
| WDM-UI-20 | Actions taken while training — logging sets, setting a status, stepping between exercises, adding or removing one — shall update the workout in place without reloading the page, so that the rest timer keeps running and the scroll position is not lost. Actions that leave the workout shall remain full navigations, and every action shall still work with JavaScript unavailable. |
| WDM-UI-09 | Set logging shall accept duration and distance in addition to weight, reps and RPE, displaying only the inputs relevant to the exercise where that is determinable. |
| WDM-UI-10 | Every weight and distance rendered shall carry its display unit, and every corresponding input shall be labelled with the unit expected. |
| WDM-UI-11 | Plans and templates shall be presented so their distinct purposes are evident: templates as reusable groupings used to build plans, plans as what a workout is performed from. |
| WDM-UI-12 | New and modified surfaces shall follow the existing Tailwind component classes in `wwwroot/css/site.css` and shall support light and dark themes. |
| WDM-UI-13 | Validation shall be declared through DataAnnotations on page models and projected by the existing `Html5ValidationTagHelper`; rules shall not be restated in markup or JavaScript. |

---

## 10. Testing Requirements

| ID | Requirement |
|----|------------|
| WDM-TEST-01 | Tests shall verify that plan reads exclude other users' plans and soft-deleted plans. |
| WDM-TEST-02 | Tests shall verify that applying a template appends its exercises in order with its prescription, and that overlapping exercises are retained as duplicates. |
| WDM-TEST-03 | Tests shall verify that a plan cannot be updated or deleted by a user who does not own it. |
| WDM-TEST-04 | Tests shall verify that deleting a plan soft-deletes it and that workouts referencing it remain intact and readable. |
| WDM-TEST-05 | Tests shall verify that starting a workout from an inactive, soft-deleted, or unowned plan returns `null`. |
| WDM-TEST-06 | Tests shall verify that starting a workout from a plan materializes one `Pending` `WorkoutExercise` per plan exercise, in the plan's order, with no prescription copied. |
| WDM-TEST-14 | Tests shall verify that `Skipped` is rejected for an exercise with sets, and that logging a set against a skipped exercise resets its status to `Pending`. |
| WDM-TEST-15 | Tests shall verify that skipped and untouched exercises are excluded from muscle-group focus, recently-performed detection and last-performed lookups. |
| WDM-TEST-16 | Tests shall verify that a workout in which every exercise is skipped or pending cannot be completed, and that one performed exercise is sufficient. |
| WDM-TEST-07 | Tests shall verify that a workout retains its recorded sets unchanged after its plan is edited. |
| WDM-TEST-08 | Tests shall verify that template reads include built-in templates and that write operations reject them. |
| WDM-TEST-09 | Tests shall verify that catalog seeding inserts missing entries, leaves existing entries unmodified, and is a no-op on a fully seeded database. |
| WDM-TEST-10 | Tests shall verify weight and distance conversion in both directions, including that a display-precision round trip leaves the stored value unchanged (WDM-34). |
| WDM-TEST-11 | Tests shall verify that progressive-overload increments differ appropriately between display units (WDM-35). |
| WDM-TEST-12 | Tests shall verify that volume, personal records and one-rep-max results are computed from canonical values and are unaffected by a change of display preference. |
| WDM-TEST-13 | Tests shall follow the existing `TestDbContextFactory` pattern, constructing services directly against a shared in-memory SQLite connection. |

---

## 11. Security Considerations

| ID | Consideration |
|----|--------------|
| WDM-SEC-01 | `WorkoutPlan` and `WorkoutPlanExercise` reads and writes shall filter on `userId` inside the query. Absence of that predicate is a data leak, not a compile error. |
| WDM-SEC-02 | Starting a workout from a plan shall verify plan ownership before creating or modifying any workout. |
| WDM-SEC-03 | `WorkoutTemplate` is the only entity in this specification with two legitimate read predicates. Reads that intentionally include built-ins shall express that intent explicitly rather than relying on an inline null check duplicated across call sites. |
| WDM-SEC-04 | Template write paths shall retain the strict ownership predicate so that built-in templates are immutable through any user-reachable route. |
| WDM-SEC-05 | The anonymous catalog page shall expose built-in templates only, and shall disclose no user-owned data. |
| WDM-SEC-06 | Page models shall translate a `null`/`false` service result into `NotFound()` and shall perform no ownership checks of their own. |

---

## 12. Future Considerations

Explicitly out of scope for this specification:

- **Scheduling.** Placing plans on dates, recurrence, reminders, iCal export or import, and how plans and
  workouts share the calendar. Deferred to its own idea and specification (idea decision M).
- **Length units for body measurements.** `Chest`, `Waist`, `Arms` and `Legs` remain unit-labelled without
  conversion; introducing an in/cm preference is deferred (D4).
- **Updating shipped catalog entries.** Seeding inserts missing entries only; correcting a published built-in
  template on an existing database is not covered (D6).
- **Per-side prescriptions.** "10 each side" is not representable; such guidance goes in notes.
- **Exercise grouping.** Supersets and circuits remain unrepresentable; plans are a flat ordered list.
- **Plan provenance.** Plans record no link to their source templates (WDM-14), so template-change propagation
  is not possible without a future schema addition.
- **Plan-versus-actual reporting.** The data to compare intent with performance now exists, and exercise status
  additionally makes adherence and perceived-difficulty trends computable. No report, chart or metric over that
  data is specified here.
- **Reconciling status with RPE.** Per-exercise effort and per-set RPE overlap conceptually (D10). Deriving one
  from the other, or consolidating them, is not specified.
- **Structured template metadata.** Difficulty, goal, equipment and estimated duration remain unmodelled; the
  gym / home / outdoor / warm-up grouping is conveyed by naming alone.

---

## 13. References

- [WorkoutDomainModelIdea.md](WorkoutDomainModelIdea.md) — Source idea document, including the full 25-template
  catalog appendix and the decision record (A–U, 1–10)
- [Idea.md](Idea.md) — Original product concept
- [Implementation.md](Implementation.md) — Authoritative feature checklist, § Workout Templates
- [1RM_Calculation.md](1RM_Calculation.md) — One-rep-max algorithms operating on set weights
- [TailwindGuidelines.md](TailwindGuidelines.md) — Styling conventions for new surfaces
- `CLAUDE.md` — Repository architecture, user-scoping, seeding and testing conventions
