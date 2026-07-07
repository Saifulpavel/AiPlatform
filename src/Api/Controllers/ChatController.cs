using Asp.Versioning;
using AiPlatform.Api.Common;
using AiPlatform.Api.Contracts;
using AiPlatform.Application.Features.Chat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiPlatform.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/chat")]
public class ChatController(IMediator mediator) : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Chat(ChatRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ChatCommand(request.Message, request.Provider), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }
}
