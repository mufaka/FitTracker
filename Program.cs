using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using FitTracker.Data;
using FitTracker.Models;
using FitTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Run correctly under `systemctl start/stop`: signals readiness with sd_notify so
// a Type=notify unit does not report started until the app is actually serving,
// handles SIGTERM as a graceful shutdown, and formats logs for the journal. This
// is a no-op when the process is not running under systemd, so it stays safe on
// Windows and during local development.
builder.Services.AddSystemd();

// Behind a TLS-terminating reverse proxy the request arrives over plain HTTP,
// so Request.Scheme is "http" and every absolute URL the app generates is
// downgraded with it. The visible symptom is the cookie auth challenge: an
// unauthenticated visitor to https://host/ is sent to http://host/Identity/
// Account/Login, because the challenge builds that URL from the scheme it sees.
// Honouring X-Forwarded-Proto fixes the scheme for the whole pipeline, which
// also lets UseHsts and the confirmation-link generators do the right thing.
//
// Those headers come from the client, so they are only trusted from proxies
// named in configuration. With none configured the middleware is left out of
// the pipeline entirely rather than registered with empty lists: empty
// KnownProxies *and* KnownNetworks means "check nobody", which accepts a
// forwarded scheme from any caller that can reach the port.
IPAddress[] knownProxies = (builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    .Select(value => IPAddress.TryParse(value.Trim(), out var address)
        ? address
        // Silently skipping a typo would put the app back to emitting http://
        // URLs with nothing to show for it, so refuse to start instead.
        : throw new InvalidOperationException(
            $"ForwardedHeaders:KnownProxies contains \"{value}\", which is not an IP address."))
    .ToArray();

if (knownProxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;

        // The defaults trust loopback only, which is never right here: the
        // proxy is a different machine.
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        foreach (var proxy in knownProxies)
        {
            options.KnownProxies.Add(proxy);
        }
    });
}

// Writable locations are configuration, not constants, so a Linux deployment can
// point them outside the application directory while Windows keeps the defaults.
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

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
builder.Services.AddScoped<IWorkoutPlanService, WorkoutPlanService>();
builder.Services.AddScoped<IPersonalRecordService, PersonalRecordService>();
builder.Services.AddScoped<IOneRepMaxService, OneRepMaxService>();
builder.Services.AddScoped<IAchievementService, AchievementService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IProgressPhotoService, ProgressPhotoService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IWorkoutSuggestionService, WorkoutSuggestionService>();
builder.Services.AddScoped<IAnalyticsPdfExportService, AnalyticsPdfExportService>();
builder.Services.AddTransient<IEmailSender, LoggingEmailSender>();

builder.Services.AddRazorPages();

var app = builder.Build();

// First in the pipeline: HSTS, the HTTPS redirect and the authentication
// challenge all read Request.Scheme, so the correction has to land before any
// of them.
if (knownProxies.Length > 0)
{
    app.UseForwardedHeaders();
    app.Logger.LogInformation(
        "Trusting X-Forwarded-Proto and X-Forwarded-For from {KnownProxies}.",
        string.Join(", ", knownProxies.Select(p => p.ToString())));
}

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
