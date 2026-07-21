# Workout Domain Model Idea — Templates, Plans, and Workouts

## Overview

This document started as "seed some default workout templates" and grew into something larger: the exercise that
tried to define what a *template* is exposed that FitTracker has no separate notion of a **planned** workout and a
**performed** one. Today a single `Workout` row is both at once.

The direction now under exploration is a three-layer model:

| Layer | Entity | What it is | Lifetime |
| --- | --- | --- | --- |
| **Template** | `WorkoutTemplate` | A reusable grouping of exercises. Not necessarily a full workout — a warm-up, an accessory block, or a complete session are all templates. | Permanent, reusable |
| **Plan** | `WorkoutPlan` *(new)* | A reusable recipe — a list of things to do, assembled from one or more templates plus individually chosen exercises. Creating one does **not** start anything. | Permanent, reusable |
| **Record** | `Workout` *(existing)* | The record of actually training — the work performed, which may deviate from the plan. | Per performance |

The human's framing: *"A plan is just a list of things to do… It's like a recipe. You can make the cake once or
make it every month, or just on birthdays. It's the Workout that matters."*

**How this got here.** Revisions 1–3 assumed a template *was* a full workout and that picking one immediately
started training. Revision 4 introduced the plan/record split under the names `Workout` (plan) and
`WorkoutSession` (record). Revision 5 inverted that naming: **`Workout` keeps its current meaning as the
performed record**, and the plan becomes a new `WorkoutPlan`. `WorkoutSession` is not introduced — `Workout`
already fills that role. **Revision 6** settles the plan's own semantics (reusable, active/inactive, referenced
rather than copied) and pulls scheduling out into a document of its own.

The naming choice is worth restating because it changes the size of the work by an order of magnitude. Under
revision 4's naming, every consumer of `Workout` — 8 services, ~15 page files, 62 `IsCompleted` references —
had to be re-read against a table that had silently changed meaning. Under this naming, those consumers are
correct as written and stay untouched.

## Human-Provided Direction

Confirmed by the human. Settled unless revisited.

**Domain model**

| # | Decision |
| --- | --- |
| A | **A template is a grouping of exercises**, not necessarily a full workout. |
| B | **Templates are combinable.** A plan may be built from one or more templates *and* individually selected exercises. |
| C | **Creating a plan does not start training.** Plans may be scheduled ahead of time and modified after creation. |
| D | **Performing a plan is a guided progression** that records what was actually done, which may stray from what the plan prescribed. |
| E | **The performed record is the existing `Workout` entity.** It already means "the work that happened" and keeps that meaning. |
| F | The human has accepted that this is an architectural change spanning many areas, and considers it the right way to model the domain. |
| G | **`Workout` stays; the plan is a new `WorkoutPlan`.** Chosen deliberately over renaming `Workout` to `WorkoutPlan` and introducing `WorkoutSession` for the record, because it fits the existing codebase far better. The conceptual "session" is the `Workout` row. |
| H | **Ad-hoc workouts have no plan.** Starting a workout without one is normal and stays as it is today; no throwaway plan is created. |
| I | **A plan is a reusable recipe, not an occurrence.** One plan may guide many workouts, any time the user chooses. |
| J | **Templates are helpers for creating plans — nothing more.** A full-session template creates a plan; a warm-up template contributes part of one. Templates never feed a workout directly. |
| K | **The workout references the plan it followed; the plan is immutable at the time of the workout.** The plan is presented as guidance and nothing is copied onto the workout beyond what the user actually performs. |
| L | **A plan is either active or inactive.** Active plans can be scheduled; inactive ones cannot. No draft or archived states. |
| M | **Scheduling is deferred to its own idea document**, likely built on iCal, along with how plans and workouts share the calendar. |
| N | **The app has never been deployed.** There are no existing users and no production data, so schema changes carry no migration burden — the reset path is deleting `FitTracker.db` and restarting. |
| O | **Editing a plan that workouts have already followed just warns.** No versioning, no freezing — the edit cannot change what the user actually did. |
| P | **Plans are soft-deleted**, never removed outright. |
| Q | **Inactive means fully retired** — not schedulable and not available to guide a new workout — but a user can reactivate a plan at any time. |
| R | **Overlapping exercises from combined templates are kept exactly as they are.** The builder shows a visual indicator of the overlap and the user edits the plan afterwards; nothing is merged or dropped automatically. |
| S | **Weights and distances are stored in one canonical unit and converted for display and input** according to the user's preference. This applies to the existing lbs/kg handling as well, not only to the new distance field. |
| T | **Plans do not record which templates they came from.** The copy is anonymous once made. |
| U | **One specification covers this entire document**; phasing belongs to the implementation plan rather than to separate idea → spec → plan cycles. Scheduling remains its own idea (decision M). |

