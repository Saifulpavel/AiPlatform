using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Rag;

// Method: keyword | embedding | qdrant (null হলে keyword)। Provider: LLM (null হলে default)।
public record RagQueryCommand(string Question, string? Method, string? Provider) : IRequest<Result<RagResponse>>;
