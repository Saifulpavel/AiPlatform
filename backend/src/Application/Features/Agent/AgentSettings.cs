namespace AiPlatform.Application.Features.Agent;

// appsettings "Agent"। feature-toggle + safety: tool-loop-এর সর্বোচ্চ ধাপ (infinite loop রোধ)।
public class AgentSettings
{
    public const string SectionName = "Agent";

    public bool Enabled { get; init; } = true;
    public int MaxIterations { get; init; } = 5;
}