**Catalog decisions carried forward from revisions 2–3**

| # | Decision |
| --- | --- |
| 1 | Built-in templates are **shared rows visible to everyone** (ownerless), alongside user-owned ones. Users may copy one and modify it. |
| 3 | **Opt-in browsing** — nothing auto-provisioned at signup. |
| 4 | **~25 templates**: a gym / home / outdoor mix, at least one home template assuming a squat rack, and **5 warm-ups**. |
| 5 | **Name and description are enough metadata** for a first pass. |
| 6 | **`DefaultSets` / `DefaultReps` are part of the template** and must reach the plan and the workout. |
| 7 | **Duration and distance fields are added now** — worth tracking generally. |
| 8 | **No per-user hiding**; the templates list offers a filter between personal and global. |
| 9 | **New templates reach existing databases on redeploy.** |
| 10 | **The catalog is browsable without logging in**, like the exercise library. |

Decision 2 from revision 2 ("a built-in is a way to start a workout") is **superseded** by decisions A–E.

## Problem Statement

**The domain conflates the plan with the performance.** A `Workout` row is created at the moment training begins,
accumulates sets as they happen, and is marked `IsCompleted` at the end. That one row is simultaneously the intent
and the record, which produces concrete limitations:

- **Nothing to schedule.** `Workout.Date` is stamped at row creation and is also the start of the clock —
  `Duration` is computed at completion as `UtcNow - Date` — so a workout cannot meaningfully exist before it is
  performed. Scheduling itself is deferred (decision M), but it has nothing to attach to until a plan exists.
- **No plan-versus-actual.** Because the plan is overwritten by what happened, there is nothing to compare
  against. "I planned 5×5 and did 5,5,5,4,3" is unrepresentable.
- **Nothing can be assembled without being live.** `IsCompleted` is the only lifecycle flag, and
  `StartWorkoutAsync` returns today's incomplete workout if one exists — a one-active-per-day heuristic standing
  in for the missing plan layer.
- **Templates are all-or-nothing.** `StartWorkoutFromTemplateAsync` copies one template into one workout, so
  templates can't be composed and can't describe anything smaller than a full session.

## Goals

- Separate the plan from the record of performing it, without disturbing what `Workout` already means.
- Make templates composable building blocks, usable at any granularity.
- Allow plans to be created ahead of time, kept, reused, and edited independently of any workout.
- Guide the user through a plan while recording what actually happened, including deviations.
- Keep every derived statistic — analytics, PRs, achievements, challenges, streaks, 1RM — reading performed
  work only.
- Preserve the user-scoping invariant across the new entities.

## Non-Goals

- **Scheduling, in any form** — placing a plan on a date, recurrence, reminders, and how plans and workouts share
  the calendar. Deferred wholesale to a separate idea document (decision M), which is expected to look at iCal.
  This document assumes only that plans exist and can be picked up whenever the user wants.
- Periodization and multi-week programming.
- User-to-user sharing of templates or plans.
- Prescribing starting weights.
- Structured catalog metadata beyond name and description (decision 5).
- Renaming or restructuring `Workout`, `WorkoutExercise` or `Set` (decision G).

## Current System Context

**The entities as they stand**

- `Workout` — `Id`, `UserId`, `Date` (creation time, doubles as clock start and calendar date), `Duration` (int
  minutes, written once at completion), `Notes`, `CreatedAt`, `IsCompleted`, `WorkoutExercises`,
  `PersonalRecords`.
