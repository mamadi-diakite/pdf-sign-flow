namespace PdfSignFlow.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultStorageRootPath = "App_Data/files";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string storageRootPath = DefaultStorageRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PdfSignFlowDbContext>(options => options.UseSqlite(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storageRootPath));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISignatureRequestRepository, SignatureRequestRepository>();
        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PdfSignFlowDbContext>();

        EnsureDataDirectoryExists(db.Database.GetConnectionString());
        await db.Database.MigrateAsync(cancellationToken);
    }

    public static async Task<bool> SeedDemoDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
    }

    // SQLite creates the database file but not its parent folder.
    private static void EnsureDataDirectoryExists(string? connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.Mode == SqliteOpenMode.Memory || builder.DataSource is "" or ":memory:")
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
