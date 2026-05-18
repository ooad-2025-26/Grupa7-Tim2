using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeForPill.Data;
using TimeForPill.Models;

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

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();