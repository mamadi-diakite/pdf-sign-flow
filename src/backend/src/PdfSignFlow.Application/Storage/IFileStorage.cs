namespace PdfSignFlow.Application.Storage;

/// <summary>Stores uploaded files outside the web root. Keys are opaque and never shown to users.</summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, CancellationToken cancellationToken);

    /// <remarks>Deleting a key that no longer exists is not an error.</remarks>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
