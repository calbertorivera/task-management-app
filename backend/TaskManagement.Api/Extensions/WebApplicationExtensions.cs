using Microsoft.AspNetCore.Builder;

namespace TaskManagement.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseTaskManagementPipeline(
        this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(
            ServiceCollectionExtensions.FrontendCorsPolicyName);

        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/openapi/v1.json",
                "TaskManagement API v1");
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}