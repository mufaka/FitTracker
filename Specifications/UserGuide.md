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

## Starting a Workout

Open `Workouts` or select `Start workout` from the dashboard.

### Workout Flow

1. Start or continue the current workout.
2. Add one or more exercises.
3. Log sets for each exercise.
4. Optionally add workout notes.
5. Complete the workout when finished.

### Logged Set Data

Each set can include:

- weight
- reps
- RPE

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
- search by notes or exercise name
- filter by date range
- page through larger result sets
- open full workout details
- delete a workout
- repeat a workout structure

### Workout Details

The details page includes:

- all logged exercises
- all sets
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

## MVP Scope Notes

This guide covers the current Phase 1 MVP functionality.

Planned future features such as templates, calendar planning, personal records, and advanced analytics are tracked in `Specifications\Implementation.md`.
