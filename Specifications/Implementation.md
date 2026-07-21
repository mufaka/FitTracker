# FitTracker Implementation Plan

This document tracks the implementation progress of the FitTracker application. Check off items as they are completed.

## Project Setup

### Initial Configuration
- [x] Create ASP.NET Core Razor Pages project (.NET 10)
- [x] Configure SQLite database connection
- [x] Set up Entity Framework Core
- [x] Configure Microsoft Identity for authentication
- [x] Set up project structure (folders for Pages, Models, Data, Services)
- [x] Add `.gitignore` for .NET projects
- [ ] Initialize Git repository

### Frontend Setup
- [x] Install and configure Tailwind CSS 4
- [x] Set up Alpine.js
- [x] Set up HTMX
- [x] Create base layout with dark mode support
- [x] Implement dark mode toggle functionality
- [x] Create responsive navigation component
- [x] Set up CSS build pipeline

### Development Tools
- [x] Configure development environment
- [x] Set up database migrations
- [x] Configure logging
- [ ] Set up development HTTPS certificate

---

## Phase 1: MVP (Minimum Viable Product)

### Database Schema & Models

#### Core Entities
- [x] Create `ApplicationUser` (extends IdentityUser)
  - [x] Add custom properties (preferences, settings)
- [x] Create `Exercise` model
  - [x] Id, Name, Category, MuscleGroups, Equipment, Description, VideoUrl
- [x] Create `Workout` model
  - [x] Id, UserId, Date, Duration, Notes, CreatedAt
- [x] Create `WorkoutExercise` model
  - [x] Id, WorkoutId, ExerciseId, Order, Notes
- [x] Create `Set` model
  - [x] Id, WorkoutExerciseId, SetNumber, Reps, Weight, Duration, Distance, RestTime, RPE
- [x] Create `UserProfile` model (integrated into ApplicationUser)
  - [x] UserId, PreferredUnits (lbs/kg), DefaultRestTimer, Goals

#### Database Context
- [x] Create `ApplicationDbContext`
- [x] Configure entity relationships
- [x] Add data annotations and fluent API configurations
- [x] Create initial migration
- [x] Apply migration to database

#### Seed Data
- [x] Create exercise seed data (50-100 exercises)
  - [x] Chest exercises
  - [x] Back exercises
  - [x] Leg exercises
  - [x] Shoulder exercises
  - [x] Arm exercises
  - [x] Core exercises
  - [x] Cardio exercises
  - [x] Mobility exercises
- [x] Create database seeder class
- [x] Run seed data on application start (development only)

### Authentication & User Management

- [x] Configure Identity services in `Program.cs`
- [x] Create Register page (`/Account/Register`)
- [x] Create Login page (`/Account/Login`)
- [x] Create Logout functionality
- [x] Create Account/Manage page (basic profile settings)
- [x] Implement email confirmation (optional for MVP)
- [x] Add password reset functionality
- [x] Create user profile setup wizard (first-time users)

### Core Pages - Dashboard

- [x] Create Dashboard page (`/Index` or `/Dashboard`)
  - [x] Display today's date
  - [x] Show today's planned workout (if any)
  - [x] Show quick stats (workouts this week, current streak)
  - [x] "Start Workout" button
  - [x] Recent workout history (last 5 workouts)
- [x] Style dashboard with Tailwind CSS
- [x] Make responsive for mobile/tablet/desktop
- [x] Add dark mode styling

### Core Pages - Exercise Library

- [x] Create Exercise Library page (`/Exercises/Index`)
  - [x] List all exercises
  - [x] Group by category (Strength, Cardio, Flexibility, etc.)
  - [x] Search functionality (HTMX)
  - [x] Filter by muscle group
  - [x] Filter by equipment
- [x] Create Exercise Details page (`/Exercises/Details/{id}`)
  - [x] Display exercise information
  - [x] Show video link (if available)
  - [x] Show usage history (how many times logged)
- [x] Implement HTMX for live search
- [x] Style with Tailwind CSS
- [x] Make responsive

### Core Pages - Workout Logging

- [x] Create Start Workout page (`/Workouts/Start`)
  - [x] Select exercises from library
  - [x] Quick log interface for adding sets
  - [x] Display current exercise
  - [x] Add set with reps and weight
  - [x] Built-in rest timer between sets
  - [x] Notes field (optional)
  - [x] "Complete Workout" button
- [x] Implement HTMX for dynamic set additions (no page refresh)
- [x] Create rest timer functionality with Alpine.js
  - [x] Configurable timer duration
  - [x] Visual countdown
  - [x] Sound/vibration notification (optional)
- [x] Auto-save progress (prevent data loss)
- [x] Progressive overload suggestions
  - [x] Show last workout data for the exercise
  - [x] Suggest weight/rep increase
- [x] Style with large tap targets for mobile
- [x] Make fully responsive

### Core Pages - Workout History

- [x] Create Workout History page (`/Workouts/History`)
  - [x] List all completed workouts
  - [x] Show date, duration, exercises
  - [x] Pagination (10-20 per page)
  - [x] Filter by date range
  - [x] Search functionality
