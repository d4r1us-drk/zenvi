using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zenvi.Data;
using Zenvi.Hub;
using Zenvi.Models;
using Zenvi.Utils;
using Zenvi.Services;

var builder = WebApplication.CreateBuilder(args);
var logHandler = new LogHandler(typeof(Program));

// Add services to the container.
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<IFollowService, FollowService>();

string connectionString;
if (builder.Environment.IsDevelopment())
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    logHandler.LogInfo($"APP IS RUNNING ON DEVELOPMENT MODE. Using connection string from appsettings.Development.json: {connectionString}");
}
else
{
    // Construct connection string with container's environment variables
    var dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST");
    var dbPort = Environment.GetEnvironmentVariable("DATABASE_PORT");
    var dbName = Environment.GetEnvironmentVariable("DATABASE_NAME");
    var dbUser = Environment.GetEnvironmentVariable("DATABASE_USER");
    var dbPassword = Environment.GetEnvironmentVariable("DATABASE_PSWD");
    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    logHandler.LogInfo($"APP IS RUNNING IN PRODUCTION. Constructed connection string using environment variables: {connectionString}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

string smtpUsername = "", smtpPassword = "";
if (builder.Environment.IsDevelopment())
{
    smtpUsername = builder.Configuration["Smtp:Username"] ??
                   throw new InvalidOperationException("SMTP Username not set in dotnet secret store.");
    smtpPassword = builder.Configuration["Smtp:Password"] ??
                   throw new InvalidOperationException("SMTP Password not set in dotnet secret store.");
}

builder.Services.AddFluentEmail(smtpUsername)
    .AddSmtpSender(new SmtpClient("smtp.gmail.com")
    {
        Port = 587,
        Credentials = new NetworkCredential(smtpUsername, smtpPassword),
        EnableSsl = true
    });

builder.Services.AddTransient<IEmailSender<User>, IdentityEmailSender>();

builder.Services.AddAuthentication().AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

if (builder.Environment.IsProduction())
{
    if (builder.Configuration.GetValue<bool>("MigrationsApplied") == false)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            logHandler.LogInfo("Applying initial database migrations...");
            context.Database.Migrate();

            // Update the appsettings.json to set MigrationsApplied to true
            var configFilePath = Path.Combine("/config", "appsettings.json");
            var json = File.ReadAllText(configFilePath);
            dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            jsonObj["MigrationsApplied"] = true;
            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(configFilePath, output);
        }
        catch (Exception ex)
        {
            logHandler.LogFatal("Something terrible happened while trying to apply database migrations.", ex);
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
app.MapHub<PostHub>("/postHub");
app.MapRazorPages();

app.Run();
