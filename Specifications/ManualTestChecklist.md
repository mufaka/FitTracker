# FitTracker Manual Test Checklist

Use this checklist to validate the Phase 1 MVP in a local development environment.

## Test Environment

- Application builds successfully
- Database migrations applied
- Seed data available
- Built-in template catalog seeded (25 ownerless templates)
- Browser local storage cleared before dark mode and autosave checks when needed
- A signed-out browser or private window available for the catalog checks

## Authentication & Account

### Registration

- [ ] Open `/Identity/Account/Register`
- [ ] Register a new user with a valid email and password
- [ ] Confirm registration succeeds
- [ ] Verify email confirmation screen is shown
- [ ] In development, open the generated confirmation link
- [ ] Confirm the email confirmation page reports success

### Login / Logout

- [ ] Open `/Identity/Account/Login`
- [ ] Log in with a valid confirmed account
- [ ] Confirm redirect to profile setup for a first-time user
- [ ] Complete setup and confirm redirect to the app
- [ ] Sign out successfully
- [ ] Log back in and confirm normal sign-in flow

### Password Reset

- [ ] Open `Forgot password`
- [ ] Request a reset link for an existing account
- [ ] In development, use the generated reset link
- [ ] Set a new password successfully
- [ ] Confirm login works with the new password

### Account Management

- [ ] Open `/Identity/Account/Manage/Index`
- [ ] Confirm email address and confirmation status are displayed
- [ ] Update phone number
- [ ] Update preferred units
- [ ] Update default rest timer
- [ ] Update goals
- [ ] Confirm changes persist after refresh

## Dashboard

- [ ] Open `/Index`
- [ ] Confirm today's date is shown
- [ ] Confirm quick stats render without errors
- [ ] Confirm daily summary renders correctly
- [ ] Confirm recent workouts list renders when workout data exists
- [ ] Confirm empty states render correctly for a new account
- [ ] Confirm `Plans` appears in the sidebar and in the mobile menu
- [ ] Confirm the "Start from a plan" card lists up to three active plans and starts one directly
- [ ] Confirm the suggestion card reads "Suggested plan" and its `Use suggestion` action starts that plan

## Exercise Library

- [ ] Open `/Exercises/Index`
- [ ] Confirm seeded exercises are listed
- [ ] Search by exercise name
- [ ] Search by muscle group text
- [ ] Filter by category
- [ ] Filter by equipment
- [ ] Open an exercise details page
- [ ] Confirm details, video link, and user usage stats display correctly

## Workout Templates

### Ownership Filter

- [ ] Open `/Templates/Index`
- [ ] Confirm the All / Yours / Built-in chips render and the selected one is highlighted
- [ ] Confirm `All` lists your own templates first, then the built-ins, each group sorted by name
- [ ] Select `Yours` and confirm only templates you created are listed
- [ ] Select `Built-in` and confirm only seeded templates are listed
- [ ] Delete a personal template and confirm the list returns with the same filter still selected

### Built-in Templates

- [ ] Confirm a built-in card carries a `Built-in` chip and offers `Copy` only
- [ ] Confirm no `Edit` link and no `Delete template` button appear on a built-in
- [ ] Confirm a personal card offers `Copy`, `Edit`, `Delete template`, and an Active / Paused chip
- [ ] Open `/Templates/Create/{id}` for a built-in id directly and confirm you are redirected to `/Templates/Index`

### Copying a Template

- [ ] Copy a built-in and confirm you land in the editor for a new template named `<original> (copy)`
- [ ] Confirm the copy appears under `Yours` and offers Edit and Delete
- [ ] Confirm the copy's exercises, order, targets, and notes match the original
- [ ] Rename the copy, change a target, remove an exercise, and save
- [ ] Reopen the original built-in and confirm it is unchanged
- [ ] Copy the same built-in again and confirm the name is still `<original> (copy)`, not `(copy) (copy)`

### Template Editor

- [ ] Add an exercise the template already contains and confirm it is refused
- [ ] Move a row up and down and confirm sets, reps, duration, distance, and notes move with the exercise name
- [ ] Remove a middle row and confirm the remaining rows keep their own values
- [ ] Save with no exercises and confirm it is refused

