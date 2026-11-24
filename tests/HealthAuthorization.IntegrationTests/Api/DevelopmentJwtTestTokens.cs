using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using HealthAuthorization.API.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorization.IntegrationTests.Api;

internal class DevelopmentJwtTestTokens
{
    private const string Issuer = "dotnet-user-jwts";
    private const string Audience = "http://localhost:5044";
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

    internal void Configure(WebApplicationBuilder builder)
    {
        var section = $"Authentication:Schemes:{SecurityConfiguration.DevelopmentScheme}";
        builder.Configuration[$"{section}:ValidIssuer"] = Issuer;
        builder.Configuration[$"{section}:ValidAudiences:0"] = Audience;
        builder.Configuration[$"{section}:SigningKeys:0:Issuer"] = Issuer;
        builder.Configuration[$"{section}:SigningKeys:0:Value"] = Convert.ToBase64String(_key);
    }

    internal string Create(string role, string? userId, bool expired = false, bool invalidSignature = false,
        string? issuer = null, string? audience = null, string? secondUserId = null)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim> { new Claim("role", role) };
        if (userId is not null)
        {
            claims.Add(new Claim("healthauthorization_user_id", userId));
        }
        if (secondUserId is not null)
        {
            claims.Add(new Claim("healthauthorization_user_id", secondUserId));
        }

        var key = new SymmetricSecurityKey(invalidSignature ? RandomNumberGenerator.GetBytes(32) : _key);
        var token = new JwtSecurityToken(issuer ?? Issuer, audience ?? Audience, claims, now.AddHours(-2),
            expired ? now.AddHours(-1) : now.AddMinutes(5), new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
