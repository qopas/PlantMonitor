using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PlantMonitor.Application.Common.Interfaces;
using PlantMonitor.Domain.Entities;
using PlantMonitor.Infrastructure.Authentication;
using PlantMonitor.Infrastructure.Data;
using PlantMonitor.Infrastructure.Services;
using Serilog;

namespace PlantMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                            ?? configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(configuration.GetValue<int>("Database:CommandTimeout", 30));
                npgsqlOptions.EnableRetryOnFailure(configuration.GetValue<int>("Database:MaxRetryCount", 3));
            }));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<User, IdentityRole<long>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                  ?? configuration["JwtSettings:SecretKey"] 
                  ?? configuration["Jwt:Key"];

        var jwtIssuer = Environment.GetEnvironmentVariable("API_BASE_URL")
                     ?? configuration["JwtSettings:Issuer"] 
                     ?? configuration["Jwt:Issuer"];

        var jwtAudience = Environment.GetEnvironmentVariable("API_BASE_URL")
                       ?? configuration["JwtSettings:Audience"] 
                       ?? configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT SecretKey not configured");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
        }
        else
        {
            services.AddMemoryCache();
        }
        
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDeviceTokenService, DeviceTokenService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<JwtTokenService>();

        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await context.Database.MigrateAsync();
        
        await SeedDataAsync(scope.ServiceProvider);
    }

    private static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long> { Name = role });
            }
        }

        var adminEmail = "admin@plantmonitor.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}