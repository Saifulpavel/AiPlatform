using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using AiPlatform.Application.Features.Agent;
using AiPlatform.Infrastructure.Agent;
using NSubstitute;

namespace AiPlatform.Application.Tests.Agent;

public class RunAgentCommandHandlerTests
{
    private readonly IChatProviderResolver _resolver = Substitute.For<IChatProviderResolver>();
    private readonly ILlmUsageRecorder _usage = Substitute.For<ILlmUsageRecorder>();
    private readonly ITool[] _tools = [new CurrentTimeTool()];

    private RunAgentCommandHandler Handler(AgentSettings? settings = null) =>
        new(_resolver, _tools, _usage, settings ?? new AgentSettings { Enabled = true, MaxIterations = 5 });

    // IChatProvider + IToolCallingProvider দুইই — resolver.Resolve এটা ফেরায়।
    private static IChatProvider ToolProvider(out IToolCallingProvider tool)
    {
        var provider = Substitute.For<IChatProvider, IToolCallingProvider>();
        tool = (IToolCallingProvider)provider;
        return provider;
    }

    [Fact]
    public async Task CallsTool_ThenReturnsFinalAnswer()
    {
        var provider = ToolProvider(out var tool);
        tool.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<IReadOnlyList<ToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(
                new ChatToolResult(null, [new ToolCall("c1", "current_time", "{}")], "groq", "llama"),
                new ChatToolResult("এখন সময় জানানো হলো।", [], "groq", "llama"));
        _resolver.Resolve("groq").Returns(provider);

        var result = await Handler().Handle(new RunAgentCommand("কয়টা বাজে?", "groq"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("এখন সময় জানানো হলো।", result.Value.Answer);
        Assert.Contains("current_time", result.Value.ToolsUsed);
    }

    [Fact]
    public async Task NoToolCalls_ReturnsAnswerImmediately()
    {
        var provider = ToolProvider(out var tool);
        tool.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<IReadOnlyList<ToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new ChatToolResult("সরাসরি উত্তর", [], "groq", "llama"));
        _resolver.Resolve("groq").Returns(provider);

        var result = await Handler().Handle(new RunAgentCommand("hi", "groq"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.ToolsUsed);
    }

    [Fact]
    public async Task ExceedsMaxIterations_ReturnsFailure()
    {
        var provider = ToolProvider(out var tool);
        // সবসময় tool call → কখনো শেষ হয় না
        tool.CompleteWithToolsAsync(Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<IReadOnlyList<ToolDefinition>>(), Arg.Any<CancellationToken>())
            .Returns(new ChatToolResult(null, [new ToolCall("c1", "current_time", "{}")], "groq", "llama"));
        _resolver.Resolve("groq").Returns(provider);

        var result = await Handler(new AgentSettings { Enabled = true, MaxIterations = 2 })
            .Handle(new RunAgentCommand("loop", "groq"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Agent.MaxIterations", result.Error.Code);
    }

    [Fact]
    public async Task ProviderWithoutToolSupport_ReturnsValidationError()
    {
        _resolver.Resolve("gemini").Returns(Substitute.For<IChatProvider>()); // tool-calling নেই

        var result = await Handler().Handle(new RunAgentCommand("hi", "gemini"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Disabled_ReturnsValidationError()
    {
        var result = await Handler(new AgentSettings { Enabled = false })
            .Handle(new RunAgentCommand("hi", "groq"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Agent.Disabled", result.Error.Code);
    }
}
