using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ViewerComponent.Configuration;
using ViewerComponent.Services;

namespace ViewerComponent.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViewerComponent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ViewerOptions>(
            configuration.GetSection(ViewerOptions.SectionName));

        services.AddScoped<IViewerService, ViewerService>();
        
        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        return services;
    }
}