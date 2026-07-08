using System.Runtime.CompilerServices;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

public class ChatStreamCommandHandler(IChatProviderResolver resolver)
    : IStreamRequestHandler<ChatStreamCommand, string>
{
    public async IAsyncEnumerable<string> Handle(
        ChatStreamCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var provider = resolver.Resolve(request.Provider);
        if (provider is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            throw new InvalidOperationException($"No configured LLM provider available for '{requested}'.");
        }

        await foreach (var token in provider.StreamAsync(
            [new ChatMessage("user", request.Message)], cancellationToken))
        {
            yield return token;
        }
    }
}
