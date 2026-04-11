# FitTracker Manual Test Checklist

Use this checklist to validate the Phase 1 MVP in a local development environment.

## Test Environment

- Application builds successfully
- Database migrations applied
- Seed data available
- Browser local storage cleared before dark mode and autosave checks when needed

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

## Exercise Library

- [ ] Open `/Exercises/Index`
- [ ] Confirm seeded exercises are listed
- [ ] Search by exercise name
- [ ] Search by muscle group text
- [ ] Filter by category
- [ ] Filter by equipment
- [ ] Open an exercise details page
- [ ] Confirm details, video link, and user usage stats display correctly

## Workout Logging

### Start Workout

- [ ] Open `/Workouts/Start`
- [ ] Confirm a new in-progress workout is created for the user
- [ ] Add an exercise successfully
- [ ] Add multiple exercises successfully
- [ ] Remove an exercise successfully

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

## UI / UX Checks

### Dark Mode

- [ ] Toggle dark mode from the main layout
- [ ] Refresh the page and confirm preference persists
- [ ] Check dashboard, exercises, workouts, analytics, and identity pages in dark mode

### Responsive Design

- [ ] Test mobile width layout
- [ ] Test tablet width layout
- [ ] Test desktop layout
- [ ] Confirm navigation works on small screens
- [ ] Confirm workout logging controls remain usable on touch-sized layouts

## Regression / Stability

- [ ] Restart the app and confirm startup succeeds
- [ ] Confirm database seed process does not create duplicate exercise issues
- [ ] Confirm no obvious Razor Page routing errors occur during common navigation
- [ ] Confirm no unexpected validation or server errors appear in core flows

## Issue Log

Record any defects discovered during testing.

| Area | Scenario | Result | Notes |
|---|---|---|---|
| Authentication |  |  |  |
| Workout Logging |  |  |  |
| History |  |  |  |
| Analytics |  |  |  |
| Responsive / Dark Mode |  |  |  |
