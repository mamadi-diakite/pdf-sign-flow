using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfSignFlow.Infrastructure.Persistence;

namespace PdfSignFlow.Api.IntegrationTests;

public sealed class DatabaseStartupTests(PdfSignFlowApiFactory factory) : IClassFixture<PdfSignFlowApiFactory>
{
    [Fact]
    public async Task Startup_applies_all_migrations()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PdfSignFlowDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }
}
