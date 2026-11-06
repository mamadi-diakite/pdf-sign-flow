namespace PdfSignFlow.Application.Requests;

public sealed class SignatureRequestService(
    ISignatureRequestRepository requests,
    IFileStorage storage,
    TimeProvider timeProvider)
{
    public const long MaxDocumentBytes = 20 * 1024 * 1024;

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public async Task<RequestResult<SignatureRequestDetails>> CreateAsync(
        Guid ownerId,
        CreateSignatureRequestRequest input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors["title"] = ["Title is required."];
        }
        else if (input.Title.Trim().Length > SignatureRequest.TitleMaxLength)
        {
            errors["title"] = [$"Use at most {SignatureRequest.TitleMaxLength} characters."];
        }

        if (input.Message?.Trim().Length > SignatureRequest.MessageMaxLength)
        {
            errors["message"] = [$"Use at most {SignatureRequest.MessageMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            return RequestResult<SignatureRequestDetails>.Invalid(errors);
        }

        var request = new SignatureRequest(ownerId, input.Title, input.Message, Now());
        requests.Add(request);
        await requests.SaveChangesAsync(cancellationToken);

        return RequestResult<SignatureRequestDetails>.Ok(ToDetails(request));
    }

    public async Task<IReadOnlyList<SignatureRequestSummary>> ListAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var owned = await requests.ListForOwnerAsync(ownerId, cancellationToken);

        return
        [
            .. owned
                .OrderByDescending(r => r.UpdatedAtUtc)
                .Select(r => new SignatureRequestSummary(r.Id, r.Title, r.Status.ToString(), r.Documents.Count(), r.UpdatedAtUtc)),
        ];
    }

    public async Task<SignatureRequestDetails?> GetAsync(Guid ownerId, Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await requests.FindForOwnerAsync(requestId, ownerId, cancellationToken);

        return request is null ? null : ToDetails(request);
    }

    /// <param name="length">Size announced by the upload, checked before the content is read.</param>
    public async Task<RequestResult<RequestFileResponse>> AddDocumentAsync(
        Guid ownerId,
        Guid requestId,
        string? fileName,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var request = await requests.FindForOwnerAsync(requestId, ownerId, cancellationToken);
        if (request is null)
        {
            return RequestResult<RequestFileResponse>.NotFound;
        }

        var name = Path.GetFileName(fileName)?.Trim();
        var error = length switch
        {
            <= 0 => "The file is empty.",
            > MaxDocumentBytes => $"The file is larger than {MaxDocumentBytes / 1024 / 1024} MB.",
            _ when string.IsNullOrEmpty(name) => "The file has no name.",
            _ when name.Length > RequestFile.FileNameMaxLength => $"The file name is longer than {RequestFile.FileNameMaxLength} characters.",
            _ => null,
        };

        await using var buffer = new MemoryStream();
        if (error is null)
        {
            await content.CopyToAsync(buffer, cancellationToken);
            if (!buffer.GetBuffer().AsSpan(0, (int)buffer.Length).StartsWith(PdfSignature))
            {
                error = "Only PDF files can be signed.";
            }
        }

        if (error is not null)
        {
            return RequestResult<RequestFileResponse>.Invalid(new Dictionary<string, string[]> { ["file"] = [error] });
        }

        buffer.Position = 0;
        var key = await storage.SaveAsync(buffer, cancellationToken);
        var file = request.AddFile(RequestFileKind.Document, name!, RequestFile.PdfContentType, buffer.Length, key, Now());
        requests.AddFile(file);
        await requests.SaveChangesAsync(cancellationToken);

        return RequestResult<RequestFileResponse>.Ok(ToResponse(file));
    }

    /// <returns><c>false</c> when the request or the file does not exist for this owner.</returns>
    public async Task<bool> RemoveFileAsync(Guid ownerId, Guid requestId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var request = await requests.FindForOwnerAsync(requestId, ownerId, cancellationToken);
        var file = request?.Files.SingleOrDefault(f => f.Id == fileId);
        if (file is null)
        {
            return false;
        }

        request!.RemoveFile(fileId, Now());
        await requests.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(file.StorageKey, cancellationToken);

        return true;
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    private static SignatureRequestDetails ToDetails(SignatureRequest request) => new(
        request.Id,
        request.Title,
        request.Message,
        request.Status.ToString(),
        request.CreatedAtUtc,
        request.UpdatedAtUtc,
        [.. request.Documents.Select(ToResponse)],
        [.. request.Attachments.Select(ToResponse)]);

    private static RequestFileResponse ToResponse(RequestFile file) =>
        new(file.Id, file.FileName, file.ContentType, file.SizeBytes, file.Position, file.UploadedAtUtc);
}
