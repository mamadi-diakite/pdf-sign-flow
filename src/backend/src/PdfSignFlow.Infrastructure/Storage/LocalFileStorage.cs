namespace PdfSignFlow.Infrastructure.Storage;

/// <summary>Stores each file under a random name in a folder that the web server does not serve.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _root = Path.GetFullPath(rootPath);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        Directory.CreateDirectory(_root);
        var key = Guid.NewGuid().ToString("N");
        await using var file = new FileStream(PathFor(key), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathFor(key));

        return Task.CompletedTask;
    }

    // Keys are generated here, so anything else (such as "../x") is refused instead of resolved.
    private string PathFor(string key) =>
        Guid.TryParseExact(key, "N", out _)
            ? Path.Combine(_root, key + ".bin")
            : throw new ArgumentException("Unknown storage key.", nameof(key));
}
