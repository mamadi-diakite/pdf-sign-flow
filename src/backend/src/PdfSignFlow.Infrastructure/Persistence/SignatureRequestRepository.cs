using Microsoft.EntityFrameworkCore;
using PdfSignFlow.Application.Requests;
using PdfSignFlow.Domain;

namespace PdfSignFlow.Infrastructure.Persistence;

public sealed class SignatureRequestRepository(PdfSignFlowDbContext db) : ISignatureRequestRepository
{
    public Task<SignatureRequest?> FindForOwnerAsync(Guid requestId, Guid ownerId, CancellationToken cancellationToken) =>
        db.SignatureRequests
            .Include(r => r.Files)
            .SingleOrDefaultAsync(r => r.Id == requestId && r.OwnerId == ownerId, cancellationToken);

    public async Task<IReadOnlyList<SignatureRequest>> ListForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.SignatureRequests
            .AsNoTracking()
            .Include(r => r.Files)
            .Where(r => r.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

    public void Add(SignatureRequest request) => db.SignatureRequests.Add(request);

    // A client-generated key on a new child would otherwise be taken for an existing row.
    public void AddFile(RequestFile file) => db.RequestFiles.Add(file);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
