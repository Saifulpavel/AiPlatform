using System.Diagnostics;
using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Agent;

// agentic loop: LLM-কে tools দিই → tool_calls এলে execute করে ফল feed back → content এলে থামি।
// MaxIterations = safety ceiling (runaway loop রোধ)।
public class RunAgentCommandHandler(
    IChatProviderResolver resolver,
    IEnumerable<ITool> tools,
    ILlmUsageRecorder usageRecorder,
    AgentSettings settings)
    : IRequestHandler<RunAgentCommand, Result<AgentResponse>>
{
    private readonly AgentSettings _settings = settings;

    private const string SystemPrompt =
        "You are a helpful assistant with access to tools. Call a tool when it helps answer accurately; " +
        "otherwise answer directly. When you have enough information, give the final answer in Bengali.";

    public async Task<Result<AgentResponse>> Handle(RunAgentCommand request, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
            return Error.Validation("Agent.Disabled", "Agent feature is disabled.");

        // tool-calling সমর্থন করে এমন provider লাগবে (OpenAI-compatible)।
        if (resolver.Resolve(request.Provider) is not IToolCallingProvider provider)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            return Error.Validation("Agent.NoToolSupport",
                $"Provider '{requested}' does not support tool-calling. Use an OpenAI-compatible provider (e.g. groq).");
        }

        var toolMap = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var toolDefs = tools
            .Select(t => new ToolDefinition(t.Name, t.Description, t.ParametersSchema))
            .ToList();

        var messages = new List<ChatMessage>
        {
            new("system", SystemPrompt),
            new("user", request.Message),
        };
        var toolsUsed = new List<string>();

        for (var iteration = 0; iteration < _settings.MaxIterations; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await provider.CompleteWithToolsAsync(messages, toolDefs, cancellationToken);
            stopwatch.Stop();

            usageRecorder.Record(new LlmUsageRecord(
                "agent", result.Provider, result.Model, result.InputTokens, result.OutputTokens, stopwatch.ElapsedMilliseconds));

            // tool call না চাইলে = final answer।
            if (result.ToolCalls.Count == 0)
                return new AgentResponse(result.Content ?? string.Empty, toolsUsed);

            // assistant-এর tool-call decision history-তে রাখি (নইলে পরের turn context হারায়)।
            messages.Add(new ChatMessage("assistant", result.Content ?? string.Empty, ToolCalls: result.ToolCalls));

            foreach (var call in result.ToolCalls)
            {
                toolsUsed.Add(call.Name);
                var output = await ExecuteToolAsync(toolMap, call, cancellationToken);
                messages.Add(new ChatMessage("tool", output, ToolCallId: call.Id));
            }
        }

        return Error.Failure("Agent.MaxIterations",
            $"Agent did not finish within {_settings.MaxIterations} steps.");
    }

    private static async Task<string> ExecuteToolAsync(
        IReadOnlyDictionary<string, ITool> toolMap, ToolCall call, CancellationToken cancellationToken)
    {
        if (!toolMap.TryGetValue(call.Name, out var tool))
            return $"Unknown tool: {call.Name}";

        try
        {
            using var args = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            return await tool.ExecuteAsync(args.RootElement, cancellationToken);
        }
        catch (Exception ex)
        {
            // tool ব্যর্থ হলে LLM-কে জানাই (throw করলে পুরো agent মরে) — সে recover করতে পারে।
            return $"Tool '{call.Name}' failed: {ex.Message}";
        }
    }
}