- [x] Create Workout Details page (`/Workouts/Details/{id}`)
  - [x] Show all exercises and sets
  - [x] Display workout notes
  - [x] Option to delete workout
  - [x] Option to "Repeat Workout" (start new with same exercises)
- [x] Style with Tailwind CSS
- [x] Make responsive

### Daily Summary

- [x] Create Daily Summary component
  - [x] Exercises completed today
  - [x] Total volume (sets × reps × weight)
  - [x] Total workout duration
  - [x] Calories burned estimate
- [x] Display on Dashboard
- [x] Create dedicated Daily Summary page (`/Analytics/Daily`)
- [x] Style with charts/graphs
- [x] Make responsive

### Services & Business Logic

- [x] Create `WorkoutService`
  - [x] Start workout
  - [x] Add exercise to workout
  - [x] Log set
  - [x] Complete workout
  - [x] Calculate workout volume
  - [x] Get progressive overload suggestions
- [x] Create `ExerciseService`
  - [x] Search exercises
  - [x] Filter exercises
  - [x] Get exercise history for user
- [x] Create `AnalyticsService`
  - [x] Calculate daily summary
  - [x] Calculate total volume
  - [x] Estimate calories burned
- [x] Add unit tests for services (optional for MVP)

### UI Components

- [x] Create reusable components (Razor partials)
  - [x] Exercise card
  - [x] Workout card
  - [x] Set input component
  - [x] Timer component
  - [x] Statistics card
  - [x] Loading spinner
  - [x] Empty state components
- [x] Style all components with Tailwind CSS
- [x] Ensure dark mode compatibility

### Testing & Bug Fixes

Reference checklist: `Specifications/ManualTestChecklist.md`

- [ ] Test user registration flow
- [ ] Test login/logout flow
- [ ] Test starting a workout
- [ ] Test logging sets and exercises
- [ ] Test completing a workout
- [ ] Test viewing workout history
- [ ] Test exercise search and filters
- [ ] Test dark mode toggle
- [ ] Test responsive design on multiple devices
- [ ] Fix any discovered bugs
- [ ] Performance testing with larger datasets

### Documentation

- [x] Write README.md with setup instructions
- [x] Document database schema
- [x] Add code comments where necessary
- [x] Create user guide (optional)

---

## Phase 2: Enhanced Tracking

### Workout Templates

Reshaped by the workout domain model change
(`Specifications/WorkoutDomainModelSpecification.md`). A template is no longer a
whole workout: it is a reusable grouping of exercises — a warm-up, a push block,
an easy run — and it cannot start anything. Templates are combined into a plan,
and the plan is what a workout is performed from, so "Start from Template" is
withdrawn and the dashboard's start action points at a plan instead. A template
may also be ownerless: a null `UserId` means a built-in that every user sees.

- [x] Create `WorkoutTemplate` model
  - [x] Id, UserId (nullable — null is a built-in), CatalogKey, Name, Description, IsActive
- [x] Create `WorkoutTemplateExercise` model
  - [x] Id, TemplateId, ExerciseId, Order, DefaultSets, DefaultReps, DefaultDurationSeconds, DefaultDistance, Notes
- [x] Add database migration
- [x] Create Templates page (`/Templates/Index`)
  - [x] List personal and built-in templates, filtered by owner
  - [x] Create new template
  - [x] Edit template
  - [x] Copy template
  - [x] Delete template
- [x] Create Template Builder page (`/Templates/Create`)
  - [x] Add exercises to template
  - [x] Set default sets/reps
  - [x] Set default duration and distance
  - [x] Reorder exercises
- [x] Add "Start from Template" option to dashboard — shipped, then withdrawn (see above)
- [x] Style with Tailwind CSS

Every part of a prescription is now independently optional: a stretch prescribes a
duration and no reps, a run a distance and neither. `DefaultSets` and `DefaultReps`
went from `int` defaulting to 3 and 10 to `int?`, so "unspecified" is a value the
model can hold rather than a number the builder invents. The builder still offers
3 × 10 when an exercise is added, because that is what most of them want.

Ownership is two read predicates rather than one, and `TemplateOwnership` names them
so no call site has to remember an inline null check. Reads are deliberately wide —
a built-in has to be readable to be previewed and copied — and writes are strict:
`SaveTemplateAsync` and `DeleteTemplateAsync` both match on `UserId == userId`, which
a built-in's null can never satisfy. That predicate, not a check in the UI, is what
makes the catalog immutable through every user-reachable route; `/Templates/Create`
redirects a hand-typed built-in id to the list, where the offered action is Copy. A
copy is an ordinary user-owned template with `CatalogKey` cleared, so later seeding
cannot collide with it.

### Built-in Template Catalog

- [x] Define the catalog as data (`Data/TemplateCatalog.cs`)
- [x] Seed 25 ownerless templates, identified by `CatalogKey`
- [x] Add the exercises the catalog needs to the library
- [x] Add the Mobility category
- [x] Create the catalog page (`/Templates/Catalog`), readable without an account
- [x] Style with Tailwind CSS

Twenty-five entries: nine gym blocks, six home, five outdoor and five warm-ups.
They are blocks rather than sessions — the user combines them into a plan and
performs the plan.