## Template Catalog

- [ ] Sign out, or use a private window, and open `/Templates/Catalog`
- [ ] Confirm the page renders without redirecting to login
- [ ] Confirm 25 templates are listed, sorted by name
- [ ] Confirm every exercise of every template is listed with its position, muscle groups, equipment, and target chip
- [ ] Confirm per-exercise notes render where present (for example "Each side.")
- [ ] Confirm no personal data appears: no signed-in navigation, no templates you own, no `Copy` action
- [ ] Confirm the header offers `Create an account` and `Sign in` while signed out
- [ ] Sign in, reload the page, and confirm each card now offers `Copy` and the header offers `Your templates` and `Build a plan`
- [ ] Copy from the catalog and confirm you land in the editor for the new owned copy

## Workout Plans

### Building a Plan from Templates

- [ ] Open `/Plans/Index` and choose `Create plan`
- [ ] Enter a name and apply a template
- [ ] Confirm every exercise from the template is appended in the template's order
- [ ] Confirm sets, reps, duration, distance, and notes come across with it
- [ ] Confirm the template picker resets to "Select a template..." after applying
- [ ] Apply a second template and confirm its exercises are appended after the first block rather than merged into it
- [ ] Confirm the exercise count chip matches the combined total
- [ ] Apply a template that repeats an exercise the plan already holds
- [ ] Confirm both rows are kept and each carries an "Appears more than once" badge
- [ ] Confirm the duplicated rows keep their own targets rather than sharing one

### Ordering and Removing

- [ ] Give two adjacent rows visibly different targets
- [ ] Move a row up and down and confirm its sets, reps, duration, distance, and notes travel with it
- [ ] Confirm the `Exercise n` chips renumber after a move
- [ ] Confirm `Move up` is disabled on the first row and `Move down` on the last
- [ ] Remove a middle row and confirm the remaining rows keep their own values
- [ ] Remove one of a duplicated pair and confirm the duplicate badge clears

### Saving a Plan

- [ ] Save and confirm a redirect to `/Plans/Index` with the plan listed
- [ ] Confirm the card shows an `Active` chip, the exercise count, and the first four exercises in order with a prescription chip each
- [ ] Confirm a plan with more than four exercises shows "+ n more"
- [ ] Confirm a prescription chip renders only the parts that were set, and "No target" when none were
- [ ] Save with no name and confirm the required-field message appears
- [ ] Save with no exercises and confirm it is refused
- [ ] Reopen the saved plan for editing and confirm every target round-trips unchanged

### Retiring, Reactivating, and Deleting

- [ ] Choose `Deactivate` and confirm the chip changes to `Retired` and `Start workout` disappears
- [ ] Confirm the retired plan is still listed and offers `Reactivate`
- [ ] Confirm the retired plan no longer appears in the dashboard plan list
- [ ] Open `/Workouts/Start?planId={id}` for the retired plan and confirm it is not found
- [ ] Choose `Reactivate` and confirm `Start workout` returns
- [ ] Choose `Delete plan`, confirm the browser prompt appears, and confirm the plan disappears from `/Plans/Index`
- [ ] Confirm the deleted plan is gone from the dashboard plan list
- [ ] Confirm a workout already performed from the deleted plan still opens from history with its sets intact

### Editing a Plan a Workout Was Performed From

- [ ] Perform and complete a workout from a plan, logging at least one set
- [ ] Open that plan for editing, change a target, and choose `Save plan`
- [ ] Confirm the "This plan has already been trained from" panel appears instead of the plan saving
- [ ] Confirm the panel offers `Save the changes anyway` and `Leave the plan as it is`
- [ ] Choose `Leave the plan as it is` and confirm the change was not saved
- [ ] Repeat, choose `Save the changes anyway`, and confirm the plan saves and returns to `/Plans/Index`
- [ ] Open the completed workout from history and confirm its recorded sets, weights, reps, and RPE are unchanged
- [ ] Edit the same plan again and confirm the warning appears again — the acknowledgement is per save, not remembered
- [ ] Edit a plan no workout has been performed from and confirm the warning does not appear

