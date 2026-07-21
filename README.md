# FitTracker

FitTracker is a comprehensive fitness tracking application that helps users plan, track, and analyze their workouts with detailed daily, weekly, and monthly summaries.

## Tech Stack

- **.NET 10** - ASP.NET Core with Razor Pages
- **SQLite** - Database with Entity Framework Core
- **Microsoft Identity** - Authentication and user management
- **Tailwind CSS 4** - Styling framework, configured entirely in CSS
- **Alpine.js** - Client-side interactivity
- **HTMX** - Dynamic partial page updates
- **Chart.js** - Analytics and progress charts
- **QuestPDF / ImageSharp** - PDF export and progress photo processing
- **Dark Mode** - Full support with user preference persistence
- **Installable** - Web app manifest, so it can be added to a phone home screen

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) (for Tailwind CSS)
- Visual Studio 2022+ or VS Code

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd FitTracker
```

### 2. Restore .NET Dependencies

```bash
dotnet restore
```

### 3. Install Node Dependencies (for Tailwind CSS)

```bash
npm install
```

### 4. Build Tailwind CSS

For development with watch mode:
```bash
npm run watch:css
```

For production build:
```bash
npm run build:css
```

### 5. Set Up the Database

Nothing to do. The app applies any pending migrations and seeds the reference
data (exercise library, achievement definitions) at startup, so the database is
created on first run. Both steps are idempotent and run in every environment.

To apply migrations by hand instead — for example to inspect the schema before
starting the app:

```bash
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet run
```

Or press F5 in Visual Studio.

The application will be available at http://localhost:5184, as configured by the
`http` profile in `Properties/launchSettings.json`. There is no HTTPS profile;
TLS is expected to be terminated by a reverse proxy in production.

## Project Structure

```
FitTracker/
├── FitTracker.slnx        # Solution file for app and tests
├── Data/                  # Database context and seed data
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs   # Seeds the exercise library and achievements
├── Migrations/            # EF Core migrations
├── Models/                # Entity models (exercises, workouts, sets, PRs,
│                          # templates, measurements, photos, achievements,
│                          # challenges)
├── Services/              # Business logic, one service per feature area
│                          # (analytics, workouts, PRs, achievements,
│                          # challenges, export, photos, suggestions)
├── TagHelpers/            # Html5ValidationTagHelper — projects DataAnnotations
│                          # onto native HTML5 validation attributes
├── Pages/                 # Razor Pages
│   ├── Shared/            # Shared layouts, partials, and components
│   ├── Index.cshtml       # Dashboard
│   └── Error.cshtml
├── Areas/                 # Identity pages (scaffolded, then restyled)
│   └── Identity/
├── FitTracker.Tests/      # Service tests and test infrastructure
├── wwwroot/               # Static files
│   ├── css/               # site.css (Tailwind source) -> output.css (generated)
│   ├── js/                # site.js — client-side validation
│   ├── icons/             # PWA and home screen icons
│   ├── lib/               # Client libs served locally (alpinejs, htmx, chartjs)
│   └── manifest.json      # Web app manifest
├── Specifications/        # Project documentation
│   ├── Idea.md            # Project concept and features
│   ├── Implementation.md  # Implementation checklist
│   ├── Progress.md        # Session progress summary
│   ├── DatabaseSchema.md  # Database schema reference
│   ├── TailwindGuidelines.md # Tailwind 4 conventions for this project
│   ├── ManualTestChecklist.md # Manual MVP validation checklist
│   └── UserGuide.md       # End-user guide for the MVP
├── appsettings.json       # Configuration
├── Program.cs             # Application entry point
└── package.json           # Node.js dependencies
```

## Database Schema

### Core Entities

- **ApplicationUser** - Extends IdentityUser with fitness preferences
- **Exercise** - Exercise library (name, category, muscle groups, equipment)
- **Workout** - Workout sessions (date, duration, notes)
- **WorkoutExercise** - Exercises within a specific workout
- **Set** - Individual sets (reps, weight, duration, RPE)

### Tracking & Progress

- **PersonalRecord** - Detected PRs per exercise, with estimated 1RM
- **BodyMeasurement** - Weight and body measurements over time
- **ProgressPhoto** - Photo metadata; files live outside the database

### Planning

- **WorkoutTemplate** / **WorkoutTemplateExercise** - Reusable session plans

### Gamification

- **Achievement** / **UserAchievement** - Lifetime milestones and unlocks
- **Challenge** / **UserChallenge** - Goals measured over a window that starts
  when the user joins

Note that `Specifications/DatabaseSchema.md` currently documents only the Phase 1
entities and predates everything from Phase 2 onwards.

## Features

### Phase 1: MVP — complete
- ✅ User authentication and registration
- ✅ Password reset, email confirmation, and profile setup
- ✅ Database setup with EF Core
- ✅ Dark mode support
- ✅ Responsive design foundation
- ✅ Exercise library
- ✅ Workout logging
- ✅ Daily summaries

### Phase 2: Enhanced Tracking — complete
- ✅ Workout templates
- ✅ Calendar view
- ✅ Weekly/monthly summaries
- ✅ Personal records tracking
- ✅ Progress charts

### Phase 3: Advanced Features — complete
- ✅ Body measurements
- ✅ Progress photos
- ✅ 1RM estimates
- ✅ Advanced analytics
- ✅ Data export (CSV, JSON, PDF)
- ✅ Workout suggestions based on least-worked muscle groups

### Phase 4: Polish — in progress
- ✅ Achievements and badges
- ✅ Challenges — goals measured over a window that starts when you join
- ✅ Mobile touch targets, audited across six device profiles
- ✅ Installable web app manifest and icons
- ⬜ Service worker and offline support
- ⬜ Accessibility pass
- ⬜ Advanced 1RM tracking, AI-based suggestions

`Specifications/Implementation.md` is the authoritative checklist, including the
items deliberately left undone and why. Two caveats on the above: Phase 1's
features are all built, but its manual test pass is still outstanding, and the
one remaining Phase 2 item is drag-and-drop calendar planning, marked optional.

## Development

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add <MigrationName>
```

