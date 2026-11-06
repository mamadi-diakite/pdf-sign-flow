namespace PdfSignFlow.Infrastructure.Tests;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _user = new("anna.smith@example.com", "Anna", "hash", Now);

    public UserRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Users.Add(_user);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Finds_a_user_by_email_and_by_id()
    {
        await using var db = _database.CreateContext();
        var repository = new UserRepository(db);

        Assert.Equal(_user.Id, (await repository.FindByEmailAsync("anna.smith@example.com", default))?.Id);
        Assert.Equal(_user.Email, (await repository.FindByIdAsync(_user.Id, default))?.Email);
        Assert.Null(await repository.FindByEmailAsync("nobody@example.com", default));
        Assert.Null(await repository.FindByIdAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Adds_a_user_and_saves_a_changed_password()
    {
        var added = new User("lucas@example.com", "Lucas", "hash", Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new UserRepository(db);
            repository.Add(added);
            (await repository.FindByIdAsync(_user.Id, default))!.ChangePassword("new-hash");
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var repositoryCheck = new UserRepository(check);
        Assert.NotNull(await repositoryCheck.FindByIdAsync(added.Id, default));
        Assert.Equal("new-hash", (await repositoryCheck.FindByIdAsync(_user.Id, default))?.PasswordHash);
    }
}
