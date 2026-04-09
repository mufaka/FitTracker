# FitTracker

FitTracker is a comprehensive fitness tracking application that helps users plan, track, and analyze their workouts with detailed daily, weekly, and monthly summaries.

## Tech Stack

- **.NET 10** - ASP.NET Core with Razor Pages
- **SQLite** - Database with Entity Framework Core
- **Microsoft Identity** - Authentication and user management
- **Tailwind CSS 4** - Styling framework
- **Alpine.js** - Client-side interactivity
- **HTMX** - Dynamic partial page updates
- **Dark Mode** - Full support with user preference persistence

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

The database will be created automatically on first run, but you can manually create it:

```bash
dotnet ef database update
```

### 6. Run the Application

```bash
dotnet run
```

Or press F5 in Visual Studio.

The application will be available at:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001

## Project Structure

```
FitTracker/
├── Data/                  # Database context and migrations
│   └── ApplicationDbContext.cs
├── Models/                # Entity models
│   ├── ApplicationUser.cs
│   ├── Exercise.cs
│   ├── Workout.cs
│   ├── WorkoutExercise.cs
│   └── Set.cs
├── Pages/                 # Razor Pages
│   ├── Shared/           # Shared layouts and partials
│   ├── Index.cshtml      # Dashboard
│   └── Error.cshtml
├── Areas/                 # Identity pages (auto-generated)
│   └── Identity/
├── wwwroot/              # Static files
│   ├── css/
│   └── js/
├── Specifications/        # Project documentation
│   ├── Idea.md           # Project concept and features
│   └── Implementation.md # Implementation checklist
├── appsettings.json      # Configuration
├── Program.cs            # Application entry point
├── tailwind.config.js    # Tailwind CSS configuration
└── package.json          # Node.js dependencies
```

## Database Schema

### Core Entities

- **ApplicationUser** - Extends IdentityUser with fitness preferences
- **Exercise** - Exercise library (name, category, muscle groups, equipment)
- **Workout** - Workout sessions (date, duration, notes)
- **WorkoutExercise** - Exercises within a specific workout
- **Set** - Individual sets (reps, weight, duration, RPE)

## Features

### Phase 1: MVP (Current)
- ✅ User authentication and registration
- ✅ Database setup with EF Core
- ✅ Dark mode support
- ✅ Responsive design foundation
- ⏳ Exercise library
- ⏳ Workout logging
- ⏳ Daily summaries

### Phase 2: Enhanced Tracking (Planned)
- Workout templates
- Calendar view
- Weekly/monthly summaries
- Personal records tracking
- Progress charts

### Phase 3: Advanced Features (Planned)
- Body measurements
- Progress photos
- 1RM estimates
- Advanced analytics
- Data export

### Phase 4: Polish (Planned)
- Achievements and badges
- Challenges
- PWA support
- Performance optimizations

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

### Tailwind CSS

The CSS source is in `wwwroot/css/site.css` and outputs to `wwwroot/css/output.css`.

Run the watcher during development:
```bash
npm run watch:css
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

### User Settings

Default user preferences are set in `ApplicationUser.cs`:
- Preferred units: lbs (can be changed to kg)
- Default rest timer: 90 seconds
- Dark mode: false (user can toggle)

## Troubleshooting

### Database Issues

If you encounter database issues, delete `FitTracker.db` and run:
```bash
dotnet ef database update
```

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
