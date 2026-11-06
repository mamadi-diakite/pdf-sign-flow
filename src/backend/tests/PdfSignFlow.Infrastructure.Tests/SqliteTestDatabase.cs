namespace PdfSignFlow.Infrastructure.Tests;

/// <summary>In-memory SQLite database that lives as long as its connection stays open.</summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.Migrate();
    }

    public PdfSignFlowDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PdfSignFlowDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
