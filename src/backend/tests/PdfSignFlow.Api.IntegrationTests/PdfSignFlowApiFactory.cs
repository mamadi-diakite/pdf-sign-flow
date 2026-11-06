using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace PdfSignFlow.Api.IntegrationTests;

/// <summary>Runs the API against its own throwaway SQLite file.</summary>
public class PdfSignFlowApiFactory : WebApplicationFactory<Program>
{
    public static readonly string TestSigningKey =
        Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray());

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), "pdf-sign-flow-tests", $"{Guid.NewGuid():N}.db");

    public string StorageRootPath { get; } =
        Path.Combine(Path.GetTempPath(), "pdf-sign-flow-tests", $"{Guid.NewGuid():N}-files");

    protected virtual string EnvironmentName => "Development";

    protected virtual bool SeedDemoData => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_databasePath}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData.ToString());
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.UseSetting("Storage:RootPath", StorageRootPath);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
        if (Directory.Exists(StorageRootPath))
        {
            Directory.Delete(StorageRootPath, recursive: true);
        }
    }
}

public sealed class DemoDataApiFactory : PdfSignFlowApiFactory
{
    protected override bool SeedDemoData => true;
}

public sealed class ProductionDemoDataApiFactory : PdfSignFlowApiFactory
{
    protected override string EnvironmentName => "Production";

    protected override bool SeedDemoData => true;
}
