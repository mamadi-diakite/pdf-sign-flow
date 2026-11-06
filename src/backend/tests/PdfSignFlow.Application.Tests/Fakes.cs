using PdfSignFlow.Application.Auth;
using PdfSignFlow.Application.Requests;
using PdfSignFlow.Application.Security;
using PdfSignFlow.Application.Storage;
using PdfSignFlow.Application.Users;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public void Add(User user) => Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemorySignatureRequestRepository : ISignatureRequestRepository
{
    public List<SignatureRequest> Requests { get; } = [];

    public List<RequestFile> AddedFiles { get; } = [];

    public int SaveCount { get; private set; }

    public Task<SignatureRequest?> FindForOwnerAsync(Guid requestId, Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(Requests.SingleOrDefault(r => r.Id == requestId && r.OwnerId == ownerId));

    public Task<IReadOnlyList<SignatureRequest>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SignatureRequest>>([.. Requests.Where(r => r.OwnerId == ownerId)]);

    public void Add(SignatureRequest request) => Requests.Add(request);

    public void AddFile(RequestFile file) => AddedFiles.Add(file);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        var key = $"key-{Files.Count + 1}";
        Files[key] = copy.ToArray();

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        Files.Remove(key);
        return Task.CompletedTask;
    }
}
