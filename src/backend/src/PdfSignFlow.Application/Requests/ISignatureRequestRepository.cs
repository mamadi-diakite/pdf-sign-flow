namespace PdfSignFlow.Application.Requests;

public interface ISignatureRequestRepository
{
    /// <returns>The request with its files, or <c>null</c> when it does not exist or belongs to someone else.</returns>
    Task<SignatureRequest?> FindForOwnerAsync(Guid requestId, Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignatureRequest>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(SignatureRequest request);

    /// <summary>Marks a file added to a loaded request as new.</summary>
    void AddFile(RequestFile file);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