Seeding runs per entry rather than behind the whole-table `AnyAsync()` guard the rest
of `DbInitializer` uses. A guard would seed the catalog once and then skip every entry
added afterwards; keying on `CatalogKey` means a template added in a later release
reaches a database seeded by an earlier one, and re-running is still a no-op. Entries
already present are left exactly as they are.

Catalog entries name their exercises instead of carrying ids — names are stable and
readable, ids are neither. A name that does not resolve throws and fails startup, the
same choice `Program.cs` already makes for a bad schema: a template seeded with a hole
in it would be much harder to notice.

The exercises the catalog needs are seeded per name for the same reason — the exercise
library block is behind an `AnyAsync()` check that would skip them forever on a
database that already has a library. Eighteen were added: two bodyweight strength
movements, five cardio entries (walking, sprint intervals and three warm-up drills),
and eleven under a new Mobility category for warm-up and activation work. The
seed-time `TracksOneRepMax` rule leaves every Mobility entry false, which is right —
none of them has a meaningful one-rep max.

`/Templates/Catalog` is a separate, unauthenticated page rather than relaxed
authorization on `/Templates/Index`, which shows a user's own templates and has to
stay protected. The only read it makes is `GetCatalogAsync`, which returns ownerless
rows and nothing else.

### Workout Plans

- [x] Create `WorkoutPlan` model
  - [x] Id, UserId, Name, Description, IsActive, IsDeleted, CreatedAt
- [x] Create `WorkoutPlanExercise` model
  - [x] Id, WorkoutPlanId, ExerciseId, Order, TargetSets, TargetReps, TargetDurationSeconds, TargetDistance, Notes
- [x] Add database migration
- [x] Create Plans page (`/Plans/Index`)
  - [x] List plans, active first
  - [x] Deactivate and reactivate a plan
  - [x] Delete a plan
- [x] Create Plan Builder page (`/Plans/Create`)
  - [x] Apply a template, personal or built-in
  - [x] Add individual exercises
  - [x] Reorder exercises
  - [x] Prescribe sets, reps, duration, distance and notes per exercise
  - [x] Warn before saving an edit to a plan that workouts already reference
- [x] Start a guided workout from a plan (`/Workouts/Start?planId=`)
- [x] Suggest a plan on the dashboard, in place of the template suggestion
- [x] Style with Tailwind CSS

A plan is a recipe and holds no performance data of any kind — no set, no weight, no
date. Creating one does not start training: a `Workout` still records what was
actually done, and an ad-hoc workout with no plan behind it stays a first-class way
to train and behaves exactly as workouts always have.

Applying a template appends everything it contributes, in its order and with its
prescription, and never merges, deduplicates or drops. A plan is a flat ordered list,
so an exercise the plan already holds becomes a second entry: a warm-up's light
push-ups and a main block's working push-ups are not the same line. The builder badges
the duplicates and leaves the decision to the user. Nothing records which templates a
plan was assembled from — once applied, the lines are simply the plan's.

Retiring and deleting are different actions. An inactive plan cannot start a workout
but stays in the list so it can be brought back. A deleted plan is soft-deleted and
never physically removed, which is what keeps the reference a workout holds resolving
forever; the foreign key from `Workout` is `Restrict` rather than `Cascade`, so a plan
could not take performed workouts with it even if a hard delete were introduced later.

Guidance is read live from the plan every time it is drawn and is never copied onto
the workout — starting from a plan materializes one `WorkoutExercise` per planned
exercise, identity and order only. Editing a plan therefore changes what a past
workout displays as its intent, and can never alter a recorded set. No version is
kept, which is why the builder warns before the edit lands rather than after: a save
to a plan the user's own workouts already reference is held back once, and a second,
deliberately different button confirms it. The guidance read is intentionally not
filtered by active or deleted, because a workout performed months ago still has to be
able to show what it was aiming at; ownership is still enforced, and the separate read
that starts a new workout does require the plan to be active and undeleted.

#### Exercise Status

- [x] Add `Status` to `WorkoutExercise` — Pending, Skipped, Easy, Medium, Hard
- [x] Rate how an exercise felt during a workout
- [x] Skip an exercise
- [x] Require at least one performed exercise before a workout can be completed

Strings against a `WorkoutExerciseStatuses` constants class rather than a CLR enum,
following `AchievementCriteria` and `ChallengeGoalTypes` — the codebase contains no
enums.

Skipping and recording work are mutually exclusive, enforced from both directions.
Skip is refused on an exercise that has sets, and the button is absent there rather
than offered and rejected. Logging a set against something already marked skipped
clears the mark back to Pending — the work is the stronger statement — but never to
an effort rating, because only the user says how it felt.

"Performed" means at least one recorded set, or an effort rating. It exists twice,
deliberately: `WorkoutExercise.IsPerformed` for collections already in memory and
`WorkoutExerciseStatuses.PerformedPredicate` for queries EF has to translate. The two
encode one rule and must not drift apart.

Completing a workout used to test that it had any exercise at all. That became vacuous
the moment starting from a plan creates a row per planned exercise, and would have let
a workout in which nothing was done feed personal records, achievements and challenges
— so the test is now at least one performed exercise. Everything else derived from
exercise rows uses the same rule: the usage history on the exercise details page, the
least-worked muscle groups the suggestion service focuses on, and the exercise counts
the dashboard shows for a past workout.

