using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Agent;

// tool-agent: LLM tool call করে, execute হয়, feed back — final answer পর্যন্ত। Provider tool-capable হতে হবে।
public record RunAgentCommand(string Message, string? Provider) : IRequest<Result<AgentResponse>>;

// ToolsUsed = কোন কোন tool কল হয়েছে (transparency/observability)।
// LatencyMs = সব LLM turn-এর যোগফল (tool execution বাদ)। agent একাধিক turn নেয়,
// তাই এটা এক call-এর নয় — পুরো loop-এ model কত সময় নিল।
public record AgentResponse(string Answer, IReadOnlyList<string> ToolsUsed, long LatencyMs = 0);
