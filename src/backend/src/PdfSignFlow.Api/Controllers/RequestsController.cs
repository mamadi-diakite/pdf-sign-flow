using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSignFlow.Api.Auth;
using PdfSignFlow.Application.Contracts.Requests;
using PdfSignFlow.Application.Requests;

namespace PdfSignFlow.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public sealed class RequestsController(SignatureRequestService service) : ControllerBase
{
    // Room for the multipart envelope around the largest accepted PDF.
    private const long MaxUploadBytes = SignatureRequestService.MaxDocumentBytes + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SignatureRequestSummary>>> List(CancellationToken cancellationToken) =>
        User.GetUserId() is { } userId ? Ok(await service.ListAsync(userId, cancellationToken)) : Unauthorized();

    [HttpPost]
    public async Task<ActionResult<SignatureRequestDetails>> Create(
        CreateSignatureRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var result = await service.CreateAsync(userId, request, cancellationToken);

        return result.Value is { } details
            ? CreatedAtAction(nameof(Get), new { id = details.Id }, details)
            : InvalidRequest(result.Errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SignatureRequestDetails>> Get(Guid id, CancellationToken cancellationToken)
    {
        var details = User.GetUserId() is { } userId ? await service.GetAsync(userId, id, cancellationToken) : null;

        return details is null ? NotFound() : Ok(details);
    }

    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<ActionResult<RequestFileResponse>> AddDocument(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return NotFound();
        }

        await using var content = file.OpenReadStream();
        var result = await service.AddDocumentAsync(userId, id, file.FileName, content, file.Length, cancellationToken);

        return result.Status switch
        {
            RequestResultStatus.Ok => Created($"/api/requests/{id}", result.Value),
            RequestResultStatus.NotFound => NotFound(),
            _ => InvalidRequest(result.Errors),
        };
    }

    [HttpDelete("{id:guid}/files/{fileId:guid}")]
    public async Task<IActionResult> RemoveFile(Guid id, Guid fileId, CancellationToken cancellationToken)
    {
        var removed = User.GetUserId() is { } userId
            && await service.RemoveFileAsync(userId, id, fileId, cancellationToken);

        return removed ? NoContent() : NotFound();
    }

    private ActionResult InvalidRequest(IReadOnlyDictionary<string, string[]> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
            {
                ModelState.AddModelError(field, message);
            }
        }

        return ValidationProblem();
    }
}
