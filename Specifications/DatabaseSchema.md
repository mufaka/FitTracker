# FitTracker Database Schema

This document summarizes the current database structure for the FitTracker MVP.

## Overview

FitTracker uses Entity Framework Core with SQLite.

The application database contains:

- ASP.NET Core Identity tables for authentication and user management
- FitTracker domain tables for exercises, workouts, and workout sets

## Core Domain Tables

### `AspNetUsers` (`ApplicationUser`)

Extends `IdentityUser` with FitTracker-specific profile fields.

| Column | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `TEXT` | Yes | Primary key from Identity |
| `UserName` | `TEXT` | No | Identity login name |
| `NormalizedUserName` | `TEXT` | No | Indexed by Identity |
| `Email` | `TEXT` | No | Used for login and confirmation |
| `NormalizedEmail` | `TEXT` | No | Indexed by Identity |
| `EmailConfirmed` | `INTEGER` | Yes | Email confirmation state |
| `PasswordHash` | `TEXT` | No | Identity-managed |
| `PhoneNumber` | `TEXT` | No | Optional profile/contact field |
| `PhoneNumberConfirmed` | `INTEGER` | Yes | Identity-managed |
| `PreferredUnits` | `TEXT` | No | Defaults to `lbs` |
| `DefaultRestTimer` | `INTEGER` | Yes | Defaults to `90` seconds |
| `Goals` | `TEXT` | No | Used for first-time setup completion |
| `DarkMode` | `INTEGER` | Yes | Stored user preference |

### `Exercises`

Stores the exercise library and any custom user exercises.

| Column | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `INTEGER` | Yes | Primary key |
| `Name` | `TEXT` | Yes | Indexed |
| `Category` | `TEXT` | Yes | Indexed |
| `MuscleGroups` | `TEXT` | Yes | Comma-separated descriptive text |
| `Equipment` | `TEXT` | Yes | Equipment used |
| `Description` | `TEXT` | No | Optional details |
| `VideoUrl` | `TEXT` | No | Optional tutorial/demo URL |
| `IsCustom` | `INTEGER` | Yes | Indicates user-defined exercise |
| `UserId` | `TEXT` | No | Optional owning user for custom exercises |

### `Workouts`

Stores workout sessions for a specific user.

| Column | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `INTEGER` | Yes | Primary key |
| `UserId` | `TEXT` | Yes | Foreign key to `AspNetUsers.Id` |
| `Date` | `TEXT` | Yes | Workout date/time |
| `Duration` | `INTEGER` | Yes | Stored in minutes |
| `Notes` | `TEXT` | No | Workout-level notes |
| `CreatedAt` | `TEXT` | Yes | UTC creation timestamp |
| `IsCompleted` | `INTEGER` | Yes | Distinguishes drafts/in-progress vs completed workouts |

Indexes:

- `IX_Workouts_Date`
- `IX_Workouts_UserId`

### `WorkoutExercises`

Join table between workouts and exercises, preserving order within a workout.

| Column | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `INTEGER` | Yes | Primary key |
| `WorkoutId` | `INTEGER` | Yes | Foreign key to `Workouts.Id` |
| `ExerciseId` | `INTEGER` | Yes | Foreign key to `Exercises.Id` |
| `Order` | `INTEGER` | Yes | Sequence within the workout |
| `Notes` | `TEXT` | No | Exercise-specific notes |

### `Sets`

Stores individual logged sets for each workout exercise.

| Column | Type | Required | Notes |
|---|---|---:|---|
| `Id` | `INTEGER` | Yes | Primary key |
| `WorkoutExerciseId` | `INTEGER` | Yes | Foreign key to `WorkoutExercises.Id` |
| `SetNumber` | `INTEGER` | Yes | Order within the exercise |
| `Reps` | `INTEGER` | No | Optional rep count |
| `Weight` | `TEXT` / decimal | No | Configured with precision `(10,2)` |
| `Duration` | `INTEGER` | No | Optional timed set duration |
| `RestTime` | `INTEGER` | No | Optional rest time in seconds |
| `RPE` | `INTEGER` | No | Rate of perceived exertion |
| `CreatedAt` | `TEXT` | Yes | UTC creation timestamp |

## Entity Relationships

### User to Workouts

- One `ApplicationUser` has many `Workouts`
- `Workout.UserId` is required
- Delete behavior: `Cascade`

### Workout to WorkoutExercises

- One `Workout` has many `WorkoutExercises`
- `WorkoutExercise.WorkoutId` is required
- Delete behavior: `Cascade`

### Exercise to WorkoutExercises

- One `Exercise` can appear in many `WorkoutExercises`
- `WorkoutExercise.ExerciseId` is required
- Delete behavior: `Restrict`

### WorkoutExercise to Sets

- One `WorkoutExercise` has many `Sets`
- `Set.WorkoutExerciseId` is required
- Delete behavior: `Cascade`

## Identity Tables

In addition to the FitTracker tables above, ASP.NET Core Identity creates the standard authentication tables:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserRoles`
- `AspNetUserTokens`
- `AspNetRoleClaims`

## Notes

- FitTracker currently treats `Goals` as the signal that first-time profile setup has been completed.
- Exercise search and workout history rely on the indexed `Exercises.Name`, `Exercises.Category`, `Workouts.Date`, and `Workouts.UserId` columns.
- The schema is currently aligned to the MVP scope in `Specifications\Implementation.md`.
