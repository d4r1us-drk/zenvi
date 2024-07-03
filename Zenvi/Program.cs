using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zenvi.Components;
using Zenvi.Components.Account;
using Zenvi.Data;
using Zenvi.Data.Models;
using Zenvi.Services;
using Zenvi.Utils;

var builder = WebApplication.CreateBuilder(args);
var logHandler = new LogHandler<Program>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton<DataHandlerService>();
builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

//// CONFIGURE DATABASE CONNECTION
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
////---

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

//// CONFIGURE IDENTITY EMAIL SENDER WITH FLUENTEMAIL
// TODO Implement logic when app is running in production
string smtpUsername = "", smtpPassword = "";
if (builder.Environment.IsDevelopment())
{
    smtpUsername = builder.Configuration["Smtp:Username"] ?? throw new InvalidOperationException("SMTP Username not set in dotnet secret store.");
    smtpPassword = builder.Configuration["Smtp:Password"] ?? throw new InvalidOperationException("SMTP Password not set in dotnet secret store.");
}

builder.Services.AddFluentEmail(smtpUsername)
    .AddSmtpSender(new SmtpClient("smtp.gmail.com")
    {
        Port = 587,
        Credentials = new NetworkCredential(smtpUsername, smtpPassword),
        EnableSsl = true,
    });

builder.Services.AddTransient<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
// ---

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Zenvi.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();