using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using AiPlatform.Application.Features.Chat;
using NSubstitute;
using Xunit;

namespace AiPlatform.Application.Tests.Chat;

public class ChatCommandHandlerTests
{
    private readonly IChatProviderResolver _resolver = Substitute.For<IChatProviderResolver>();
    private readonly ILlmUsageRecorder _usage = Substitute.For<ILlmUsageRecorder>();

    [Fact]
    public async Task KnownProvider_ReturnsAnswer()
    {
        var provider = Substitute.For<IChatProvider>();
        provider.CompleteAsync(Arg.Any<IReadOnlyList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResult("4", "gemini", "gemini-2.5-flash-lite"));
        _resolver.Resolve("gemini").Returns(provider);

        var result = await new ChatCommandHandler(_resolver, _usage)
            .Handle(new ChatCommand("2+2?", "gemini"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("4", result.Value.Answer);
        Assert.Equal("gemini", result.Value.Provider);
    }

    [Fact]
    public async Task UnknownProvider_ReturnsValidationFailure()
    {
        _resolver.Resolve(Arg.Any<string?>()).Returns((IChatProvider?)null);

        var result = await new ChatCommandHandler(_resolver, _usage)
            .Handle(new ChatCommand("hi", "does-not-exist"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