#### Guided Workout Flow

- [x] Show one exercise at a time on an in-progress workout, by default
- [x] Advance to the next unanswered exercise when one is skipped or rated
- [x] Show progress, and a tappable strip giving direct access to any exercise
- [x] Offer the finish screen once every exercise has been answered for
- [x] Keep the full lineup one tap away, and make it what a completed workout renders
- [x] Present an input row per outstanding prescribed set, pre-filled and submitted together
- [x] Withhold the effort rating until the exercise has a recorded set

The whole lineup is the right thing when you are building a workout and the wrong
thing when you are performing one: a plan materializes every exercise up front, so the
screen that starts a plan opened onto a wall of cards with no sense of where in it you
were. The focused flow answers "what now?" — one exercise, its plan target, its logging
inputs, and the two answers there are: how it felt, or skipped.

It is the default because a workout is performed on a phone, one movement at a time.
`Show full list` goes back, and a completed workout is always the list — there is
nothing left to walk through, and reviewing wants everything at once.

The opt-out and the exercise the flow is on live in the query string (`?list=true`,
`?at=`), not in the database and not in `localStorage`. Every action on this page is a
form post followed by a redirect, so the state has to survive a redirect either way —
and Alpine is loaded deferred, so a client-side mode would render the full list and
then hide it, flashing the exact thing the mode exists to remove. Nothing about a view
preference this small earns a column. The flag is the opt-*out* so that the absence of
everything still lands on the flow.

Answering advances; navigating does not. Rating an exercise or skipping it moves to
the next one still open, wrapping past the end so anything passed over earlier comes
back round before the workout is declared done — the exercise just answered for is
offered again last, and only if nothing else is waiting. Logging sets deliberately does
not advance: the user still says how it went. Clearing an answer stays put, because
clearing is not answering. The order is a proposal throughout — the step strip reaches
any exercise at any time (WDM-UI-08).

A prescription of 3 × 10 is three input rows, pre-filled with 10 reps and submitted
together, rather than the same one-row form posted three times. Only the sets still
outstanding are offered — `max(1, TargetSets − logged)` — so what is already in the
table above is never invited twice, and the one-row floor keeps an extra set in reach
past the prescription. Pre-filling means a row nobody did would otherwise record
itself, so `Clear` blanks a row in one tap and an empty row logs nothing. Rows are
named `Weight_{workoutExerciseId}_{index}` and post as one form with a
`SetRows_{workoutExerciseId}` count, because the page-wide form covers every exercise
at once in list mode.

In the flow the effort rating waits for a set: advancing on `Easy` alone would move on
having recorded an opinion and nothing else. `Skip` stays available throughout — it is
the answer for an exercise there is nothing to record about. This is a constraint of
the presentation, not of the domain: `SetExerciseStatusAsync` still accepts a rating on
its own (WDM-52), which is what the full list and every existing caller rely on.

An effort rating supplies the RPE for sets logged without one — `Easy` 5, `Medium` 7,
`Hard` 9 (WDM-58). The rating already says how the work felt, so asking for the same
judgement again on every row is a tax on logging, and a set with no RPE tells the
analytics nothing at all. It works in both directions: rating an exercise fills the
sets already recorded against it, and logging against an exercise already rated — which
the full list allows — takes the same value.

`Set.IsRpeDerived` is what keeps that honest. Only a value this rule wrote is ever
written over, so a number the user typed survives a re-rating; changing `Easy` to `Hard`
moves 5 to 9 and leaves a typed 8 alone; and clearing the answer clears the supplied
values back to null rather than leaving a number nobody chose sitting in the record. The
alternative — deriving at read time — was rejected because `Set.RPE` is read by the
export, the workout summary average and two views, and threading the owning exercise's
status through all of them to compute a display value is more surface than one column.
The set table renders a supplied RPE muted, so it does not read as one that was typed.

Ending a workout confirms in a native `<dialog>` opened with `showModal()` rather than
`confirm()` (WDM-UI-22). The element centres itself in the viewport, dims the page,
traps focus and closes on Escape with none of that written here; `modal-card` only adds
the card look and the entry animation. Both buttons stay ordinary submits carrying their
handler and have their default prevented by Alpine, so with no scripting they still
finish the workout — the confirmation is the thing that degrades, not the action. The
completion dialog says how many exercises are still outstanding, which `confirm()` had
no room for.

The exercise being logged is one surface. It was three: a bordered box for the recorded
sets inside a bordered box for the set form inside the exercise card, each with its own
padding, which on a phone left the numbers less than half the screen. Recorded sets and
the rows still on offer now share a single table — one line per set, recorded and
outstanding alike, which is also what makes the two read as the same thing.

`table-layout: fixed` is what keeps a set on one line at any width without a media
query: the columns divide whatever space there is. The column count changes with the
movement (three for a lift or a run, four for timed core work), so nothing can be
hard-coded — the ends are sized and the measurement columns share the rest. Card padding
drops to 16px below 640px, recorded values are set a size smaller than the fields, and
`input-cell` is a standalone field rather than `input` plus an override, because both
would set padding and which one wins is a question about utility ordering — the side
padding `input` carries is most of the width a four-column row has to spend, and losing
that argument silently cut values in half. Measured at 320, 390 and 1280: nothing wraps,
nothing is cut off, the page never scrolls sideways.