## Workout Logging

### Start Workout

- [ ] Open `/Workouts/Start`
- [ ] Confirm a new in-progress workout is created for the user
- [ ] Add an exercise successfully
- [ ] Add multiple exercises successfully
- [ ] Remove an exercise successfully

### Guided Workout from a Plan

- [ ] Choose `Start workout` on an active plan from `/Plans/Index`
- [ ] Confirm one card appears per planned exercise, in plan order
- [ ] Confirm each card carries a `Planned` chip and a `Plan target:` line matching the plan
- [ ] Confirm a target that sets only a duration, or only a distance, renders just that part
- [ ] Confirm a per-exercise plan note renders under the target
- [ ] Confirm no sets exist yet and each set input reads `Add set #1`
- [ ] Confirm the set inputs suit the movement: weight and reps for lifts, duration for core and mobility, duration and distance for cardio
- [ ] Edit the plan's targets in another tab, acknowledging the warning, then reload the workout and confirm the target line shows the new values
- [ ] Add an exercise the plan does not name and confirm it appears with no target line
- [ ] Start a plan while an in-progress workout for today already holds exercises, and confirm you continue that session instead, without plan targets

### Exercise Status and Skipping

- [ ] Log a set against a planned exercise and confirm it appears in that card's set table
- [ ] Rate an exercise `Easy`, then `Medium`, then `Hard`, and confirm the chosen chip highlights and the header chip reads "Felt hard"
- [ ] Confirm a `Clear` action appears once a status is set and returns the card to `Planned`
- [ ] Choose `Skip` on an untouched exercise and confirm the card dims, a red `Skipped` chip appears, and the action becomes `Un-skip`
- [ ] Confirm the `Skip` option is absent on an exercise that already has sets
- [ ] Log a set against a skipped exercise and confirm the skip clears, the card is no longer dimmed, and no effort rating was applied on your behalf
- [ ] Choose `Un-skip` on a skipped exercise and confirm it returns to `Planned`

### Focused Flow

- [ ] Start a workout from a plan of at least three exercises and confirm it opens on **one** exercise, the first not yet performed or skipped, with no flag in the URL
- [ ] Confirm the header reads `Exercise 1 of {n}`, the progress bar is empty, and the chip reads `0 of {n} done`
- [ ] Confirm the step strip shows one numbered marker per exercise with the current one filled in
- [ ] Confirm the focused card still shows its `Plan target`, note, and the set inputs suited to the movement
- [ ] Confirm `Easy` / `Medium` / `Hard` are disabled before any set, with the reason given underneath, and `Skip this exercise` is not
- [ ] Choose `Skip this exercise` and confirm the flow moves to exercise 2 and marker 1 turns dashed
- [ ] Log a set and confirm the rating buttons enable, the flow **stays** on that exercise, and the set is in its table
- [ ] Rate the exercise `Medium` and confirm the flow moves on and the marker turns green
- [ ] Confirm `Skip this exercise` is absent once a set is logged, and `Clear this answer` leaves the flow where it is
- [ ] Tap an earlier marker in the strip and confirm that exercise is shown even though it is already answered for
- [ ] Confirm `← Previous exercise` and `Next exercise →` step through in order and are absent at the ends
- [ ] Open `Add an exercise that isn't in the lineup`, add one, and confirm it appears at the end of the strip
- [ ] Confirm that panel starts open while the workout has one exercise or none
- [ ] Answer for every exercise and confirm the finish screen replaces the card, listing each exercise with its status and linking back to it
- [ ] Confirm `Show full list` returns to the whole lineup with every status preserved, and `Focus mode` returns
- [ ] Open a completed workout at `/Workouts/Start?id={id}` and confirm it renders as the full list, with no focused flow
- [ ] Type workout notes in the flow, complete the workout, and confirm the notes were saved
- [ ] Repeat the flow at phone width and confirm nothing scrolls sideways and the step strip scrolls on its own

### In-Place Updates

