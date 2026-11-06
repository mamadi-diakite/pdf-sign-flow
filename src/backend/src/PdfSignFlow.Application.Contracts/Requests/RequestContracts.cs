namespace PdfSignFlow.Application.Contracts.Requests;

public sealed record CreateSignatureRequestRequest(string Title, string? Message);

public sealed record SignatureRequestSummary(Guid Id, string Title, string Status, int DocumentCount, DateTime UpdatedAtUtc);

public sealed record SignatureRequestDetails(
    Guid Id,
    string Title,
    string? Message,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<RequestFileResponse> Documents,
    IReadOnlyList<RequestFileResponse> Attachments);

public sealed record RequestFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    int Position,
    DateTime UploadedAtUtc);