Every action taken while training swaps the form in place with htmx instead of reloading:
logging sets, answering for an exercise, stepping between them, adding or removing one.
A reload restarted the rest timer's Alpine state and threw away the scroll position in
the middle of a set, which on a phone is exactly when it hurts.
`Pages/Workouts/_WorkoutForm.cshtml` is the whole swap unit rather than a card, because
one action changes several parts at once — logging a set changes the set table, the rows
still on offer and the progress strip together. The form declares
`hx-target="#workout-form"` and `hx-swap="outerHTML"`, descendants inherit both and add
only their own `hx-post`/`hx-get`, and the handlers return `Partial("_WorkoutForm", this)`
when `HX-Request` is set. The rest timer sits outside the form and so keeps running.

Three things it is easy to get wrong. Every `hx-post` carries
`hx-include="closest form"`, because htmx does not include an enclosing form on its own
and the antiforgery token lives in it. Completing and cancelling deliberately carry no
`hx-*` at all — they leave the workout, so a full navigation is right, and htmx ignores
a submit button it was not told about. And `RespondAsync` sets `HX-Push-Url`: without it
the address bar would still read `?planId=`, which opens a workout rather than continuing
one, so a reload part-way through would have started a second session.

Underneath, every action is still a plain form post to the same handler, which is what
runs when htmx is not there. `IsHtmxRequest` is the only branch, and it decides between
the partial and the redirect that has always followed a post here — so the no-JavaScript
path is not a separate code path to keep in step.

Set inputs are named by set number rather than row position (`Weight_{id}_{setNumber}`),
and the numbering runs past the highest set recorded rather than past the count. Both
matter more than they look: the draft autosave keys off the input name, so
position-named rows put the weight just logged for set 1 into the row offered for set 3,
and removing a set leaves a gap that counting would fill with a number already in use.
`LogSetAsync` numbers new sets the same way for the same reason.

`Services/GuidedWorkoutFlow.cs` holds the ordering, database-free like `UnitConverter`
so it can be tested without a context, and it decides "answered for" with the same
WDM-54 rule everything else derived from a workout uses: performed, or deliberately
skipped. The wrap-up section stays rendered in both modes — the notes textarea is where
`WorkoutNotes` is posted from, so hiding rather than merely de-emphasising it would
silently blank the notes on completion.

### Canonical Measurement Units

- [x] Store every weight in kilograms and every distance in kilometres
- [x] Convert at the display boundary only (`Services/UnitConverter.cs`)
- [x] Derive the distance unit from the existing weight preference
- [x] Record duration and distance on a set
- [x] Add database migration

Every stored weight is kilograms and every stored distance is kilometres, whatever
the user prefers to read, so comparisons, aggregates and personal records are
unit-safe by construction instead of by everybody remembering. `UnitConverter` is the
only place canonical storage meets a display unit: nothing else converts or formats a
stored measurement, because a second conversion site is how a plausible-looking number
ends up in the wrong unit. Services return canonical values and views convert once at
render. `DisplayUnits.ForUserAsync` exists for the services that build prose, a CSV or
a PDF and so have no view to convert on their behalf; it is kept apart from the
converter so the converter stays pure and testable without a database.

There is no distance preference. `lbs` implies miles and `kg` implies kilometres,
derived from the weight preference that already existed — no second setting to keep in
step, and no way for the two to disagree.

Two rounding depths, for one reason. Canonical values keep four decimals and displayed
ones two, because at two decimals a display round trip does not survive: 45 lbs stores
as 20.41 kg and comes back as 44.99 lbs. Duration is seconds and is never converted.
Progressive-overload increments are chosen in the user's own unit, because a 5 lb jump
and a 5 kg jump are not the same increment and converting one into the other lands on
a weight nobody can load.

`Set.Distance` is new; `Set.Duration` was already on the model but nothing ever wrote
to it. Both are logged now, and the set input shows only the fields the exercise's
category can use — a run has no reps, a bench press has no distance — falling back to
the weight-and-reps shape the app has always shown for anything unrecognised.

Existing rows are deliberately not back-filled. Before this change a stored weight held
whatever number the user typed in their own unit; from here it means kilograms, so a row
written earlier by an lbs user is reinterpreted and reads about 2.2× too high. The
application has never been deployed and has no production data, so the documented reset
is the one already in `CLAUDE.md`: delete `FitTracker.db` and let migrations and seeding
rebuild it. A conversion `UPDATE` keyed on each user's `PreferredUnits` is the
alternative if that ever stops being true — it is not written, because it can only be
run once and cannot tell an already-converted database from an unconverted one. The
reasoning is on the `CanonicalMeasurementPrecision` migration, which is itself
deliberately empty and deliberately kept: SQLite has no decimal type and EF maps
`decimal` to `TEXT`, so precision and scale are model metadata that never reach the
database and there is no column to alter, but deleting the migration would leave the
model snapshot ahead of the migration history.

### Calendar View

- [x] Create Calendar page (`/Calendar`)
  - [x] Display monthly calendar
  - [x] Show workouts on completed dates
  - [x] Show planned workouts
  - [x] Click date to plan/view workout