- [ ] Start the rest timer, then log a set, and confirm the timer keeps counting down without restarting
- [ ] Confirm the browser's loading indicator never spins for these actions — the page is not reloading
- [ ] Repeat for rating an exercise, skipping one, stepping with the strip and with Previous/Next, removing a set, and adding an exercise
- [ ] Confirm the address bar follows the flow — after several actions it reads `?id={id}&at={workoutExerciseId}`, not the `?planId=` it opened with
- [ ] Reload at that point and confirm you continue the same workout rather than starting a second one
- [ ] Confirm the button pressed dims while its request is in flight and cannot be pressed twice
- [ ] Scroll down mid-exercise, log a set, and confirm the view returns to the top of the current exercise rather than jumping to the page top
- [ ] Switch to `Show full list`, rate an exercise, and confirm it stays in list mode and does not scroll
- [ ] Choose `Complete workout` and confirm it is a full navigation to the workout details page
- [ ] Choose `Cancel workout` on another session and confirm it navigates to the dashboard
- [ ] Disable JavaScript and confirm logging sets, rating, skipping and stepping all still work as ordinary form posts and links

### Prescribed Set Rows

- [ ] Open an exercise a plan prescribes `3 × 10` for and confirm three rows appear, each with reps pre-filled to 10 and the button reading `Log 3 sets`
- [ ] Confirm recorded sets and the rows still on offer are in the same table, numbered continuously
- [ ] Fill two rows, choose the `×` on the third, and confirm every input in that row empties
- [ ] Submit and confirm exactly two sets were recorded, numbered 1 and 2 — the cleared row must record nothing
- [ ] Confirm one row is now offered, numbered `Set 3`, being the remainder of the prescription
- [ ] Log it, and confirm a single further row appears numbered `Set 4`, so an extra set stays in reach
- [ ] Remove the first set from the table and confirm the row on offer keeps counting up rather than reusing a number still in the table, and arrives empty
- [ ] Confirm an exercise with no prescription, or one the plan gives no `TargetSets`, shows a single `Add set #n` row as before
- [ ] Confirm a cardio exercise's rows carry duration and distance, pre-filled from the plan's target and in your own units
- [ ] Confirm a plan target of `5 km` shows as `3.11` for a lbs/miles user
- [ ] Confirm the rows never scroll sideways at phone width

### RPE Supplied by the Effort Rating

- [ ] Log two sets, one with `RPE` left blank and one with `8` typed
- [ ] Rate the exercise `Hard` and confirm the blank one now reads `9`, greyed, and the typed `8` is unchanged
- [ ] Re-rate `Easy` and confirm the supplied value becomes `5` while the `8` stays put
- [ ] Choose `Clear this answer` and confirm the supplied value goes back to `–` and the `8` remains
- [ ] Confirm `Easy` / `Medium` / `Hard` give 5 / 7 / 9
- [ ] In `Show full list`, rate an exercise *before* logging anything, then log a set with no RPE, and confirm it takes the rating's value
- [ ] Skip an exercise and confirm no RPE is written anywhere

### Ending a Workout

- [ ] Choose `Complete workout` and confirm a panel appears centred in the viewport over a dimmed page — not a browser dialog
- [ ] Confirm it names how many exercises are still outstanding, when some are
- [ ] Press `Escape` and confirm it closes and the workout is untouched
- [ ] Choose `Keep going` and confirm the same
- [ ] Confirm from the panel and confirm the workout completes and lands on its details page
- [ ] Repeat for `Cancel workout`, confirming the wording warns the session is discarded
- [ ] Disable JavaScript and confirm `Complete workout` still completes, without a confirmation

### Exercise Card Density

- [ ] At 320px, confirm the exercise card is one surface — no bordered box inside a bordered box
- [ ] Confirm each set is a single line at 320px, 390px and desktop, with nothing wrapping
- [ ] Confirm no value is cut off in any column, recorded or being entered — check a weighted timed exercise, which carries four measurement columns
- [ ] Confirm the page never scrolls sideways at 320px
- [ ] Confirm every field and the `×` controls are still at least 44px tall
- [ ] Confirm `Easy` / `Medium` / `Hard` sit three abreast rather than stacked, at every width
- [ ] Confirm the rest timer's `Start` / `Pause` / `Reset` sit three abreast on a phone
- [ ] Confirm the progressive overload note reads as a note against the exercise, not as another card
- [ ] Confirm a completed workout shows the same table with values only — no inputs, no `×`

