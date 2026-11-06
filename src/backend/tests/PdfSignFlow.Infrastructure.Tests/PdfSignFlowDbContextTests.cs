using Microsoft.EntityFrameworkCore;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Infrastructure.Tests;

public sealed class PdfSignFlowDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_a_request_with_its_files()
    {
        var owner = await AddUserAsync("owner@example.com");
        var request = new SignatureRequest(owner.Id, "Lease renewal", "Please sign.", Now);
        request.AddFile(RequestFileKind.Document, "lease.pdf", "application/pdf", 1200, "key-1", Now);
        request.AddFile(RequestFileKind.Attachment, "plan.png", "image/png", 800, "key-2", Now);
        request.AddFile(RequestFileKind.Document, "annex.pdf", "application/pdf", 900, "key-3", Now);

        await using (var db = _database.CreateContext())
        {
            db.SignatureRequests.Add(request);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.SignatureRequests
                .Include(r => r.Files)
                .Include(r => r.Owner)
                .SingleAsync(r => r.Id == request.Id);

            Assert.Equal("Lease renewal", loaded.Title);
            Assert.Equal(SignatureRequestStatus.Draft, loaded.Status);
            Assert.Equal(owner.Email, loaded.Owner!.Email);
            Assert.Equal(["lease.pdf", "annex.pdf"], loaded.Documents.Select(f => f.FileName));
            Assert.Equal(["plan.png"], loaded.Attachments.Select(f => f.FileName));
        }
    }

    [Fact]
    public async Task Removing_a_file_deletes_its_row()
    {
        var owner = await AddUserAsync("owner@example.com");
        var request = new SignatureRequest(owner.Id, "Lease renewal", null, Now);
        var file = request.AddFile(RequestFileKind.Document, "lease.pdf", "application/pdf", 1200, "key-1", Now);

        await using (var db = _database.CreateContext())
        {
            db.SignatureRequests.Add(request);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.SignatureRequests.Include(r => r.Files).SingleAsync();
            loaded.RemoveFile(file.Id, Now.AddMinutes(1));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            Assert.Equal(0, await db.RequestFiles.CountAsync());
            Assert.Equal(Now.AddMinutes(1), (await db.SignatureRequests.SingleAsync()).UpdatedAtUtc);
        }
    }

    [Fact]
    public async Task Reads_dates_back_as_utc()
    {
        await AddUserAsync("owner@example.com");

        await using var db = _database.CreateContext();
        var user = await db.Users.SingleAsync();

        Assert.Equal(DateTimeKind.Utc, user.CreatedAtUtc.Kind);
        Assert.Equal(Now, user.CreatedAtUtc);
    }

    [Fact]
    public async Task Email_is_unique()
    {
        await AddUserAsync("owner@example.com");

        await Assert.ThrowsAsync<DbUpdateException>(() => AddUserAsync("Owner@Example.com"));
    }

    [Fact]
    public async Task A_user_with_requests_cannot_be_deleted()
    {
        var owner = await AddUserAsync("owner@example.com");
        await using (var db = _database.CreateContext())
        {
            db.SignatureRequests.Add(new SignatureRequest(owner.Id, "Lease renewal", null, Now));
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.Users.Remove(await context.Users.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<User> AddUserAsync(string email)
    {
        var user = new User(email, "Owner", "hash", Now);
        await using var db = _database.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }
}
