using Asp.Versioning;
using AiPlatform.Api.Common;
using AiPlatform.Api.Contracts;
using AiPlatform.Application.Features.Analyzer;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiPlatform.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analyze")]
public class AnalyzerController(IMediator mediator) : ApiControllerBase
{
    // customer message → typed { sentiment, category, priority, summary }
    [HttpPost]
    public async Task<IActionResult> Analyze(AnalyzeRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AnalyzeCommand(request.Text, request.Provider), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