### Completing a Guided Workout

- [ ] With every exercise still pending, choose `Complete workout` and confirm it is refused with "Record a set or rate an exercise before completing the workout"
- [ ] Skip every exercise and confirm completion is still refused
- [ ] Rate one exercise without logging a set and confirm the workout can now be completed
- [ ] Complete a workout with one exercise performed, one rated, one skipped, and one untouched
- [ ] Confirm `/Workouts/Details/{id}` dims the skipped exercise with a `Skipped` chip, shows `Felt <rating>` on the rated one, `Not performed` on the untouched one, and a set table on the performed one
- [ ] Confirm the detail header's exercise count counts only performed exercises
- [ ] Confirm `/Workouts/History` cards count and list only performed exercises
- [ ] Search history for an exercise that was only skipped and confirm the workout is not returned
- [ ] Search history for an exercise that was performed and confirm the workout is returned
- [ ] Confirm the dashboard's recent workouts count only performed exercises
- [ ] Confirm the calendar day detail counts only performed exercises for a completed workout, but every row for one still in progress

### Sets, Timer, and Autosave

- [ ] Add a set with weight, reps, and RPE
- [ ] Add multiple sets to the same exercise
- [ ] Remove a set successfully
- [ ] Confirm rest timer starts when adding a set
- [ ] Confirm timer can start, pause, resume, and reset
- [ ] Confirm timer persists after refresh during an active countdown
- [ ] Enter unsaved form values and refresh
- [ ] Confirm autosaved values restore correctly

### Progressive Overload

- [ ] Complete a workout with at least one logged exercise
- [ ] Start or continue a later workout with the same exercise
- [ ] Confirm the progressive overload panel appears
- [ ] Confirm suggested reps and/or weight are reasonable based on prior data

### Complete / Cancel Workout

- [ ] Complete a workout successfully
- [ ] Confirm duration and notes are saved
- [ ] Confirm completed workout state is shown
- [ ] Start another workout and cancel it
- [ ] Confirm canceled workout is discarded

## Workout History

- [ ] Open `/Workouts/History`
- [ ] Confirm completed workouts are listed
- [ ] Search by note text
- [ ] Search by exercise name
- [ ] Filter by date range
- [ ] Test pagination when enough workout data exists
- [ ] Open `/Workouts/Details/{id}`
- [ ] Confirm set statistics, total volume, and notes are correct
- [ ] Delete a workout successfully
- [ ] Repeat a workout successfully

## Daily Analytics

- [ ] Open `/Analytics/Daily`
- [ ] Confirm daily totals render for a date with workouts
- [ ] Navigate to previous and next dates
- [ ] Confirm empty-state behavior for a date with no workouts

## Units and Conversion

### Switching Preferred Units

- [ ] Set preferred units to `lbs` at `/Identity/Account/Manage/Index`
- [ ] Log a set of 100 lbs × 5 on a weighted lift and complete the workout
- [ ] Record a body measurement of 200 lbs at `/Measurements`
- [ ] Switch preferred units to `kg`
- [ ] Confirm every surface below shows the converted value — about 45.36 kg and 90.72 kg — rather than the same number relabelled
- [ ] Workout screen: the set table and the weight input's `(kg)` label at `/Workouts/Start`
- [ ] Workout details: weight, per-set volume, the totals row, and the `Total volume` card
- [ ] History: the workout cards, and the dashboard daily summary's `Total volume`
- [ ] PRs: weight and estimated 1RM at `/PRs/Index`
- [ ] Progress: `/Progress/Index` and `/Progress/Exercise/{id}`, including the chart values
- [ ] 1RM tracking: best, current, per-week change, and the Epley / Brzycki / Lombardi table at `/Progress/OneRepMax`
- [ ] Analytics: volumes on `/Analytics/Index`, `/Analytics/Weekly`, `/Analytics/Monthly`, and `/Analytics/Daily`
- [ ] Measurements: the latest weight card, the history table, and the weight field when editing an entry
- [ ] 1RM calculator: `/OneRepMax` labels the result `kg` with no `?units=` in the URL, and does not convert what you type
- [ ] 1RM calculator: open it from an exercise details page and confirm the weight, reps, and unit arrive prefilled in the unit that page rendered
- [ ] Catalog distances: `Easy Run` reads `5 km` and `Long Ride` `20 km` in kg, and `3.11 mi` and `12.43 mi` in lbs
- [ ] Catalog distances: the `Run Intervals` sprints read `0.4 km` in kg and `0.25 mi` in lbs
- [ ] Plans and templates: the prescription chips on both list pages, and the `Distance (km)` / `Distance (mi)` label in both editors
- [ ] Exports at `/Export`: the workouts CSV header reads `Weight (kg)` with converted values (no distance column is exported), and the measurements and personal records CSVs likewise
- [ ] Analytics PDF from `/Analytics/Index` → `Export PDF`: average volume, muscle-group volumes, the weekly volume trend, the PR timeline's best 1RM, and recent PRs all carry the current unit
- [ ] Switch back to `lbs` and confirm every surface returns to the original numbers

