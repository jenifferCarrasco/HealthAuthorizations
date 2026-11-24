using HealthAuthorization.API.Hubs;
using HealthAuthorization.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace HealthAuthorization.API.Security;

public static class SecurityConfiguration
{
    public const string BusinessAccess = "BusinessAccess";
    public const string Administrator = "Administrator";
    public const string Operator = "Operator";
    public const string DevelopmentScheme = "DevelopmentJwt";
    private const string ProductionScheme = "ProductionJwt";
    private const string DevelopmentIssuer = "dotnet-user-jwts";

    public static void AddApiSecurity(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var development = builder.Environment.IsDevelopment();
        var scheme = development ? DevelopmentScheme : ProductionScheme;
        var jwt = builder.Configuration.GetSection("Jwt");
        if (!development)
        {
            ValidateExternalSettings(builder.Configuration, jwt);
        }

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = scheme;
            options.DefaultChallengeScheme = scheme;
            options.DefaultForbidScheme = scheme;
        }).AddJwtBearer(scheme, options =>
        {
            ConfigureBearer(options, development, jwt);
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(BusinessAccess, policy =>
            {
                policy.RequireAuthenticatedUser().RequireRole(Administrator, Operator);
            });
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.Replace(ServiceDescriptor.Scoped<ICurrentUser, HttpCurrentUser>());
    }

    private static void ValidateExternalSettings(IConfiguration configuration, IConfigurationSection jwt)
    {
        if (!Uri.TryCreate(jwt["Authority"], UriKind.Absolute, out var authority) ||
            authority.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(jwt["Audience"]))
        {
            throw new InvalidOperationException("Configure Jwt:Authority con HTTPS y Jwt:Audience para el proveedor externo.");
        }

        if (configuration.GetSection($"Authentication:Schemes:{ProductionScheme}").Exists())
        {
            throw new InvalidOperationException("Configure el esquema de producción únicamente mediante la sección Jwt.");
        }
    }

    private static void ConfigureBearer(JwtBearerOptions options, bool development, IConfigurationSection jwt)
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        var validation = options.TokenValidationParameters;
        validation.RequireSignedTokens = true;
        validation.ValidateIssuerSigningKey = true;
        validation.ValidateIssuer = true;
        validation.ValidateAudience = true;
        validation.ValidateLifetime = true;
        validation.RequireExpirationTime = true;
        validation.ClockSkew = TimeSpan.FromSeconds(30);
        validation.NameClaimType = HttpCurrentUser.UserIdClaim;

        if (development)
        {
            validation.ValidIssuer = DevelopmentIssuer;
            validation.RoleClaimType = "role";
        }
        else
        {
            options.Authority = jwt["Authority"];
            options.Audience = jwt["Audience"];
            options.RequireHttpsMetadata = true;
            validation.RoleClaimType = jwt["RoleClaimType"] ?? "roles";
            validation.ValidAlgorithms = new[]
            {
                SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
                SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
                SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512
            };
        }

        options.Events.OnTokenValidated = context =>
        {
            if (HttpCurrentUser.GetUserId(context.Principal) is null)
            {
                context.Fail("El token no contiene un identificador de usuario válido.");
            }

            return Task.CompletedTask;
        };
        options.Events.OnMessageReceived = ReadHubTokenAsync;
    }

    private static Task ReadHubTokenAsync(MessageReceivedContext context)
    {
        var request = context.Request;
        if (request.Path != AuthorizationHub.Route || request.Headers.ContainsKey("Authorization"))
        {
            return Task.CompletedTask;
        }

        var isWebSocket = HttpMethods.IsConnect(request.Method) || request.HttpContext.WebSockets.IsWebSocketRequest ||
            request.Headers.Upgrade.Any(value => string.Equals(value, "websocket", StringComparison.OrdinalIgnoreCase));
        var isEventStream = request.GetTypedHeaders().Accept?.Any(value =>
            string.Equals(value.MediaType.Value, "text/event-stream", StringComparison.OrdinalIgnoreCase)) == true;
        if ((isWebSocket || isEventStream) &&
            request.Query.TryGetValue("access_token", out var values) && values.Count == 1)
        {
            context.Token = values[0];
        }

        return Task.CompletedTask;
    }
}
