# FitTracker User Guide

This guide walks through the main Phase 1 MVP features available in FitTracker.

## Getting Started

### 1. Create an Account

1. Open the application.
2. Select `Create account`.
3. Enter your email and password.
4. Submit the registration form.
5. Confirm your email if prompted.

In development, confirmation links may be shown directly in the app instead of being delivered by email.

### 2. Complete Profile Setup

After your first sign-in, FitTracker asks for a few basic preferences:

- preferred units (`lbs` or `kg`)
- default rest timer length
- primary training goals

These values help personalize workout logging and account setup.

### 3. Sign In

Use the `Log in` page to access your dashboard.

If you forget your password, use `Forgot your password?` to generate a reset link.

## Dashboard

The dashboard is your main landing page after sign-in.

It includes:

- workouts completed this week
- current streak
- recent workout history
- today's workout status
- daily summary totals
- up to three of your active plans, each with a `Start plan` action

Use the quick actions to jump directly into:

- starting a workout
- browsing the exercise library
- viewing daily analytics

## Exercise Library

Open `Exercises` to browse the exercise database.

### What You Can Do

- view all available exercises
- search by exercise name
- search by muscle group text
- filter by category
- filter by equipment
- open exercise details pages

### Exercise Details

Each exercise details page can show:

- exercise description
- equipment used
- video link, if available
- your usage count
- last performed date
- best logged set

## Templates and Plans

FitTracker keeps the reusable pieces separate from the thing you train from.

A template is a grouping of exercises you expect to reuse: a warm-up, an accessory block, or a whole session. Templates are the ingredients, and they exist to build plans from.

A plan is the recipe. It is an ordered list of exercises with the targets you are aiming at, and it is what a workout is performed from. Creating a plan does not start any training — it only describes a session you can start later.

Open `Templates` and `Plans` from the main navigation.

### Built-in Templates

FitTracker ships with 25 ready-made templates: gym sessions and splits, home workouts with dumbbells or a rack, outdoor running and cycling, and warm-ups.

- available to everyone; they belong to no account
- browsable at `/Templates/Catalog` without signing in, with every exercise and target listed
- shown alongside your own on `Templates`, where the `Built-in` filter narrows the list to just these
- not editable, because they are not yours

Use `Copy` to take one for yourself. The copy is named with a `(copy)` suffix, belongs to you, and opens straight in the editor — changing it is the reason to copy it.

### Your Own Templates

- create one with `Create template`
- edit or delete any template you own
- give each exercise optional defaults: sets, reps, duration, and distance
- mark a template active or paused

A template has no `Start workout` action. Build a plan from it, and perform the plan.

### Building a Plan

Open `Plans`, then `Create plan`.

1. Name the plan and add an optional description.
2. Apply one or more templates. Each `Apply template` appends that template's exercises and targets to the end of the plan, so you can stack a warm-up onto a main block.
3. Add individual exercises from the library. A newly added exercise starts at 3 sets of 10.
4. Reorder with `Move up` and `Move down`, or drop a line with `Remove`.
5. Set the targets you want per exercise: sets, reps, duration in seconds, distance, and a note.

Every target is optional. Leave a field blank and the plan simply says nothing about it — a stretch prescribes a duration and no reps, a run a distance and neither.

If the same exercise ends up in the plan twice, both entries are kept and flagged `Appears more than once` rather than merged. A warm-up's light push-ups and a working set of push-ups are not the same entry, and only you can say which you meant.

A plan needs at least one exercise before it can be saved.

### Retiring and Deleting Plans

An active plan can start a workout. `Deactivate` retires one: it stays on the `Plans` page, shows as `Retired`, and offers `Reactivate` instead of `Start workout`. Nothing is lost, and you can bring it back at any time.

`Delete plan` takes the plan off your list for good. Workouts already performed from it are kept, and they still show what they were aiming at.

### Editing a Plan You Have Already Trained From

Workouts read their targets from the plan itself, every time they are shown. So if you edit a plan that workouts have already been performed from, FitTracker warns you before saving and asks you to confirm with `Save the changes anyway`.

Saving changes what those past workouts show as their intent. It never touches a set you recorded — weight, reps, duration, distance, and RPE are exactly as you logged them. No copy of the earlier version of the plan is kept.