Apply migrations:
```bash
dotnet ef database update
```

Remove last migration:
```bash
dotnet ef migrations remove
```

A new migration is applied the next time the app starts — `Program.cs` calls
`Database.MigrateAsync()` during startup. If that fails the app refuses to start
rather than serving requests against a schema it cannot use.

### Tailwind CSS

Tailwind 4 is configured entirely in CSS — there is no `tailwind.config.js`.
The source is `wwwroot/css/site.css` (tracked) and it compiles to
`wwwroot/css/output.css` (generated, git-ignored). The build runs automatically
on `dotnet build` / `dotnet publish`.

Run the watcher during development:
```bash
npm run watch:css
```

`site.css` holds the theme tokens (`@theme`), the class-based dark mode variant,
and the `ui`-style component classes defined with `@utility`. When a look recurs
across pages, add a `@utility` there rather than repeating utility strings in
markup — see `Specifications/TailwindGuidelines.md`.

### Client libraries

Alpine.js, htmx, and Chart.js are served from `wwwroot/lib/` rather than a CDN.
Versions are pinned in `package.json`; the `CopyClientLibs` MSBuild target
refreshes `wwwroot/lib/` from `node_modules` on each build. To upgrade one, bump
`package.json`, run `npm install`, and rebuild.

### Form validation

Validation rules are declared once, as DataAnnotations on the page models.
`Html5ValidationTagHelper` projects them onto the native HTML5 attributes
(`required`, `minlength`, `min`/`max`, `pattern`), so the browser enforces the
same rules the server does without them being restated in markup.

`wwwroot/js/site.js` adds a small Alpine component for the two things the
browser cannot do alone: `[Compare]` (password confirmation) has no HTML5
equivalent and is fed into the same native validity pipeline via
`setCustomValidity()`, and messages are rendered into the existing
`asp-validation-for` spans instead of unstyleable browser bubbles. The message
text is read from the `data-val-*` attributes ASP.NET already emits, so client
and server report failures identically. There is no jQuery in the project.

### Installable web app