- `WorkoutExercise` — `WorkoutId`, `ExerciseId`, `Order`, `Notes`, `Sets`. It carries no prescription.
- `Set` — `SetNumber`, `Reps`, `Weight`, `Duration` (int?, currently dead — `LogSetAsync` writes only weight,
  reps and RPE), `RestTime`, `RPE`. **No `Distance` field.**
- `WorkoutTemplate` / `WorkoutTemplateExercise` — user-owned; the template exercise already carries
  `DefaultSets`, `DefaultReps`, `Order` and `Notes`, of which `StartWorkoutFromTemplateAsync` currently keeps
  only `Order` and `Notes`.
- `PersonalRecord` hangs off `Workout`, which remains correct — a PR is something that was performed.

**Who reads what (and therefore what decision G protects)**

- `_context.Workouts` is queried in 5 services (`WorkoutService`, `AnalyticsService`, `AchievementService`,
  `ChallengeService`, `ExportService`) and 5 page models (`Index`, `Calendar`, `Analytics/Daily`,
  `Workouts/Details`, `Workouts/History`).
- `WorkoutExercises` is referenced across 6 services and 12 page/view files, including
  `Pages/Shared/Components/_WorkoutCard.cshtml`.
- Sets are read by 8 services including `OneRepMaxService`, `PersonalRecordService` and `ExerciseService`.
- `IsCompleted` appears **62 times across 15 files**.
- 8 test files in `FitTracker.Tests/Services/` construct `Workout` graphs directly.

Every one of those reads "the work that happened" and continues to mean exactly that. They are not in scope.

## Proposed Shape (exploratory)

A sketch to react to, not a specification.

**Template layer — existing, semantics widened.** `WorkoutTemplate` and `WorkoutTemplateExercise` keep their
names; a template need no longer be a whole session. The template exercise gains duration and distance alongside
`DefaultSets`/`DefaultReps`. Global templates become ownerless rows (decision 1), so template *reads* widen to
`t.UserId == userId || t.UserId == null` while writes and deletes stay strict.

**Plan layer — new.**

- `WorkoutPlan` — `Id`, `UserId`, `Name`, `Description`, `IsActive` (decision L), soft-delete state (decision P),
  `CreatedAt`, `Exercises`. No date field: a plan is a recipe, not an occurrence, and scheduling is deferred
  (decision M). Inactive means retired from use entirely and is reversible (decision Q).
- `WorkoutPlanExercise` — `WorkoutPlanId`, `ExerciseId`, `Order`, and the prescription: `TargetSets`,
  `TargetReps`, `TargetDuration`, `TargetDistance`, `Notes`. Populated by copying from whichever templates
  contributed, then freely editable.
- Deliberately holds **no set data**. Because plans live in their own tables and every aggregate reads `Workout`,
  it is structurally impossible for planned work to be counted as performed work — the most damaging failure
  mode in this design is ruled out by construction rather than by discipline.
- Shaped almost identically to `WorkoutTemplate`/`WorkoutTemplateExercise`, which is expected: a template is a
  reusable fragment and a plan is the assembled thing. `IsActive` even carries the same meaning it already has on
  templates.

**Record layer — existing, one addition.** `Workout` gains a nullable `WorkoutPlanId` — a reference to the plan
being followed, not a copy of it (decision K). Null means an ad-hoc workout, which is exactly today's behaviour
and today's fastest path (decision H). Everything else about `Workout`, `WorkoutExercise` and `Set` stays as it
is, except `Set` gaining `Distance` so that duration- and distance-based prescriptions can actually be recorded.

**Flow.** Templates → plan → workout, in that order, always (decision J). There is no template-to-workout path:
`StartWorkoutFromTemplateAsync` is replaced by a start-from-plan equivalent, and the template's role ends the
moment a plan is assembled. One plan may be followed by many workouts (decision I).

**What changes, in total**