## Starting a Workout

Open `Workouts` or select `Start workout` from the dashboard to train with nothing planned. To be guided by a plan, use `Start plan` on the dashboard or `Start workout` on the `Plans` page.

Only one workout is open at a time. Starting again on the same day continues the session you already have.

### Workout Flow

1. Start or continue the current workout.
2. Add one or more exercises.
3. Log sets for each exercise.
4. Optionally add workout notes.
5. Complete the workout when finished.

### Guided Workouts

A workout started from a plan opens with the plan's exercises already in place, in order, each showing its `Plan target` and any note the plan carries.

None of it is binding:

- log what you actually did, whether or not it matches the target
- add exercises the plan does not contain, from the same `Add exercise` picker
- remove an exercise from the session entirely
- `Skip` one you are not going to do, and `Un-skip` it if you change your mind
- rate how one felt with `Easy`, `Medium`, or `Hard`, and `Clear` to remove that rating

`Skip` is no longer offered once you have logged a set against an exercise, and logging a set against something you had skipped clears the skip. The record and the mark cannot contradict each other.

Skipping and effort ratings are available in any workout, not only a guided one.

Targets are read from the plan as the page is drawn, so a guided workout always shows the plan's current version.

### One Exercise at a Time

A workout in progress shows you one exercise, not the whole list. Seeing everything is useful when you are putting a session together and a distraction when you are doing one — so the screen shows what to do now: the exercise, what the plan asks for, the inputs to log it, and how it went.

Above it, a progress bar and a numbered strip show how far through you are and what each exercise's state is — green for done, dashed for skipped. Nothing about the order is enforced: tap any number to jump to that exercise, or step with `← Previous exercise` and `Next exercise →`. You can go back into something you have already answered for and change your mind.

Once every exercise has been done or skipped, the card gives way to a summary of the session with each exercise's status, and the workout is ready to complete.

`Show full list` puts the whole lineup back, and `Focus mode` returns. Nothing is lost either way — this changes what you are shown, never what is recorded — and the workout notes and `Complete workout` button stay at the bottom of the page in both. A workout you have already completed always opens as the full list.

### Logging the Sets a Plan Asks For

If the plan says `3 × 10`, you get three rows, each already holding 10 reps. Type the weights, hit `Log 3 sets`, and all three are recorded at once — no adding them one at a time.

You are not held to it:

- **Did fewer sets?** Choose `Clear` on a row you did not do. It empties, and nothing is recorded for it.
- **Did more?** Once the prescribed rows are logged, another row appears for the next set.
- **Did something different?** Type over the reps. The plan proposes; the record is what you enter.

Only rows with something in them are saved, so an untouched row can never turn into a set you did not do.

Exercises with no plan behind them, and plans that do not say how many sets, keep the single `Add set` row.

### RPE You Do Not Have to Type

Leave `RPE` blank and how you rate the exercise fills it in: `Easy` gives 5, `Medium` 7, `Hard` 9. You have already said how it felt, so there is no need to say it again on every row.

Anything you type is yours and stays — change the rating later and a typed RPE does not move, only the ones that came from the rating. Clear the rating and those go back to blank. Values that came from a rating are shown greyed, so you can tell them from the ones you entered.

### Marking an Exercise Done

`Easy`, `Medium`, and `Hard` stay disabled until you have logged at least one set. Marking an exercise done and moving on is meant to record what you actually did, not just an opinion about it — so log the set first, then say how it felt, and the flow moves to the next exercise.

If you are not doing an exercise at all, `Skip this exercise` is always available. That is the honest answer, and it keeps the exercise in the record as skipped.

### Completing a Workout

`Complete workout` and `Cancel workout` both ask first, in a panel in the middle of the screen. Completing tells you how many exercises are still outstanding before you commit; cancelling warns that everything logged in the session goes. Press `Escape`, or the decline button, to go back to the workout.

A workout needs at least one exercise you actually performed before it can be completed: one with a logged set, or one you rated `Easy`, `Medium`, or `Hard`. A plan whose exercises are all untouched is not a session, and will not count towards records, achievements, or challenges.

Skipped exercises stay in the record. They appear on the workout details page, dimmed and marked `Skipped`, because what you deliberately did not do is part of what happened.

