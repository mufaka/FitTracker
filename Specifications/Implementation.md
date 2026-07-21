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
  - [x] Id, WorkoutExerciseId, SetNumber, Reps, Weight, Duration, RestTime, RPE
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

- [x] Create `WorkoutTemplate` model
  - [x] Id, UserId, Name, Description, IsActive
- [x] Create `WorkoutTemplateExercise` model
  - [x] Id, TemplateId, ExerciseId, Order, DefaultSets, DefaultReps, Notes
- [x] Add database migration
- [x] Create Templates page (`/Templates/Index`)
  - [x] List all user templates
  - [x] Create new template
  - [x] Edit template
  - [x] Delete template
- [x] Create Template Builder page (`/Templates/Create`)
  - [x] Add exercises to template
  - [x] Set default sets/reps
  - [x] Reorder exercises
- [x] Add "Start from Template" option to dashboard
- [x] Style with Tailwind CSS

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
  - [x] Suggest workouts based on templates and history
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

- [ ] Create 1RM history tracking
- [ ] Display 1RM progression charts
- [ ] Add 1RM predictions based on trends
- [ ] Create 1RM leaderboard (personal, by exercise)
- [ ] Style with Tailwind CSS

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
