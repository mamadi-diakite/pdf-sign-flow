namespace PdfSignFlow.Infrastructure.Tests;

public sealed class SignatureRequestRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _owner = new("anna@example.com", "Anna", "hash", Now);
    private readonly User _other = new("lucas@example.com", "Lucas", "hash", Now);

    public SignatureRequestRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Users.AddRange(_owner, _other);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Adds_a_request_then_finds_it_for_its_owner_only()
    {
        var request = new SignatureRequest(_owner.Id, "Lease", null, Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new SignatureRequestRepository(db);
            repository.Add(request);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var repositoryCheck = new SignatureRequestRepository(check);
        Assert.NotNull(await repositoryCheck.FindForOwnerAsync(request.Id, _owner.Id, default));
        Assert.Null(await repositoryCheck.FindForOwnerAsync(request.Id, _other.Id, default));
        Assert.Single(await repositoryCheck.ListForOwnerAsync(_owner.Id, default));
        Assert.Empty(await repositoryCheck.ListForOwnerAsync(_other.Id, default));
    }

    [Fact]
    public async Task Adds_a_file_to_a_saved_request_and_lists_it()
    {
        var request = new SignatureRequest(_owner.Id, "Lease", null, Now);
        await using (var db = _database.CreateContext())
        {
            db.SignatureRequests.Add(request);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var repository = new SignatureRequestRepository(db);
            var loaded = await repository.FindForOwnerAsync(request.Id, _owner.Id, default);
            repository.AddFile(loaded!.AddFile(RequestFileKind.Document, "lease.pdf", RequestFile.PdfContentType, 10, "key-1", Now));
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var listed = Assert.Single(await new SignatureRequestRepository(check).ListForOwnerAsync(_owner.Id, default));
        Assert.Equal(["lease.pdf"], listed.Documents.Select(f => f.FileName));
        Assert.Equal(1, await check.RequestFiles.CountAsync());
    }
}
