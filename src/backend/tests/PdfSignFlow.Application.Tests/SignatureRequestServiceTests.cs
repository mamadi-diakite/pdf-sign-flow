namespace PdfSignFlow.Application.Tests;

public sealed class SignatureRequestServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%fake demo content\n%%EOF");

    private readonly InMemorySignatureRequestRepository _requests = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly SignatureRequestService _service;

    public SignatureRequestServiceTests()
    {
        _service = new SignatureRequestService(_requests, _storage, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Create_adds_a_draft_for_the_owner()
    {
        var result = await _service.CreateAsync(OwnerId, new CreateSignatureRequestRequest(" Lease renewal ", " Please sign. "));

        Assert.Equal(RequestResultStatus.Ok, result.Status);
        var request = Assert.Single(_requests.Requests);
        Assert.Equal(OwnerId, request.OwnerId);
        Assert.Equal(1, _requests.SaveCount);
        var details = result.Value!;
        Assert.Equal(request.Id, details.Id);
        Assert.Equal("Lease renewal", details.Title);
        Assert.Equal("Please sign.", details.Message);
        Assert.Equal("Draft", details.Status);
        Assert.Equal(Now.UtcDateTime, details.CreatedAtUtc);
        Assert.Equal(Now.UtcDateTime, details.UpdatedAtUtc);
        Assert.Empty(details.Documents);
        Assert.Empty(details.Attachments);
    }

    [Fact]
    public async Task Create_accepts_a_missing_message()
    {
        var result = await _service.CreateAsync(OwnerId, new CreateSignatureRequestRequest("Lease renewal", null));

        Assert.Null(result.Value?.Message);
    }

    [Theory]
    [InlineData("", null, "title")]
    [InlineData("  ", null, "title")]
    [InlineData("long-title", null, "title")]
    [InlineData("Lease", "long-message", "message")]
    public async Task Create_validates_title_and_message(string title, string? message, string field)
    {
        title = title == "long-title" ? new string('a', SignatureRequest.TitleMaxLength + 1) : title;
        message = message == "long-message" ? new string('a', SignatureRequest.MessageMaxLength + 1) : message;

        var result = await _service.CreateAsync(OwnerId, new CreateSignatureRequestRequest(title, message));

        Assert.Equal(RequestResultStatus.Invalid, result.Status);
        Assert.Equal([field], result.Errors.Keys);
        Assert.Empty(_requests.Requests);
    }

    [Fact]
    public async Task Create_rejects_a_missing_input()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateAsync(OwnerId, null!));
    }

    [Fact]
    public async Task List_shows_only_own_requests_most_recent_first()
    {
        var older = AddRequest("Older", Now.UtcDateTime.AddDays(-1));
        older.AddFile(RequestFileKind.Document, "a.pdf", RequestFile.PdfContentType, 10, "k1", Now.UtcDateTime.AddDays(-1));
        AddRequest("Newer", Now.UtcDateTime);
        _requests.Requests.Add(new SignatureRequest(Guid.NewGuid(), "Someone else", null, Now.UtcDateTime));

        var list = await _service.ListAsync(OwnerId);

        Assert.Equal(["Newer", "Older"], list.Select(r => r.Title));
        Assert.Equal([0, 1], list.Select(r => r.DocumentCount));
        Assert.All(list, r => Assert.Equal("Draft", r.Status));
    }

    [Fact]
    public async Task Get_returns_documents_and_attachments_of_own_request_only()
    {
        var request = AddRequest("Lease", Now.UtcDateTime);
        request.AddFile(RequestFileKind.Document, "lease.pdf", RequestFile.PdfContentType, 10, "k1", Now.UtcDateTime);
        request.AddFile(RequestFileKind.Attachment, "plan.png", "image/png", 20, "k2", Now.UtcDateTime);

        var details = await _service.GetAsync(OwnerId, request.Id);

        Assert.Equal(["lease.pdf"], details!.Documents.Select(d => d.FileName));
        Assert.Equal(["plan.png"], details.Attachments.Select(d => d.FileName));
        Assert.Null(await _service.GetAsync(Guid.NewGuid(), request.Id));
    }

    [Fact]
    public async Task Add_document_stores_the_pdf_and_records_it()
    {
        var request = AddRequest("Lease", Now.UtcDateTime.AddHours(-1));

        var result = await AddDocumentAsync(request.Id, "fakepath/ lease.pdf ", Pdf);

        Assert.Equal(RequestResultStatus.Ok, result.Status);
        var stored = Assert.Single(_storage.Files);
        Assert.Equal(Pdf, stored.Value);
        var file = Assert.Single(request.Documents);
        Assert.Same(file, Assert.Single(_requests.AddedFiles));
        Assert.Equal(stored.Key, file.StorageKey);
        Assert.Equal(new RequestFileResponse(file.Id, "lease.pdf", "application/pdf", Pdf.Length, 0, Now.UtcDateTime), result.Value);
        Assert.Equal(Now.UtcDateTime, request.UpdatedAtUtc);
        Assert.Equal(1, _requests.SaveCount);
    }

    [Fact]
    public async Task Add_document_to_an_unknown_request()
    {
        var result = await AddDocumentAsync(Guid.NewGuid(), "lease.pdf", Pdf);

        Assert.Equal(RequestResultStatus.NotFound, result.Status);
        Assert.Empty(_storage.Files);
    }

    [Theory]
    [InlineData("empty", "The file is empty.")]
    [InlineData("too-large", "The file is larger than 20 MB.")]
    [InlineData("no-name", "The file has no name.")]
    [InlineData("long-name", "The file name is longer than 255 characters.")]
    [InlineData("not-pdf", "Only PDF files can be signed.")]
    [InlineData("tiny", "Only PDF files can be signed.")]
    public async Task Add_document_rejects_invalid_files(string @case, string message)
    {
        var request = AddRequest("Lease", Now.UtcDateTime);
        var (name, bytes, length) = @case switch
        {
            "empty" => ("lease.pdf", Array.Empty<byte>(), 0L),
            "too-large" => ("lease.pdf", Pdf, SignatureRequestService.MaxDocumentBytes + 1),
            "no-name" => ((string?)null, Pdf, Pdf.Length),
            "long-name" => (new string('a', 252) + ".pdf", Pdf, Pdf.Length),
            "not-pdf" => ("lease.pdf", Encoding.ASCII.GetBytes("PK\u0003\u0004 not a pdf"), 20L),
            _ => ("lease.pdf", Encoding.ASCII.GetBytes("%P"), 2L),
        };

        await using var content = new MemoryStream(bytes);
        var result = await _service.AddDocumentAsync(OwnerId, request.Id, name, content, length);

        Assert.Equal(RequestResultStatus.Invalid, result.Status);
        Assert.Equal([message], result.Errors["file"]);
        Assert.Empty(_storage.Files);
        Assert.Empty(request.Files);
    }

    [Fact]
    public async Task Add_document_rejects_a_missing_stream()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddDocumentAsync(OwnerId, Guid.NewGuid(), "a.pdf", null!, 1));
    }

    [Fact]
    public async Task Remove_file_deletes_the_row_and_the_stored_content()
    {
        var request = AddRequest("Lease", Now.UtcDateTime);
        await AddDocumentAsync(request.Id, "lease.pdf", Pdf);
        var file = Assert.Single(request.Files);

        Assert.True(await _service.RemoveFileAsync(OwnerId, request.Id, file.Id));

        Assert.Empty(request.Files);
        Assert.Empty(_storage.Files);
        Assert.Equal(2, _requests.SaveCount);
    }

    [Fact]
    public async Task Remove_file_returns_false_for_unknown_request_or_file()
    {
        var request = AddRequest("Lease", Now.UtcDateTime);

        Assert.False(await _service.RemoveFileAsync(OwnerId, Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(await _service.RemoveFileAsync(OwnerId, request.Id, Guid.NewGuid()));
        Assert.Equal(0, _requests.SaveCount);
    }

    private SignatureRequest AddRequest(string title, DateTime createdAtUtc)
    {
        var request = new SignatureRequest(OwnerId, title, null, createdAtUtc);
        _requests.Requests.Add(request);

        return request;
    }

    private async Task<RequestResult<RequestFileResponse>> AddDocumentAsync(Guid requestId, string name, byte[] bytes)
    {
        await using var content = new MemoryStream(bytes);

        return await _service.AddDocumentAsync(OwnerId, requestId, name, content, bytes.Length);
    }
}