### Logged Set Data

Each set can include:

- weight
- reps
- duration
- distance
- RPE

The fields offered follow the exercise. Cardio movements ask for duration and distance instead of weight and reps; core and mobility work adds a duration field to the usual pair; everything else stays weight and reps. Every field is optional.

## Rest Timer

The workout screen includes a built-in rest timer.

### Timer Features

- configurable duration
- preset buttons for common intervals
- start, pause, resume, and reset
- persistence after refresh during an active session
- vibration notification when supported by the browser/device

## Autosave

FitTracker automatically stores in-progress workout form values in local browser storage.

This helps preserve:

- selected exercise
- set entry values
- workout notes
- timer state

If the page refreshes during a workout, your saved draft values should restore automatically.

## Progressive Overload Suggestions

When an exercise has prior workout history, FitTracker can display a progressive overload card.

This panel summarizes:

- when the exercise was last performed
- how many sets you completed
- top set details
- previous total volume
- suggested next target

## Workout History

Open `Workout history` to review completed sessions.

### Available Features

- browse completed workouts
- search by notes or exercise name, matching only exercises you performed
- filter by date range
- page through larger result sets
- open full workout details
- delete a workout
- repeat a workout structure

### Workout Details

The details page includes:

- all logged exercises
- all sets, including their duration and distance
- exercises you skipped, dimmed and marked `Skipped`
- how each exercise felt, if you rated it
- total sets and reps
- total volume
- average RPE
- workout notes

## Daily Analytics

Open `Analytics` to view a daily breakdown.

The daily analytics page shows:

- workout count for the selected date
- exercises completed
- total volume
- total workout duration
- calories burned estimate
- weekly context and date navigation

## Profile Management

Open `Profile` from the main navigation to update account settings.

### You Can Manage

- email confirmation status
- phone number
- preferred units
- default rest timer
- primary goals

## Units

Choose `lbs` or `kg` in `Profile`. The choice applies everywhere a measurement is shown or typed: logging, workout history, analytics, personal records, body measurements, plan and template targets, and exported files.

Everything you have ever logged is converted, not relabelled. Switch from `lbs` to `kg` and a 225 lbs bench press reads as 102.06 kg; switch back and it reads 225 lbs again. Earlier versions of FitTracker changed the label and left the number alone, which is no longer the case.

Distances follow the same preference:

- `lbs` shows distances in miles (`mi`)
- `kg` shows distances in kilometres (`km`)

Durations are seconds whichever you choose, and are never converted.

## Dark Mode

Dark mode is available throughout the application.

### Behavior

- toggle from the main layout
- preference persists across refreshes
- supported across dashboard, workouts, analytics, exercises, and identity pages

## Troubleshooting

### I refreshed the workout page and lost my data

Check whether browser local storage is enabled. Autosave relies on local storage.

### I do not see my confirmation or reset email

In local development, the app may expose the generated link directly instead of sending a real email.

### My workout is still marked in progress

Open `Workouts` and either:

- continue and complete the session, or
- cancel the workout if you want to discard it

### I started a plan but its exercises did not appear

A workout that is already open for today, and already has exercises in it, is continued rather than replaced. Complete or cancel that workout first, then start the plan.

### I cannot complete my workout

At least one exercise has to have been performed. Log a set against one, and rate it `Easy`, `Medium`, or `Hard`.

### Easy, Medium and Hard are greyed out

They unlock once the exercise has at least one recorded set — marking something done is meant to record what you did, not only how it felt. Log a set, or choose `Skip this exercise` if you are not doing it. In `Show full list` they are available without a set.

### A row I did not do got recorded anyway

Rows that a plan prescribes arrive with the target reps already filled in, so a row left alone is still a row with something in it. Choose `Clear` on any row you are not doing before you log — or remove the set from the table afterwards.

### The Skip button is missing on an exercise

Skipping is only offered while an exercise has no sets recorded against it. Remove those sets first if you meant to skip it.

### A plan I retired is not offered when starting a workout

Retired plans cannot guide a workout. Open `Plans` and use `Reactivate`.

## MVP Scope Notes

This guide covers the current Phase 1 MVP functionality.

Planned future features such as calendar planning, personal records, and advanced analytics are tracked in `Specifications\Implementation.md`.