### Round-Trip Stability

- [ ] In `lbs`, log a set at 45 lbs and confirm it renders as `45 lbs`, not `44.99 lbs`
- [ ] Open a measurement for editing, save it unchanged, and confirm the weight is identical afterwards
- [ ] Repeat the measurement round trip in `kg` and confirm the same
- [ ] Open a plan whose distance target came from a built-in template, save it unchanged in the same unit, and confirm the target does not move
- [ ] Save that distance target once in the other unit (3.11 mi against a 5 km target), then confirm the re-rounded value settles — about 5.01 km — and does not drift further on repeated saves
- [ ] Switch units back and forth without saving anything and confirm no displayed value changes

### Progressive Overload Increments

- [ ] In `lbs`, complete a top set of 100 lbs × 8, then start a later workout with the same exercise
- [ ] Confirm the panel suggests "Try 105 lbs for 8 reps on your top set." — a 5 lb jump
- [ ] Confirm a top set of 40 lbs × 8 suggests a 2.5 lb jump instead
- [ ] Switch to `kg`, reload, and confirm the same history suggests a 2.5 kg jump from about 45.36 kg, not a converted 2.27 kg
- [ ] Confirm a top set of 20 kg × 8 suggests a 1.25 kg jump
- [ ] Confirm a top set below 8 reps holds the weight and adds a rep instead ("Keep 100 lbs on the bar and aim for 6 reps.")
- [ ] Confirm the `Top set:`, `Volume:`, and `Target:` chips carry the same unit as the prose

## UI / UX Checks

### Dark Mode

- [ ] Toggle dark mode from the main layout
- [ ] Refresh the page and confirm preference persists
- [ ] Check dashboard, exercises, workouts, analytics, and identity pages in dark mode
- [ ] Check plans, templates, the catalog, and a guided workout in dark mode, including the duplicate badge, the edit warning panel, and the skipped and effort chips

### Responsive Design

- [ ] Test mobile width layout
- [ ] Test tablet width layout
- [ ] Test desktop layout
- [ ] Confirm navigation works on small screens
- [ ] Confirm workout logging controls remain usable on touch-sized layouts
- [ ] Confirm the plan builder's move, remove, and target inputs remain usable at mobile width
- [ ] Confirm the status and skip chips on the workout screen remain tappable at mobile width

## Regression / Stability

- [ ] Restart the app and confirm startup succeeds
- [ ] Confirm database seed process does not create duplicate exercise issues
- [ ] Confirm a restart does not duplicate the built-in templates or alter copies made from them
- [ ] Confirm no obvious Razor Page routing errors occur during common navigation
- [ ] Confirm no unexpected validation or server errors appear in core flows

## Issue Log

Record any defects discovered during testing.

| Area | Scenario | Result | Notes |
|---|---|---|---|
| Authentication |  |  |  |
| Templates / Catalog |  |  |  |
| Plans |  |  |  |
| Workout Logging |  |  |  |
| History |  |  |  |
| Analytics |  |  |  |
| Units / Conversion |  |  |  |
| Responsive / Dark Mode |  |  |  |
