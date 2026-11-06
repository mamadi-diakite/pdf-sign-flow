namespace PdfSignFlow.Domain;

public sealed class RequestFile
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 100;
    public const int StorageKeyMaxLength = 200;
    public const string PdfContentType = "application/pdf";

    private RequestFile()
    {
    }

    internal RequestFile(
        Guid requestId,
        RequestFileKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        int position,
        DateTime uploadedAtUtc)
    {
        Id = Guid.CreateVersion7();
        RequestId = requestId;
        Kind = Guard.Defined(kind, nameof(kind));
        FileName = Guard.Required(fileName, nameof(fileName), FileNameMaxLength);
        ContentType = Guard.Required(contentType, nameof(contentType), ContentTypeMaxLength).ToLowerInvariant();
        SizeBytes = Guard.Positive(sizeBytes, nameof(sizeBytes));
        StorageKey = Guard.Required(storageKey, nameof(storageKey), StorageKeyMaxLength);
        Position = position;
        UploadedAtUtc = Guard.Utc(uploadedAtUtc, nameof(uploadedAtUtc));

        if (Kind == RequestFileKind.Document && ContentType != PdfContentType)
        {
            throw new ArgumentException("Documents to sign must be PDF files.", nameof(contentType));
        }
    }

    public Guid Id { get; private set; }

    public Guid RequestId { get; private set; }

    public RequestFileKind Kind { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>Key of the file in the storage, never shown to users.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    /// <summary>Order of the file among the files of the same kind.</summary>
    public int Position { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }
}
