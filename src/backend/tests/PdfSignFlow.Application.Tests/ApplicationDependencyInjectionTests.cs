using Microsoft.Extensions.DependencyInjection;
using PdfSignFlow.Application.Auth;
using PdfSignFlow.Application.Requests;
using PdfSignFlow.Application.Security;
using PdfSignFlow.Application.Storage;
using PdfSignFlow.Application.Users;

namespace PdfSignFlow.Application.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void Registers_the_auth_service()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IUserRepository, InMemoryUserRepository>()
            .AddSingleton<IPasswordHasher, FakePasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeTokenIssuer>()
            .AddSingleton<ISignatureRequestRepository, InMemorySignatureRequestRepository>()
            .AddSingleton<IFileStorage, InMemoryFileStorage>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<SignatureRequestService>());
    }
}
