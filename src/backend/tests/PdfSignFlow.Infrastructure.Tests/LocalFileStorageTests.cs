namespace PdfSignFlow.Infrastructure.Tests;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pdf-sign-flow-tests", Guid.NewGuid().ToString("N"), "files");

    public void Dispose()
    {
        var parent = Path.GetDirectoryName(_root)!;
        if (Directory.Exists(parent))
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task Saves_content_under_a_random_key_in_a_new_folder()
    {
        var storage = new LocalFileStorage(_root);

        var key = await storage.SaveAsync(new MemoryStream([1, 2, 3]), default);
        var other = await storage.SaveAsync(new MemoryStream([4]), default);

        Assert.NotEqual(key, other);
        Assert.Matches("^[0-9a-f]{32}$", key);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Path.Combine(_root, key + ".bin")));
    }

    [Fact]
    public async Task Delete_removes_the_file_and_ignores_a_missing_one()
    {
        var storage = new LocalFileStorage(_root);
        var key = await storage.SaveAsync(new MemoryStream([1]), default);

        await storage.DeleteAsync(key, default);
        await storage.DeleteAsync(key, default);

        Assert.Empty(Directory.GetFiles(_root));
    }

    [Theory]
    [InlineData("../secret")]
    [InlineData("not-a-key")]
    public async Task Refuses_keys_it_did_not_generate(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new LocalFileStorage(_root).DeleteAsync(key, default));
    }

    [Fact]
    public async Task Rejects_missing_arguments()
    {
        Assert.Throws<ArgumentException>(() => new LocalFileStorage(" "));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new LocalFileStorage(_root).SaveAsync(null!, default));
    }
}
