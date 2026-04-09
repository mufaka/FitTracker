# FitTracker Idea

## Overview
FitTracker is a comprehensive fitness tracking application that helps users plan, track, and analyze their workouts with detailed daily, weekly, and monthly summaries.

## Tech Stack
- **Framework**: .NET 10 ASP.NET Core with Razor Pages
- **Database**: SQLite with Entity Framework Core
- **Authentication**: Microsoft Identity
- **Frontend**:
  - Tailwind CSS 4 for styling
  - Alpine.js for client-side interactivity
  - HTMX for dynamic partial page updates
  - Fully responsive design
  - Dark mode support

## Core Features

### 1. Workout Planning
- **Pre-plan workouts**: Create workout templates for future sessions
- **Exercise library**: Comprehensive database of exercises with categories (strength, cardio, flexibility, etc.)
- **Workout templates**: Save frequently used workout routines for personal use
- **Calendar view**: Visual planning interface for scheduling workouts
- **Notes**:
  - Recurring workout schedules: Not in current scope
  - Sharing templates: Not needed for this version (private, personal use only)

### 2. Workout Tracking
- **Quick log mode**: Fast, streamlined logging interface (primary mode)
- **Real-time logging**: Track exercises as you perform them
- **Set tracking**: Log sets, reps, weight, duration, and rest time
- **Progressive overload**: Automatically suggest weight/rep increases based on previous performance
- **Exercise notes**: Optional detailed notes for each exercise (form cues, how it felt, etc.)
- **Video links**: Option to link to external form/tutorial videos for exercises
- **Timer integration**: Built-in rest timer between sets
- **Notes**:
  - Video recording: Not supported (linking to external videos is sufficient)
  - GPS tracking: Not in scope for this application

### 3. Analytics & Summaries

#### Daily Summary
- Exercises completed
- Total volume (sets × reps × weight)
- Total workout duration
- Calories burned estimate
- Personal records (PRs) achieved

#### Weekly Summary
- Total workouts completed
- Workout frequency and consistency
- Volume comparison week-over-week
- Muscle group distribution
- Rest days taken
- Week-over-week progress charts

#### Monthly Summary
- Monthly workout count
- Total volume and progression trends
- Body measurements progress (if tracked)
- PRs achieved during the month
- Most/least worked muscle groups
- Adherence to planned workouts percentage

### 4. Progress Tracking
- **Personal Records**: Track PRs for each exercise
- **1RM Estimates**: Calculate and display one-rep max estimates based on logged sets
- **Body measurements**: Weight, body fat %, measurements (chest, waist, arms, etc.)
- **Progress photos**: Before/after photos with date stamps
- **Strength progression charts**: Visual graphs of progress over time
- **Notes**:
  - Smart scale/fitness tracker integration: Not in scope (manual entry only)

### 5. User Management
- Microsoft Identity for authentication
- User profiles with goals and preferences
- Customizable settings (units, rest timer defaults, etc.)
- Data export functionality
- **Notes**:
  - Single user profile per account only (no trainer/client roles)
  - No social features (private, personal tracking only)

## Database Schema Ideas

### Core Entities
- **User**: Identity user with profile settings
- **Exercise**: Exercise library (name, category, muscle groups, equipment needed)
- **WorkoutTemplate**: Saved workout plans
- **Workout**: Actual workout sessions (date, duration, notes)
- **WorkoutExercise**: Exercises within a workout
- **Set**: Individual sets (reps, weight, duration, RPE)
- **BodyMeasurement**: Weight and body measurements over time
- **PersonalRecord**: PRs for each exercise

## UI/UX Considerations

### Pages/Views
1. **Dashboard**: Quick overview with today's workout, recent stats, upcoming workouts
2. **Calendar**: Monthly/weekly view of planned and completed workouts
3. **Start Workout**: Active workout tracking interface
4. **Exercise Library**: Browse and search all exercises
5. **Analytics**: Detailed charts and statistics
6. **Profile**: User settings, goals, measurements
7. **History**: Past workout log with filtering

### Responsive Design
- Mobile-first approach (many users will track at the gym)
- Quick actions accessible with one thumb
- Large tap targets for exercise logging
- Swipe gestures for quick navigation (Alpine.js)

### Dark Mode
- Toggle in user preferences
- Persist preference per user
- System preference detection
- All charts and visuals optimized for both modes

### HTMX Integration
- Partial page updates for workout logging (no full page refreshes)
- Live search in exercise library
- Inline editing of workout details
- Dynamic chart loading in analytics

## Open Questions & Ideas

### Features to Consider (Future Phases)
1. **Workout AI Assistant**: Suggest next exercise based on muscle groups worked?
2. **Rest Day Recommendations**: Alert when user might be overtraining?
3. **Nutrition Tracking**: Integrate meal logging? (Might be scope creep)
4. **Mobile App**: PWA support or native mobile app later?

### Out of Scope (Confirmed)
- Community features (sharing, social, competing with friends)
- External integrations (Strava, Apple Health, Google Fit, smart scales)
- Multiple user profiles or trainer/client roles
- Video recording capability
- GPS tracking for outdoor activities

### Technical Decisions
1. **Offline capability**: Not needed - web app requires internet connection
2. **Multi-device sync**: Standard web app behavior (handled by server-side state)
3. **Backup and restore**: Managed by system administrator outside of application
4. **Data retention policy**: Deferred for now
5. **Performance optimization**: Built for long-term data storage; optimize as needed based on actual usage patterns

### Gamification Ideas
1. **Achievements/Badges**: Workout streaks, volume milestones, PR achievements
2. **Challenges**: 30-day challenges, progressive overload challenges

**Deferred/Not Planned:**
- Leaderboards: Deferred until social features are added (out of scope)
- Level system: Not wanted for this application

### Accessibility
- Keyboard navigation support
- Screen reader friendly
- High contrast mode
- Adjustable font sizes

## Success Metrics
- User retention (daily/weekly active users)
- Workout completion rate (planned vs. actual)
- Average workout duration
- User-reported goal achievement

## Development Phases

### Phase 1: MVP (Minimum Viable Product)
- User authentication and profiles
- Basic exercise library (50-100 exercises)
- Simple workout logging (sets, reps, weight)
- Daily summary view
- Responsive design with light/dark mode

### Phase 2: Enhanced Tracking
- Workout planning and templates
- Calendar view
- Weekly and monthly summaries
- Progress charts
- Personal records tracking

### Phase 3: Advanced Features
- Body measurement tracking
- Progress photos
- Advanced analytics
- Workout suggestions
- Export functionality

### Phase 4: Enhancements & Polish
- Achievements and gamification
- Advanced 1RM tracking and predictions
- Mobile optimizations and PWA support
- Performance improvements
- Advanced analytics and insights
- AI-based workout suggestions

## Notes
- **Performance**: Given SQLite backend, ensure efficient queries with proper indexing
- **Security**: Ensure workout data is private by default, implement proper authorization
- **Data Validation**: Prevent unrealistic entries (e.g., 10,000 lb bench press)
- **User Experience**: Make logging fast - users shouldn't spend more time logging than working out!

