using Microsoft.Extensions.DependencyInjection;
using PdfSignFlow.Application.Auth;
using PdfSignFlow.Application.Requests;

namespace PdfSignFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<SignatureRequestService>();

        return services;
    }
}
