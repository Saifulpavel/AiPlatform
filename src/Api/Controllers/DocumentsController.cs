using Asp.Versioning;
using AiPlatform.Api.Common;
using AiPlatform.Application.Common.Results;
using AiPlatform.Application.Features.Documents.GetStatus;
using AiPlatform.Application.Features.Documents.Upload;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiPlatform.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/documents")]
public class DocumentsController(IMediator mediator) : ApiControllerBase
{
    // .txt/.md আপলোড → background-এ chunk→embed→Qdrant। সাথে সাথে documentId ফেরত।
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return ToProblem(Error.Validation("Document.Empty", "No file uploaded."));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".txt" or ".md"))
            return ToProblem(Error.Validation("Document.Unsupported", "Only .txt and .md files are supported."));

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);

        var result = await mediator.Send(new UploadDocumentCommand(file.FileName, content), cancellationToken);
        return result.IsSuccess
            ? Accepted(new { documentId = result.Value, status = "Pending" })
            : ToProblem(result.Error);
    }

    // ingestion progress/status
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDocumentStatusQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
