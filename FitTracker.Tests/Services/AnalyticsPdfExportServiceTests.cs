using System.Text;
using FitTracker.Models;
using FitTracker.Services;
using Xunit;

namespace FitTracker.Tests.Services;

public class AnalyticsPdfExportServiceTests
{
    [Fact]
    public void ExportDashboardPdf_GeneratesPdfDocument()
    {
        var service = new AnalyticsPdfExportService();
        var summary = new AdvancedAnalyticsSummary
        {
            RangeStart = new DateTime(2026, 1, 1),
            RangeEnd = new DateTime(2026, 3, 31),
            TotalWorkouts = 18,
            TotalVolume = 24500m,
            AverageWorkoutDuration = 52.4m,
            AverageVolumePerWorkout = 1361.1m,
            TotalPersonalRecords = 4,
            MostWorkedMuscleGroups =
            {
                new AdvancedMuscleGroupItem { MuscleGroup = "Chest", Volume = 8200m, WorkoutCount = 8, AverageDuration = 50m },
                new AdvancedMuscleGroupItem { MuscleGroup = "Legs", Volume = 7600m, WorkoutCount = 6, AverageDuration = 58m }
            },
            LeastWorkedMuscleGroups =
            {
                new AdvancedMuscleGroupItem { MuscleGroup = "Calves", Volume = 900m, WorkoutCount = 2, AverageDuration = 35m }
            },
            VolumeTrend =
            {
                new AdvancedVolumeTrendPoint { Label = "Jan 05", Volume = 3200m, WorkoutCount = 2, AverageDuration = 45m },
                new AdvancedVolumeTrendPoint { Label = "Jan 12", Volume = 4100m, WorkoutCount = 3, AverageDuration = 54m }
            },
            PersonalRecordTimeline =
            {
                new PersonalRecordTimelinePoint { Label = "Jan", RecordCount = 1, BestOneRepMax = 215m },
                new PersonalRecordTimelinePoint { Label = "Feb", RecordCount = 2, BestOneRepMax = 225m }
            },
            RecentPersonalRecords =
            {
                new PersonalRecord
                {
                    Date = new DateTime(2026, 3, 14),
                    Exercise = new Exercise { Name = "Bench Press", Category = "Strength", Equipment = "Barbell", MuscleGroups = "Chest" },
                    Weight = 225m,
                    Reps = 3,
                    OneRepMax = 247.5m
                }
            }
        };

        var pdf = service.ExportDashboardPdf(summary);

        Assert.NotNull(pdf);
        Assert.True(pdf.Length > 500);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.Take(4).ToArray()));
    }
}