| Area | Change |
| --- | --- |
| New | `WorkoutPlan`, `WorkoutPlanExercise`, a plan service, a plan builder surface, and a plans list |
| Modified | `Workout` gains nullable `WorkoutPlanId`; `Set` gains `Distance`; `WorkoutTemplateExercise` gains duration/distance; `WorkoutTemplate.UserId` becomes nullable; `StartWorkoutFromTemplateAsync` gives way to start-from-plan; set logging UI gains duration/distance inputs and shows the plan's targets |
| Untouched | `AnalyticsService`, `AchievementService`, `ChallengeService`, `PersonalRecordService`, `OneRepMaxService`, `ExportService`, `Workouts/History`, `Workouts/Details`, `Index`, `_WorkoutCard`, and the 62 `IsCompleted` sites |
| Deferred | Scheduling and calendar behaviour, to a separate idea (decision M) |
| Migration | Additive, and unconstrained — the app has never been deployed (decision N), so the schema can simply change and the database be recreated. |

**Guided progression** means the workout screen walks the plan's exercises in order, showing the prescription next
to the logging inputs, and accepts deviation as normal: extra or fewer sets, a different weight, a skipped or
swapped exercise, one added on the spot. The plan stays intact; the workout records reality. That difference is
itself a new capability — plan-versus-actual comparison — which did not previously exist.

**Consequences for the template catalog.** The 25-template catalog survives unchanged in content, but its verbs
change: a built-in template is *added to a plan* rather than started. The revision-3 risk that warm-ups would each
log a separate workout disappears, since a warm-up is a block inside one plan. Templates that are partial by
nature — warm-ups, finishers, accessory blocks — become first-class rather than awkward.

## Relevant Considerations

### UX and Workflow

- A plan builder becomes a real surface: pick templates, add individual exercises, reorder, adjust the
  prescription, save — with performing it a separate, later action.
- Combining templates is additive and honest: everything each template contributes lands in the plan, duplicates
  included, with an overlap indicator drawing the eye to them (decision R). The user resolves overlaps by editing,
  which keeps the builder free of merge heuristics that would inevitably guess wrong — a warm-up's light
  Push-Ups and a main block's working Push-Ups are not the same entry, and no rule could reliably tell.
- Vocabulary needs care. Users will call a plan "my Monday workout", and the app now has both. UI copy has to
  distinguish planned from performed consistently ("Planned" vs "History", say) even though only one of them is
  called `Workout` in code.
- `Pages/Workouts/Start` splits conceptually into "build a plan" and "perform it", with the ad-hoc path preserved.
- The dashboard's "start from template" entry point becomes "start from a plan", with the ad-hoc start alongside
  it. Templates disappear from the start-a-workout flow entirely (decision J).
- `/Templates` and a new `/Plans` list sit next to each other and will look similar. What distinguishes them in
  the UI is purpose, not shape: templates are the ingredients, plans are the recipes.
- The calendar keeps showing performed workouts and is otherwise untouched until the scheduling idea lands.
- `WorkoutSuggestionService` currently suggests a template; it could suggest a plan instead, or both. Not
  urgent, but it will look inconsistent if left pointing only at templates.

### Data and State

- Decisions K and O settle plan mutability: the plan guides, the workout records, nothing is copied, and editing a
  plan that workouts already followed simply warns. A March workout will therefore describe today's version of
  the plan it points at. The performed sets are untouched, which is the whole justification — *it's the Workout
  that matters*.
- Decision P means plans are never physically deleted, so the FK from `Workout` stays valid forever and no
  `DeleteBehavior` question arises. It leaves one shape question for the specification: whether "deleted" is a
  second flag alongside `IsActive`, or whether deletion and retirement are the same state. They behave
  differently in the UI — a retired plan must remain visible enough to reactivate (decision Q), while a deleted
  one should disappear — which argues for two flags.
- Templates have no such constraint: nothing references a template after a plan is built (decision T), so
  `DeleteTemplateAsync` can keep hard-deleting.
- Per-set `Duration` already exists but is never written. Adding `Distance` and surfacing both in the logging UI
  is what makes cardio and hold-based templates meaningful — and it widens to everything that reads sets
  (analytics aggregates, PR detection, 1RM, CSV/PDF export), even though those reads keep working untouched.

### Units — Canonical Storage (decision S)

This decision is wider than it first appears, because **units are currently a display label only**.
`PreferredUnits` is read in exactly one page model, passed to the view, and concatenated onto rendered numbers.
Whatever the user types into the weight box is stored verbatim in `Set.Weight`.

