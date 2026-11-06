namespace PdfSignFlow.Domain.Tests;

public sealed class SignatureRequestTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void New_request_is_a_draft_without_files()
    {
        var request = new SignatureRequest(OwnerId, " Lease renewal ", " Please sign before Friday. ", TestDates.Now);

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(OwnerId, request.OwnerId);
        Assert.Equal("Lease renewal", request.Title);
        Assert.Equal("Please sign before Friday.", request.Message);
        Assert.Equal(SignatureRequestStatus.Draft, request.Status);
        Assert.Equal(TestDates.Now, request.CreatedAtUtc);
        Assert.Equal(TestDates.Now, request.UpdatedAtUtc);
        Assert.Empty(request.Files);
    }

    [Fact]
    public void Message_is_optional()
    {
        var request = new SignatureRequest(OwnerId, "Lease renewal", "  ", TestDates.Now);

        Assert.Null(request.Message);
    }

    [Fact]
    public void Rejects_empty_owner()
    {
        var error = Assert.Throws<ArgumentException>(() => new SignatureRequest(Guid.Empty, "Lease", null, TestDates.Now));

        Assert.Equal("ownerId", error.ParamName);
    }

    [Fact]
    public void Rejects_blank_title()
    {
        var error = Assert.Throws<ArgumentException>(() => new SignatureRequest(OwnerId, " ", null, TestDates.Now));

        Assert.Equal("title", error.ParamName);
    }

    [Fact]
    public void Rejects_message_over_the_limit()
    {
        var message = new string('a', SignatureRequest.MessageMaxLength + 1);

        var error = Assert.Throws<ArgumentException>(() => new SignatureRequest(OwnerId, "Lease", message, TestDates.Now));

        Assert.Equal("message", error.ParamName);
    }

    [Fact]
    public void Rejects_non_utc_creation_date()
    {
        var local = DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local);

        var error = Assert.Throws<ArgumentException>(() => new SignatureRequest(OwnerId, "Lease", null, local));

        Assert.Equal("createdAtUtc", error.ParamName);
    }

    [Fact]
    public void Adds_documents_and_attachments_in_their_own_order()
    {
        var request = NewRequest();
        var later = TestDates.Now.AddMinutes(5);

        var lease = request.AddFile(RequestFileKind.Document, "lease.pdf", "application/pdf", 1200, "key-1", TestDates.Now);
        var plan = request.AddFile(RequestFileKind.Attachment, "floor-plan.png", "image/png", 800, "key-2", TestDates.Now);
        var annex = request.AddFile(RequestFileKind.Document, "annex.pdf", "Application/PDF", 900, "key-3", later);

        Assert.Equal([lease, annex], request.Documents);
        Assert.Equal([plan], request.Attachments);
        Assert.Equal(0, lease.Position);
        Assert.Equal(1, annex.Position);
        Assert.Equal(0, plan.Position);
        Assert.Equal(later, request.UpdatedAtUtc);
    }

    [Fact]
    public void Added_file_keeps_its_details()
    {
        var request = NewRequest();

        var file = request.AddFile(RequestFileKind.Attachment, " notes.docx ", "Application/Msword", 4096, "key-1", TestDates.Now);

        Assert.NotEqual(Guid.Empty, file.Id);
        Assert.Equal(request.Id, file.RequestId);
        Assert.Equal(RequestFileKind.Attachment, file.Kind);
        Assert.Equal("notes.docx", file.FileName);
        Assert.Equal("application/msword", file.ContentType);
        Assert.Equal(4096, file.SizeBytes);
        Assert.Equal("key-1", file.StorageKey);
        Assert.Equal(TestDates.Now, file.UploadedAtUtc);
    }

    [Fact]
    public void Documents_to_sign_must_be_pdf()
    {
        var request = NewRequest();

        var error = Assert.Throws<ArgumentException>(
            () => request.AddFile(RequestFileKind.Document, "lease.docx", "application/msword", 100, "key", TestDates.Now));

        Assert.Equal("contentType", error.ParamName);
        Assert.Empty(request.Files);
    }

    [Theory]
    [InlineData(" ", "application/pdf", 100L, "key", "fileName")]
    [InlineData("a.pdf", " ", 100L, "key", "contentType")]
    [InlineData("a.pdf", "application/pdf", 0L, "key", "sizeBytes")]
    [InlineData("a.pdf", "application/pdf", 100L, " ", "storageKey")]
    public void Rejects_invalid_file_details(string fileName, string contentType, long size, string key, string paramName)
    {
        var request = NewRequest();

        var error = Assert.ThrowsAny<ArgumentException>(
            () => request.AddFile(RequestFileKind.Document, fileName, contentType, size, key, TestDates.Now));

        Assert.Equal(paramName, error.ParamName);
    }

    [Fact]
    public void Rejects_unknown_file_kind()
    {
        var request = NewRequest();

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => request.AddFile((RequestFileKind)9, "a.pdf", "application/pdf", 100, "key", TestDates.Now));

        Assert.Equal("kind", error.ParamName);
    }

    [Fact]
    public void Rejects_non_utc_upload_date()
    {
        var request = NewRequest();
        var local = DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local);

        var error = Assert.Throws<ArgumentException>(
            () => request.AddFile(RequestFileKind.Document, "a.pdf", "application/pdf", 100, "key", local));

        Assert.Equal("uploadedAtUtc", error.ParamName);
    }

    [Fact]
    public void Removes_a_file()
    {
        var request = NewRequest();
        var lease = request.AddFile(RequestFileKind.Document, "lease.pdf", "application/pdf", 1200, "key-1", TestDates.Now);
        var later = TestDates.Now.AddMinutes(10);

        request.RemoveFile(lease.Id, later);

        Assert.Empty(request.Files);
        Assert.Equal(later, request.UpdatedAtUtc);
    }

    [Fact]
    public void Next_file_goes_after_the_last_one_even_after_a_removal()
    {
        var request = NewRequest();
        var first = request.AddFile(RequestFileKind.Document, "a.pdf", "application/pdf", 100, "key-1", TestDates.Now);
        request.AddFile(RequestFileKind.Document, "b.pdf", "application/pdf", 100, "key-2", TestDates.Now);
        request.RemoveFile(first.Id, TestDates.Now);

        var third = request.AddFile(RequestFileKind.Document, "c.pdf", "application/pdf", 100, "key-3", TestDates.Now);

        Assert.Equal(2, third.Position);
    }

    [Fact]
    public void Removing_an_unknown_file_fails()
    {
        var request = NewRequest();

        Assert.Throws<KeyNotFoundException>(() => request.RemoveFile(Guid.NewGuid(), TestDates.Now));
    }

    [Fact]
    public void Removing_with_a_non_utc_date_fails_and_keeps_the_file()
    {
        var request = NewRequest();
        var lease = request.AddFile(RequestFileKind.Document, "lease.pdf", "application/pdf", 1200, "key-1", TestDates.Now);

        Assert.Throws<ArgumentException>(
            () => request.RemoveFile(lease.Id, DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local)));

        Assert.Single(request.Files);
    }

    private static SignatureRequest NewRequest() => new(OwnerId, "Lease renewal", null, TestDates.Now);
}
