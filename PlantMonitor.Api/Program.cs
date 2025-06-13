using Microsoft.OpenApi.Models;
using PlantMonitor.Api.Middleware;
using PlantMonitor.Application;
using PlantMonitor.Infrastructure;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger - enable in development or when explicitly enabled
var enableSwagger = builder.Configuration.GetValue<bool>("ApiSettings:EnableSwagger", 
    builder.Environment.IsDevelopment());

if (enableSwagger)
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Plant Monitor API",
            Version = "v1",
            Description = "API for Plant Monitoring System with ESP32 integration",
            Contact = new OpenApiContact
            {
                Name = "Plant Monitor Team",
                Email = "support@plantmonitor.com"
            }
        });

        // Include XML comments if available
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

        // Add JWT Bearer authentication
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        // Add API Key authentication for devices
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key for device authentication. Use X-API-Key header.",
            Name = "X-API-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });
    });
}

// Configure CORS - use configuration from appsettings
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNativeApp", policy =>
    {
        if (allowedOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Configure Railway port binding - IMPORTANT for deployment
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

// Initialize database with proper error handling for Railway
try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.InitializeDatabaseAsync();
    Log.Information("Database initialization completed successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while initializing the database");
    // In production, we might want to continue running and let health checks fail
    if (app.Environment.IsDevelopment())
    {
        throw;
    }
    Log.Warning("Continuing startup despite database initialization failure");
}

// Configure the HTTP request pipeline
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Plant Monitor API V1");
        // Don't set as root in production
        c.RoutePrefix = app.Environment.IsDevelopment() ? string.Empty : "swagger";
    });
}

// Security headers for production
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Use HTTPS redirection only in production (Railway handles SSL termination)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowReactNativeApp");

// Add custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Railway
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    environment = app.Environment.EnvironmentName
});

// Main status endpoint
app.MapGet("/", () => new
{
    service = "Plant Monitor API",
    version = "1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    endpoints = new
    {
        health = "/health",
        version = "/version",
        api = "/api/v1",
        swagger = enableSwagger ? "/swagger" : null
    }
});

// Version endpoint with Railway-specific info
app.MapGet("/version", () => new
{
    version = "1.0.0",
    buildDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
    commit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? 
             Environment.GetEnvironmentVariable("RAILWAY_GIT_COMMIT_SHA")?.Substring(0, 7) ?? "local",
    environment = app.Environment.EnvironmentName,
    port = port,
    railway = new
    {
        deploymentId = Environment.GetEnvironmentVariable("RAILWAY_DEPLOYMENT_ID"),
        serviceId = Environment.GetEnvironmentVariable("RAILWAY_SERVICE_ID"),
        projectId = Environment.GetEnvironmentVariable("RAILWAY_PROJECT_ID")
    }
});

try
{
    Log.Information("Starting Plant Monitor API on {Environment} at port {Port}", 
        app.Environment.EnvironmentName, port);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}