The consequence is a latent defect: a user who switches from lbs to kg does not convert their history, they
*relabel* it. Every past 100 lb set silently becomes "100 kg". Decision S fixes this by storing one canonical
unit and converting at the edges, which also makes every comparison, aggregate and PR check unit-safe by
construction.

What it touches:

- **Write path** — weight entry converts from the user's unit to canonical before saving; the same applies to the
  new distance field.
- **Read path** — every surface that renders a weight converts back: set inputs and history on the workout
  screen, workout details, history, `_WorkoutCard`, analytics, PRs, progress charts, 1RM pages, and CSV/PDF
  export.
- **Progressive overload** — `GetSuggestedWeightIncrement(decimal weight)` in `WorkoutService` computes a jump
  from the raw stored number, and `userUnits` is threaded through purely to build the message string. A 5 lb jump
  and a 5 kg jump are not the same increment, so this becomes genuinely unit-aware rather than cosmetic.
- **Body measurements** — `BodyMeasurement` stores `Weight` plus `Chest`, `Waist`, `Arms` and `Legs`. Body weight
  has the same lbs/kg problem, and the circumference fields introduce a *length* unit (in/cm) that has no
  preference field anywhere today. Whether measurements are in scope for this change is a scoping call.

Canonical unit choice is a specification detail. Storing metric (kg, km, cm) is the conventional choice and keeps
conversion in one direction; storing imperial would suit the `"lbs"` default the app already ships with. Either
works provided display rounding does not accumulate error on round-trips through the edit path.
- Three parallel exercise-list shapes (template / plan / workout) is the structural cost of this model. It is the
  standard shape for this domain, but it triples the places an ordering bug can live.

### Security and Authorization

- `WorkoutPlan` and `WorkoutPlanExercise` need the same treatment the architecture notes describe: `userId`
  filtered inside the query, `null`/`false` on no match, `NotFound()` in the page.
- Starting a workout from a plan must verify the plan belongs to the caller before copying anything into a new
  `Workout` row.
- Global templates make `WorkoutTemplate.UserId` nullable (decision 1), so template reads have two legitimate
  predicate shapes while plans and workouts keep exactly one. That asymmetry is the one place the scoping rule
  gets more subtle.

### Operational

- The migration is additive: two new tables, three new columns, one column made nullable. Because the app has
  never been deployed (decision N), even that is a soft constraint — the schema can change freely and the
  database be recreated from migrations and seeding at startup. Decision G's payoff is therefore code churn
  avoided, not data preserved.
- `Specifications/Implementation.md` gains a Plans section; the existing Workout Templates section needs its
  meaning revised rather than replaced. `UserGuide.md` and `ManualTestChecklist.md` follow.
- Existing tests keep working, since they construct `Workout` graphs whose meaning is unchanged. New tests are
  additive rather than a rewrite.

## Tradeoffs or Risks

- **Two things users will both call "workout".** The code is unambiguous (`WorkoutPlan` vs `Workout`), but the
  UI has to carry that distinction without jargon. This is the main cost of decision G and it lands entirely in
  copy and interaction design.
- **`Workout.Date` still doubles as clock start and calendar date.** That's fine while workouts are created at
  the moment training begins, but it means a workout can never be back-dated or pre-created — scheduling has to
  live wholly on the plan.
- **Ad-hoc must stay first-class.** Nullable `WorkoutPlanId` makes it structurally easy; the risk is that the UI
  gradually assumes a plan exists and the fastest current path (open app, start lifting) degrades.
- **Prescription drift.** Decisions K and O mean no snapshot and no versioning, so editing a reusable plan
  retroactively changes what past workouts appear to have been aiming at. Accepted knowingly, mitigated by a
  warning; the consequence is confined to plan-versus-actual displays and the recorded work is untouched.
- **Canonical units touch every numeric surface.** Decision S is the widest-reaching item in this document after
  the plan layer itself: a conversion at every read and write of weight and distance, plus unit-aware
  progressive-overload increments. Miss one display site and the number is silently wrong rather than visibly
  broken — the failure mode is a plausible-looking figure in the wrong unit, which no test catches unless the
  conversion is centralized in one place and the pages are forbidden from formatting raw values themselves.
