using Microsoft.Extensions.DependencyInjection;
using PdfSignFlow.Application.Auth;

namespace PdfSignFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();

        return services;
    }
}