- [x] Implement calendar with Alpine.js or library
- [ ] Add drag-and-drop for planning (optional)
- [x] Style with Tailwind CSS
- [x] Make responsive (mobile view: list/agenda)

### Personal Records Tracking

- [x] Create `PersonalRecord` model
  - [x] Id, UserId, ExerciseId, Weight, Reps, Date, OneRepMax
- [x] Add database migration
- [x] Implement PR detection logic
  - [x] Automatically detect PRs when workout is completed
  - [x] Create PR entry
- [x] Create PR celebration UI (toast/modal)
- [x] Create PRs page (`/PRs`)
  - [x] List all PRs by exercise
  - [x] Filter by date range
  - [x] Show progression over time
- [x] Display PRs on exercise details page
- [x] Style with Tailwind CSS

### Weekly & Monthly Summaries

- [x] Extend `AnalyticsService` for weekly/monthly calculations
  - [x] Total workouts in period
  - [x] Workout frequency
  - [x] Volume comparison (week-over-week, month-over-month)
  - [x] Muscle group distribution
  - [x] Rest days
- [x] Create Weekly Summary page (`/Analytics/Weekly`)
  - [x] Display week picker
  - [x] Show all weekly stats
  - [x] Week-over-week charts
- [x] Create Monthly Summary page (`/Analytics/Monthly`)
  - [x] Display month picker
  - [x] Show all monthly stats
  - [x] Month-over-month charts
  - [x] Adherence percentage
- [x] Add charts using chart library (Chart.js or similar)
- [x] Style with Tailwind CSS
- [x] Make responsive

### Progress Charts

- [x] Implement charting library
- [x] Create Exercise Progress page (`/Progress/Exercise/{id}`)
  - [x] Line chart of weight progression over time
  - [x] Volume progression
  - [x] Show PRs on chart
- [x] Create Overall Progress page (`/Progress`)
  - [x] Total volume over time
  - [x] Workout frequency over time
  - [x] Body weight over time (if tracked)
- [x] Make charts dark mode compatible
- [x] Make charts responsive

---

## Phase 3: Advanced Features

### Body Measurement Tracking

- [x] Create `BodyMeasurement` model
  - [x] Id, UserId, Date, Weight, BodyFatPercentage, Chest, Waist, Arms, Legs, Notes
- [x] Add database migration
- [x] Create Measurements page (`/Measurements`)
  - [x] List all measurements
  - [x] Add new measurement
  - [x] Edit measurement
  - [x] Delete measurement
- [x] Create charts for measurement trends
- [x] Add to monthly summary
- [x] Style with Tailwind CSS

### Progress Photos

- [x] Create `ProgressPhoto` model
  - [x] Id, UserId, Date, PhotoPath, Notes
- [x] Add database migration
- [x] Create Progress Photos page (`/Photos`)
  - [x] Upload photos
  - [x] View photo gallery
  - [x] Compare photos (before/after view)
  - [x] Delete photos
- [x] Implement file upload handling
- [x] Store photos securely
- [x] Add image optimization
- [x] Style with Tailwind CSS

### Advanced Analytics

- [x] Create Analytics Dashboard (`/Analytics`)
  - [x] Most worked muscle groups
  - [x] Least worked muscle groups
  - [x] Workout duration averages
  - [x] Volume trends
  - [x] PR timeline
  - [x] Workout consistency (heatmap)
- [x] Implement advanced calculations
- [x] Create interactive charts
- [x] Export analytics to PDF (optional)
- [x] Style with Tailwind CSS

### 1RM Estimates

- [x] Implement 1RM calculation formulas
  - [x] Epley formula
  - [x] Brzycki formula
  - [x] Average of multiple formulas
- [x] Display 1RM estimates on exercise pages
- [x] Show 1RM progression over time
- [x] Add to PR tracking
- [x] Create 1RM calculator tool page

### Workout Suggestions

- [x] Implement suggestion algorithm
  - [x] Suggest exercises based on least-worked muscle groups
  - [x] Suggest workouts based on plans and history
- [x] Display suggestions on dashboard
- [x] Add "Use Suggestion" quick action
- [x] Style with Tailwind CSS

### Data Export

- [x] Create Export page (`/Export`)
  - [x] Export workouts to CSV
  - [x] Export workouts to JSON
  - [x] Export measurements to CSV
  - [x] Export PRs to CSV
  - [x] Date range filter for export
- [x] Implement export functionality
- [x] Add download trigger
- [x] Style with Tailwind CSS

---

## Phase 4: Enhancements & Polish

### Achievements & Gamification

- [x] Create `Achievement` model
  - [x] Id, Name, Description, Icon, Criteria
- [x] Create `UserAchievement` model
  - [x] Id, UserId, AchievementId, UnlockedDate
- [x] Add database migration
- [x] Define achievement criteria
  - [x] First workout
  - [x] 10 workouts
  - [x] 30-day streak
  - [x] 100 total sets
  - [x] First PR
  - [x] 10 PRs
  - [x] 1M total volume
- [x] Implement achievement detection logic
- [x] Create achievement unlock notification
- [x] Create Achievements page (`/Achievements`)
  - [x] Show all achievements (locked/unlocked)
  - [x] Show progress to locked achievements
