using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DeckDuel2.Extensions;

public static class OpenApiExtensions
{
    public static OpenApiOptions AddBearerSecurityScheme(this OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Enter: Bearer {your JWT token}"
            };

            return Task.CompletedTask;
        });

        return options;
    }
}