`wwwroot/manifest.json` and `wwwroot/icons/` make the app installable to a phone
home screen, running standalone without browser chrome. Icons are generated from
the same lightning bolt used as the in-app logo, in both `any` and `maskable`
purposes; iOS takes its icon from the separate `apple-touch-icon` link instead.

A manifest carries only one `theme_color`, and this app's dark mode is
class-based rather than driven by `prefers-color-scheme`, so the `theme-color`
meta tag is updated alongside the theme toggle in `_Layout.cshtml`.

There is no service worker, which is a deliberate choice — see the PWA Support
section of `Specifications/Implementation.md`. Installing requires HTTPS in
production; `localhost` is exempt for local testing.

### Running Tests

Run the service test project with:

```bash
dotnet test .\FitTracker.Tests\FitTracker.Tests.csproj -p:SkipTailwindBuild=true
```

### Code Standards

- Follow C# naming conventions
- Use nullable reference types
- Keep controllers/page models thin, use services for business logic
- Write meaningful commit messages
- Update implementation checklist as features are completed

## Configuration

### Connection String

SQLite database connection is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=FitTracker.db"
  }
}
```

### Storage Paths

Everything the app writes is configurable, so a deployment can keep data outside
the application directory:

```json
{
  "Storage": {
    "ProgressPhotosPath": "App_Data/ProgressPhotos"
  }
}
```

Absolute paths are used as given; relative paths resolve against the content
root. Either separator works, so the same file is valid on Windows and Linux.
Any setting can also be supplied as an environment variable —
`Storage__ProgressPhotosPath`, `ConnectionStrings__DefaultConnection`.

Note that the default connection string is relative to the process working
directory, not the content root.

### Running as a systemd service

`Program.cs` calls `builder.Services.AddSystemd()`, so the app notifies systemd
when it is ready, treats `SIGTERM` as a graceful shutdown, and writes log levels
in the format the journal expects. It is a no-op off systemd, so Windows and
local development are unaffected.

`Type=notify` is what makes that worthwhile — systemd waits for the app to
actually be serving before reporting the unit as started, instead of assuming it
is up the moment the process spawns.

```ini
[Unit]
Description=FitTracker
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
User=fittracker
WorkingDirectory=/opt/fittracker
ExecStart=/opt/fittracker/FitTracker
Restart=on-failure
RestartSec=5
TimeoutStopSec=30

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
# Behind a TLS-terminating proxy. Without this the app sees plain HTTP and
# UseHttpsRedirection bounces every request into a redirect loop.
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
# Keep writable data out of the deployment directory.
Environment=ConnectionStrings__DefaultConnection=Data Source=/var/lib/fittracker/FitTracker.db
Environment=Storage__ProgressPhotosPath=/var/lib/fittracker/ProgressPhotos

[Install]
WantedBy=multi-user.target
```

`WorkingDirectory` matters: without it systemd starts the process in `/`, and a
relative connection string would try to create the database at the filesystem
root. Setting both paths above sidesteps that entirely.

### User Settings

Default user preferences are set in `ApplicationUser.cs`:
- Preferred units: lbs (can be changed to kg)
- Default rest timer: 90 seconds
- Dark mode: false (user can toggle)

## Troubleshooting

### Database Issues

Delete `FitTracker.db` and start the app. Migrations and seeding both run at
startup, so the database is rebuilt from scratch — there is no separate command
to run. Note this discards all logged data.

### Tailwind CSS Not Working

Ensure you've run:
```bash
npm install
npm run build:css
```

And that `wwwroot/css/output.css` exists.

### Build Errors

Clean and rebuild:
```bash
dotnet clean
dotnet build
```

## Contributing

This is a personal project, but suggestions and feedback are welcome!

## License

This project is for personal use and learning purposes.

## Roadmap

See `Specifications/Implementation.md` for detailed implementation progress and upcoming features.

See `Specifications/Progress.md` for the current MVP delivery status.

See `Specifications/ManualTestChecklist.md` for the current MVP manual validation checklist.

See `Specifications/UserGuide.md` for an end-user walkthrough of the current MVP.