- **Templates and plans look like duplicates.** Two entities with near-identical shape, two similar list pages,
  and two builder-like surfaces. The distinction is purposeful (ingredients versus recipe) but it has to be
  taught by the UI, and it is a plausible source of user confusion and of code that drifts apart for no reason.
- **`Set` gains a column on the busiest table**, and the logging screen is the most-used surface in the app.
  Duration/distance is a bigger piece of work than the plan layer itself and could reasonably ship separately.
- **Scope displacement.** The starter-template catalog was the original goal and is downstream of all of this.
  Decision M pulls scheduling out, which keeps this document buildable, but the catalog still lands last.

## Status — Ready for Specification

No open questions remain. The model, its edges, and the catalog that rides on it are all decided, and decision U
calls for a single specification covering this entire document, with phasing handled by the implementation plan.

Details deliberately left for the specification to pin down, each a consequence of a decision rather than a
question about direction:

- Which unit is canonical (metric or imperial), and the rounding rule that keeps edit round-trips stable.
- Whether soft-delete is a flag distinct from `IsActive`, given retired plans must stay reachable to reactivate
  while deleted ones should not.
- Whether `BodyMeasurement` joins the canonical-units change, and whether a length preference (in/cm) is
  introduced with it.
- The exact target fields on `WorkoutPlanExercise` and how they render alongside the logging inputs.
- Seeding mechanics for the catalog: the stable key per built-in, and the per-entry idempotent check that
  replaces the current all-or-nothing `AnyAsync()` guard (decision 9).
- How the plans list and templates list differentiate themselves visually, given their near-identical shape.

A plausible phase order for the implementation plan, offered as input rather than as part of this idea:
(1) plan layer, builder and start-from-plan; (2) canonical units; (3) guided progression with duration and
distance on sets; (4) global templates and the seeded catalog.

## References or Related Artifacts

- `Models/Workout.cs`, `Models/WorkoutExercise.cs`, `Models/Set.cs`, `Models/WorkoutTemplate.cs`,
  `Models/WorkoutTemplateExercise.cs`, `Models/PersonalRecord.cs`, `Models/ApplicationUser.cs`
- `Services/WorkoutService.cs` (`StartWorkoutAsync`, `StartWorkoutFromTemplateAsync`, `LogSetAsync`,
  `CompleteWorkoutAsync`), `Services/TemplateService.cs`, `Services/WorkoutSuggestionService.cs`
- `Pages/Workouts/*`, `Pages/Calendar.cshtml.cs`, `Pages/Index.cshtml.cs`, `Pages/Templates/*`
- `Data/ApplicationDbContext.cs`, `Data/DbInitializer.cs`
- `Specifications/Idea.md` (pre-planning and calendar scheduling as original intent; recurring schedules and
  sharing marked out of scope), `Specifications/Implementation.md` (§ Workout Templates, lines 218–234)
- *Scheduling idea document — not yet written (decision M).* Expected to cover placing plans on dates,
  recurrence, reminders, iCal, and how plans and workouts share the calendar.
- `CLAUDE.md` — user-scoping, seeding and service-layer conventions this model has to stay inside

## Appendix — Candidate Template Catalog (25)

Carried forward from revision 3, unchanged in content. Under this model these are **blocks**, combined into a plan
rather than started directly. Exercises in **bold** do not exist in the seeded library.

### Gym (9)

| # | Template | Sketch |
| --- | --- | --- |
| 1 | Full Body A | Back Squat 3×5, Bench Press 3×5, Bent Over Row 3×5, Overhead Press 3×8, Plank 3× 45s |
| 2 | Full Body B | Deadlift 1×5, Incline Bench 3×8, Lat Pulldown 3×10, Goblet Squat 3×10, Hanging Leg Raises 3×10 |
| 3 | Push Day | Bench Press, Incline Dumbbell Press, Overhead Press, Lateral Raises, Tricep Pushdown |
| 4 | Pull Day | Deadlift, Pull-Ups, Seated Cable Row, Face Pulls, Barbell Curl |
| 5 | Leg Day | Back Squat, Romanian Deadlift, Leg Press, Walking Lunges, Calf Raises |
| 6 | Upper Body | Bench Press, Bent Over Row, Dumbbell Shoulder Press, Lat Pulldown, Hammer Curl, Skull Crushers |
| 7 | Lower Body | Back Squat, Romanian Deadlift, Bulgarian Split Squat, Leg Curl, Calf Raises |
| 8 | Arms & Shoulders | Arnold Press, Lateral Raises, Barbell Curl, Tricep Pushdown, Hammer Curl, Shrugs |
| 9 | Machine Circuit (beginner) | Leg Press, Pec Deck, Lat Pulldown, Seated Cable Row, Leg Extension, Leg Curl |

