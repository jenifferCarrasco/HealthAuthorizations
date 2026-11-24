using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace HealthAuthorization.API.Security;

public static class OpenApiSecurity
{
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        return services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                };
                document.Security ??= new List<OpenApiSecurityRequirement>();
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Responses ??= new OpenApiResponses();
                    operation.Responses.TryAdd("401", new OpenApiResponse
                    {
                        Description = "Se requiere un token de acceso válido."
                    });
                    operation.Responses.TryAdd("403", new OpenApiResponse
                    {
                        Description = "El usuario no tiene permiso para la operación."
                    });
                }

                return Task.CompletedTask;
            });
        });
    }
}
