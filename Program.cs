using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=FitTracker.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Register application services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IPersonalRecordService, PersonalRecordService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IProgressPhotoService, ProgressPhotoService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IWorkoutSuggestionService, WorkoutSuggestionService>();
builder.Services.AddScoped<IAnalyticsPdfExportService, AnalyticsPdfExportService>();
builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Bring the database up to date, then make sure the reference data the app needs
// to function (exercise library, achievement definitions) is present. Both steps
// are idempotent, and both run in every environment: this ships as a single
// instance against a SQLite file, so there is no deploy pipeline to apply
// migrations separately. Previously neither ran outside Development, so a fresh
// clone started successfully and then failed on every request with
// "no such table".
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        // Refuse to start rather than serve a broken schema: a startup failure
        // names the problem once, whereas the alternative is every request
        // failing later for a reason that looks unrelated.
        logger.LogCritical(ex, "Database initialization failed; the application will not start.");
        throw;
    }
}

app.Run();
