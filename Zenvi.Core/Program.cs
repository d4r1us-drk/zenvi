using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Zenvi.Core.Data.Context;
using Zenvi.Core.Data.Entities;
using Zenvi.Core.Identity;
using Zenvi.Core.Services;
using Zenvi.Core.Utils;

namespace Zenvi.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var logHandler = new LogHandler<Program>();

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard authorization header using the Bearer scheme (\"Bearer {token}\")",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey
            });
            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });

        string connectionString;
        if (builder.Environment.IsDevelopment())
        {
            connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            logHandler.LogInfo($"API IS RUNNING ON DEVELOPMENT MODE. Using connection string from appsettings.Development.json: {connectionString}");
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
            logHandler.LogInfo($"API IS RUNNING IN PRODUCTION. Constructed connection string using environment variables: {connectionString}");
        }

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.Services.AddIdentityApiEndpoints<User>(options =>
                options.SignIn.RequireConfirmedEmail = true)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<ClaimsPrincipal>();

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
                EnableSsl = true
            });

        builder.Services.AddTransient<IEmailSender<User>, IdentityEmailSender>();

        builder.Services.AddScoped<DataHandlerService>();
        builder.Services.AddScoped<IMediaUploadService, MediaUploadService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapZenviIdentityApi();

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}