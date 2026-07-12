using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Agent;

// tool-agent: LLM tool call করে, execute হয়, feed back — final answer পর্যন্ত। Provider tool-capable হতে হবে।
public record RunAgentCommand(string Message, string? Provider) : IRequest<Result<AgentResponse>>;

// ToolsUsed = কোন কোন tool কল হয়েছে (transparency/observability)।
public record AgentResponse(string Answer, IReadOnlyList<string> ToolsUsed);
