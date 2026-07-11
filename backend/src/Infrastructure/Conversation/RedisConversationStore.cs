using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AiPlatform.Infrastructure.Conversation;

// conversation state Redis-এ JSON হিসেবে; key user-scoped তাই cross-user access হয় না।
//   conv:{userId}:{conversationId} → ConversationState  (TTL শেষ activity থেকে slide করে)
public class RedisConversationStore(IConnectionMultiplexer redis, IOptions<ConversationSettings> options)
    : IConversationStore
{
    private readonly ConversationSettings _settings = options.Value;

    public bool IsEnabled => _settings.Enabled;
    public int MaxRecentMessages => _settings.MaxRecentMessages;

    private static string Key(string userId, string conversationId) => $"conv:{userId}:{conversationId}";

    public async Task<ConversationState> GetAsync(string userId, string conversationId, CancellationToken cancellationToken)
    {
        var json = await redis.GetDatabase().StringGetAsync(Key(userId, conversationId));
        if (json.IsNullOrEmpty)
            return ConversationState.Empty;

        return JsonSerializer.Deserialize<ConversationState>(json.ToString()) ?? ConversationState.Empty;
    }

    public Task SaveAsync(string userId, string conversationId, ConversationState state, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(state);
        // set + TTL slide — শেষ turn থেকে TtlHours বাঁচে।
        return redis.GetDatabase().StringSetAsync(
            Key(userId, conversationId), json, TimeSpan.FromHours(_settings.TtlHours));
    }
}