### Home (6)

| # | Template | Sketch |
| --- | --- | --- |
| 10 | Bodyweight Full Body | Push-Ups, Inverted Row, **Bodyweight Squat**, Walking Lunges, Plank, Mountain Climbers |
| 11 | Core Express (15 min) | Plank, Side Plank, Crunches, Russian Twists, Mountain Climbers |
| 12 | Dumbbell Full Body | Dumbbell Bench Press, Dumbbell Row, Dumbbell Shoulder Press, Bulgarian Split Squat, Hammer Curl |
| 13 | Dumbbell Upper Body | Dumbbell Bench Press, Dumbbell Row, Arnold Press, Dumbbell Curl, Overhead Tricep Extension |
| 14 | Squat Rack Full Body | Back Squat, Bench Press, Bent Over Row, Overhead Press, Chin-Ups |
| 15 | Squat Rack Lower Body | Back Squat, Romanian Deadlift, Front Squat, Walking Lunges, Calf Raises |

### Outdoor (5)

| # | Template | Sketch |
| --- | --- | --- |
| 16 | Easy Run | Running — 30 min / 5 km, conversational pace |
| 17 | Run Intervals | **Warm-Up Walk**, **Sprint Intervals** 8× 400 m, Running (cool-down) |
| 18 | Park Calisthenics | Pull-Ups, Chest Dips, Push-Ups, Inverted Row, Hanging Leg Raises |
| 19 | Outdoor Conditioning Circuit | Burpees, Box Jumps, Mountain Climbers, Jump Rope, **Bodyweight Squat** |
| 20 | Long Ride | Cycling — 60 min / 20 km steady |

### Warm-Ups (5)

| # | Template | Sketch |
| --- | --- | --- |
| 21 | General Dynamic Warm-Up | **Jumping Jacks** 60s, **Arm Circles** 30s each way, **Leg Swings** 10 each side, **World's Greatest Stretch** 5 each side, **High Knees** 30s |
| 22 | Upper Body Warm-Up (push) | **Arm Circles**, **Band Pull-Aparts** 2×15, **Scapular Push-Ups** 2×10, **Wall Slides** 2×10, Push-Ups 1×10 light |
| 23 | Lower Body Warm-Up (squat/hinge) | **Bodyweight Squat** 2×10, **Hip Circles**, **Leg Swings**, **Glute Bridge** 2×12, Walking Lunges 1×10 |
| 24 | Pull Warm-Up (back/biceps) | **Band Pull-Aparts** 2×15, **Scapular Pull-Ups** 2×8, **Cat-Cow** 60s, **Dead Hang** 2× 20s, Face Pulls 1×15 light |
| 25 | Run Warm-Up | **Brisk Walk** 5 min, **Leg Swings**, **High Knees** 30s, **Butt Kicks** 30s, **Ankle Circles** 30s |

**Library gaps.** The main groups need a few additions (Bodyweight Squat, Walking, Sprint Intervals). The warm-up
group needs roughly 15 mobility and activation movements that have no equivalent in the seeded library, which
today has only `Strength`, `Core` and `Cardio` categories — most likely a new `Warm-Up`/`Mobility` category. The
seed-time `IsWeightLoaded` rule already handles them correctly, since a non-`Strength` category leaves
`TracksOneRepMax` false.

Two structural mismatches remain: "10 each side" is not representable, and holds like Cat-Cow depend on the
duration field landing. Both can fall back to `Notes` in a first pass.

Held in reserve as swaps: Core & Abs (gym), Classic 5×5 Strength A/B, Kettlebell Circuit, Swim, Hill Sprints, and
cool-down counterparts to the warm-ups.
