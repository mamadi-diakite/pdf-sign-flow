namespace PdfSignFlow.Domain;

public sealed class SignatureRequest
{
    public const int TitleMaxLength = 200;
    public const int MessageMaxLength = 2000;

    private readonly List<RequestFile> _files = [];

    private SignatureRequest()
    {
    }

    public SignatureRequest(Guid ownerId, string title, string? message, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        OwnerId = Guard.NotEmpty(ownerId, nameof(ownerId));
        Title = Guard.Required(title, nameof(title), TitleMaxLength);
        Message = Guard.Optional(message, nameof(message), MessageMaxLength);
        Status = SignatureRequestStatus.Draft;
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public User? Owner { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Message { get; private set; }

    public SignatureRequestStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<RequestFile> Files => _files;

    public IEnumerable<RequestFile> Documents => OfKind(RequestFileKind.Document);

    public IEnumerable<RequestFile> Attachments => OfKind(RequestFileKind.Attachment);

    /// <remarks>Files must be loaded: the new file is placed after the last one of the same kind.</remarks>
    public RequestFile AddFile(
        RequestFileKind kind,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTime nowUtc)
    {
        var position = _files.Where(f => f.Kind == kind).Select(f => f.Position + 1).DefaultIfEmpty(0).Max();
        var file = new RequestFile(Id, kind, fileName, contentType, sizeBytes, storageKey, position, nowUtc);

        _files.Add(file);
        UpdatedAtUtc = file.UploadedAtUtc;

        return file;
    }

    public void RemoveFile(Guid fileId, DateTime nowUtc)
    {
        var file = _files.SingleOrDefault(f => f.Id == fileId)
            ?? throw new KeyNotFoundException("The file is not part of this request.");

        UpdatedAtUtc = Guard.Utc(nowUtc, nameof(nowUtc));
        _files.Remove(file);
    }

    private IEnumerable<RequestFile> OfKind(RequestFileKind kind) =>
        _files.Where(f => f.Kind == kind).OrderBy(f => f.Position);
}