- [x] Style with Tailwind CSS

### Challenges

Two deliberate changes from the original shape, both to match the Achievements
feature that this mirrors:

- The window is per user, not per challenge. `Challenge` carries `DurationDays`
  and `UserChallenge` carries `StartedDate`, instead of fixed `StartDate`/
  `EndDate` on the definition. Seeded challenges with absolute dates would ship
  already expired, and there is no admin UI to create dated ones.
- Progress is derived, not stored. Achievements recompute from workout data on
  read and persist only the unlock; challenges do the same and persist only the
  join and the completion. A stored counter would have to be updated on every
  write path and would silently drift when a past workout is edited or deleted.

- [x] Create `Challenge` model
  - [x] Id, Name, Description, Icon, GoalType, Goal, DurationDays
- [x] Create `UserChallenge` model
  - [x] Id, UserId, ChallengeId, StartedDate, CompletedDate
- [x] Add database migration
- [x] Create Challenges page (`/Challenges`)
  - [x] List available challenges
  - [x] Join challenge (re-joining restarts the window)
  - [x] Track progress
- [x] Implement challenge types
  - [x] 30-day workout challenge
  - [ ] Progressive overload challenge
  - [x] Volume challenge
  - [x] Total sets challenge
- [x] Display active challenges on dashboard
- [x] Style with Tailwind CSS

Progressive overload is still open because it has no agreed definition. The
other types map onto metrics that already exist; this one needs a decision
first — most cheaply "each week's volume beats the previous week's, N weeks
running", which `AnalyticsService` already has the weekly figures for.

### Mobile Optimizations

- [x] Audit mobile performance
- [x] Optimize touch targets (minimum 44×44px)
- [ ] Implement swipe gestures with Alpine.js
  - [ ] Swipe to navigate between workouts
  - [ ] Swipe to delete items
- [ ] Add haptic feedback (where supported)
- [x] Optimize page load times
- [ ] Add app install prompt for PWA
- [x] Test on multiple mobile devices

The audit measured every interactive element across seven pages at 390×844.
Findings: no horizontal overflow anywhere, first contentful paint between 44 ms
and 268 ms on 22–87 KB transfers, and 133 undersized touch targets that turned
out to have only three causes rather than needing per-page work.

`.btn` was 42px tall — two pixels short, which failed on every page at once —
and `.chip` was 30px where it was used as a button. Both are now at least 44px,
the chip rule scoped to `:is(a, button)` so static status pills stay compact.
Every non-heatmap offender is gone: 49 down to 0.

Page load times were already good enough that optimising them would have meant
inventing work, so that item is checked as verified rather than changed.

Verified across iPhone SE, iPhone 13, iPhone 14 Pro Max, Pixel 7, Galaxy S8 and
iPad mini over nine pages — 54 page loads, zero horizontal overflow, zero
undersized targets outside the one exception below.

The Analytics consistency heatmap keeps 84 cells at 36–44px wide depending on
device. Seven columns on a 360–390px screen cannot reach 44px without either
horizontal scrolling or gutters tight enough to look broken; tightening the grid
gap on small screens recovered what was available (37px to 41px on a 390px
screen). The cells are 101px tall, so the tappable area is comfortable even
though the width misses the guideline.

Swipe gestures and haptics are deliberately not done:

- Horizontal swipe competes with the browser's own back/forward edge gesture on
  both iOS and Android, and swipe-to-delete puts a destructive action behind an
  invisible control with no undo in this app. Deleting stays an explicit button
  with a confirmation.
- `navigator.vibrate()` is the only haptics API on the web and Safari on iOS does
  not support it at all, so the work would ship Android-only feedback for an app
  used mostly on phones.

The PWA install prompt is blocked on the PWA Support section below. The app is
now installable from the browser menu, but the custom in-app prompt needs a
service worker, which is deferred there.

### PWA Support

- [x] Create `manifest.json`
  - [x] App name, icons, colors
  - [x] Display mode (standalone)
  - [x] Start URL
- [ ] Create service worker
  - [ ] Cache static assets
  - [ ] Offline fallback page
- [ ] Add installable app prompt
- [ ] Test PWA installation
- [x] Add app icons (various sizes)

Split deliberately: the installable shell is done, the service worker is not.

Chrome dropped the service worker requirement for installing from the browser
menu in version 108 on mobile and 112 on desktop, so a manifest and icons alone
give a real home screen install with a standalone window. `beforeinstallprompt`
— a custom in-app install button — still requires a service worker with a
`fetch()` handler, which is why that item stays open.

Icons are generated from the same lightning bolt already inline in
`_Layout.cshtml`, at 192 and 512 in both `any` and `maskable` purposes, with the
maskable pair full-bleed and the glyph kept inside the 80% safe circle so
Android's mask cannot clip it. iOS ignores manifest icons for the home screen,
so there is a separate `apple-touch-icon`. The manifest can only carry one
`theme_color`, so the `theme-color` meta tag is updated alongside the existing
dark mode toggle to keep the installed app's status bar in step.

