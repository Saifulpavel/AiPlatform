using System.Runtime.CompilerServices;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

public class ChatStreamCommandHandler(IChatProviderResolver resolver, IPromptGuard promptGuard)
    : IStreamRequestHandler<ChatStreamCommand, string>
{
    public async IAsyncEnumerable<string> Handle(
        ChatStreamCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // guardrails — injection block, PII redact (streaming path)
        var message = request.Message;
        if (promptGuard.IsEnabled)
        {
            var guard = promptGuard.Check(message);
            if (guard.Blocked)
                throw new InvalidOperationException(guard.Reason);
            message = guard.SafeInput;
        }

        var provider = resolver.Resolve(request.Provider);
        if (provider is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            throw new InvalidOperationException($"No configured LLM provider available for '{requested}'.");
        }

        await foreach (var token in provider.StreamAsync(
            [new ChatMessage("user", message)], cancellationToken))
        {
            yield return token;
        }
    }
}
