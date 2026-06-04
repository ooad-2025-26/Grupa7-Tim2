using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;
using TimeForPill.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IDoseWorkflowService, DoseWorkflowService>();
builder.Services.AddHostedService<DoseReminderWorker>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        context.Database.Migrate();

        if (!context.Database.CanConnect())
        {
            throw new InvalidOperationException(
                "Aplikacija ne moze uspostaviti vezu sa bazom iz DefaultConnection.");
        }

        BackfillMissingDoses(context);

        var conn = context.Database.GetDbConnection();

        logger.LogInformation(
            "Database connection OK. DataSource={DataSource}, Database={Database}",
            conn.DataSource,
            conn.Database);

        Console.WriteLine(
            $"Database connection OK. DataSource={conn.DataSource}, Database={conn.Database}");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "An error occurred while migrating or initializing the database.");

        Console.WriteLine(
            "An error occurred while migrating or initializing the database. " +
            "See logs for details: " + ex.Message);

        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapRazorPages();

app.Run();

static void BackfillMissingDoses(ApplicationDbContext context)
{
    var terapijeBezDoza = context.Terapije
        .Where(t => !context.TerapijskeDoze.Any(d => d.TerapijaId == t.Id))
        .ToList();

    foreach (var terapija in terapijeBezDoza)
    {
        var totalDoses = terapija.UkupanBrojDoza <= 0
            ? 1
            : terapija.UkupanBrojDoza;
        var intervalHours = terapija.IntervalSati <= 0
            ? 24
            : terapija.IntervalSati;
        var start = terapija.Pocetak == default
            ? DateTime.Now
            : terapija.Pocetak;

        for (var index = 0; index < totalDoses; index++)
        {
            var scheduledAt = start.AddHours(intervalHours * index);
            context.TerapijskeDoze.Add(new TerapijskaDoza
            {
                TerapijaId = terapija.Id,
                RedniBroj = index + 1,
                VrijemeUzimanja = scheduledAt,
                OriginalnoVrijemeUzimanja = scheduledAt,
                VrijemePodsjetnika = scheduledAt.AddMinutes(-5),
                Status = StatusDoze.Cekanje
            });
        }

        terapija.UkupanBrojDoza = totalDoses;
        terapija.IntervalSati = intervalHours;
        terapija.Kraj = start.AddHours(intervalHours * Math.Max(0, totalDoses - 1));
    }

    if (terapijeBezDoza.Count > 0)
    {
        context.SaveChanges();
    }
}