Verified programmatically: the manifest parses and carries every required field,
the declared icon sizes match the files' real dimensions, and all assets serve
with correct MIME types. Installing on a physical device is still worth doing
before checking off "Test PWA installation" — note that installability needs
HTTPS in production, which the nginx/caddy setup already provides; localhost is
exempt for local testing.

The service worker is deferred rather than forgotten. On an authenticated,
server-rendered app the risk is concrete: caching personalised HTML could serve
one user's dashboard to another on a shared device, and a faulty service worker
persists in browsers and is hard to recall, so it needs a deliberate kill switch
before it ships. The payoff is an offline page for an app that cannot function
offline anyway.

Offline workout logging — the feature that would actually justify a service
worker — is a much larger change and is not in this section. It needs a JSON API
(the app currently has none, every mutation is a Razor Pages form post),
IndexedDB, a sync queue and conflict resolution. Worth its own decision rather
than a checkbox.

### Advanced 1RM Tracking

- [x] Create 1RM history tracking
- [x] Display 1RM progression charts
- [x] Add 1RM predictions based on trends
- [x] Create 1RM leaderboard (personal, by exercise)
- [x] Style with Tailwind CSS

Everything lives on `/Progress/OneRepMax` — leaderboard, per-exercise history, chart
and projection on one page, the exercise chosen with `?exerciseId=`. A second route
would have split three views of the same twenty rows across two pages.

`OneRepMaxCalculator` now follows `Specifications/1RM_Calculation.md`: Lombardi
joins Epley and Brzycki, and the reported figure is the average of all three,
averaged unrounded and rounded once at the end rather than compounding three
roundings.

Two rules decide what counts, and they are the whole feature:

- **Which exercises.** `Exercise.TracksOneRepMax` is an explicit column, seeded
  and backfilled from "strength, and the equipment isn't the lifter". A derived
  rule would have been free but silently wrong forever on a custom weighted dip;
  a stored flag can be corrected per exercise. Running, yoga and planks are out.
- **Which sets.** 3–10 reps, per the spec. A single is also counted, at face
  value — the spec's own Python special-cases `reps == 1` in all three formulas,
  and a measured max is not an estimate. Doubles and 11+ produce nothing rather
  than a number nobody should train to.

Nothing is stored. History, ranking and projection all recompute from logged sets
on read, like achievements and challenges, so editing a past workout is reflected
immediately.

The projection is a least-squares fit of 1RM against elapsed days, extended to 30,
60 and 90 days out and drawn as a dashed continuation of the history line. It is
withheld below three sessions or a two-week span — a line through two points a
week apart predicts gains nobody makes — and R² is shown next to it, because a
straight line through scattered results still looks confident.

Two existing behaviours needed care, both because a set that used to produce a
number now produces none:

- PR detection ranks on estimated 1RM where there is one and falls back to raw
  weight and reps where there isn't, so bodyweight and high-rep records still
  register instead of vanishing.
- The per-session estimate on `/Progress/Exercise/{id}` now takes the best
  estimate any set in the session can produce, rather than estimating from the
  heaviest set — which, if it was a 12-rep grinder, now estimates nothing.

### AI-Based Workout Suggestions

- [ ] Choose an LLM strategy. Probably Ollama Cloud.
- [ ] Analyze workout history patterns
- [ ] Suggest optimal rest days
- [ ] Suggest exercises based on:
  - [ ] Muscle recovery time
  - [ ] Last worked date
  - [ ] User preferences
  - [ ] Progressive overload opportunities
- [ ] Display AI suggestions on dashboard
- [ ] Add "Accept Suggestion" quick action
- [ ] Style with Tailwind CSS

### Performance Improvements

- [ ] Implement query optimization
  - [ ] Add database indexes
  - [ ] Use eager loading where appropriate
  - [ ] Implement caching for frequently accessed data
- [ ] Optimize front-end bundle size
- [ ] Lazy load images
- [ ] Implement pagination on all list views
- [ ] Run performance profiler
- [ ] Fix any bottlenecks

### Accessibility Enhancements

- [ ] Audit with accessibility tools (Lighthouse, axe)
- [ ] Ensure proper ARIA labels
- [ ] Implement keyboard navigation
  - [ ] Tab order
  - [ ] Keyboard shortcuts for common actions
- [ ] Test with screen reader
- [ ] Add skip links
- [ ] Ensure proper heading hierarchy
- [ ] Add focus indicators
- [ ] Support high contrast mode
- [ ] Allow font size adjustment
- [ ] Fix any accessibility issues

### Final Polish

- [ ] Code review and refactoring
- [ ] Remove unused code
- [ ] Optimize CSS (remove unused Tailwind classes)
- [ ] Update all documentation
- [ ] Create deployment guide
- [ ] Add error logging and monitoring
- [ ] Implement user feedback mechanism
- [ ] Create help/FAQ section
- [ ] Final testing across all features
- [ ] Security audit
- [ ] Performance testing with large datasets

---


### Future Considerations
- [ ] Review "Features to Consider" from Idea document
- [ ] Prioritize new features based on user demand
- [ ] Plan next development phase

---

## Notes

- This is a living document - update as implementation progresses
- Checkboxes can be marked as you complete each task
- Feel free to reorder tasks based on dependencies and priorities
- Some tasks may be split into smaller subtasks as needed
- Testing should be ongoing throughout all phases
