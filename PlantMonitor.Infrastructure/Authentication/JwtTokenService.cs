using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlantMonitor.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PlantMonitor.Infrastructure.Authentication;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;

    public JwtTokenService(IConfiguration configuration, UserManager<User> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<string> GenerateTokenAsync(User user)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                  ?? _configuration["JwtSettings:SecretKey"] 
                  ?? _configuration["Jwt:Key"];

        var jwtIssuer = Environment.GetEnvironmentVariable("API_BASE_URL")
                     ?? _configuration["JwtSettings:Issuer"] 
                     ?? _configuration["Jwt:Issuer"];

        var jwtAudience = Environment.GetEnvironmentVariable("API_BASE_URL")
                       ?? _configuration["JwtSettings:Audience"] 
                       ?? _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT SecretKey not configured. Check JWT_SECRET_KEY environment variable or JwtSettings:SecretKey in configuration.");
        }

        if (string.IsNullOrEmpty(jwtIssuer))
        {
            throw new InvalidOperationException("JWT Issuer not configured. Check API_BASE_URL environment variable or JwtSettings:Issuer in configuration.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        // Add user roles
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Get expiry hours from configuration or environment
        var expiryHours = _configuration.GetValue<int>("JwtSettings:ExpiryInHours", 24);
        var expiryMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);
        
        var expiry = expiryMinutes > 0 
            ? DateTime.UtcNow.AddMinutes(expiryMinutes)
            : DateTime.UtcNow.AddHours(expiryHours);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}