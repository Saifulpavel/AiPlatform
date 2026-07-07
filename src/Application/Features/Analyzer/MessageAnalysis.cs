namespace AiPlatform.Application.Features.Analyzer;

// AI-এর JSON → typed object। sentiment: positive|neutral|negative, priority: low|medium|high।
public record MessageAnalysis(string Sentiment, string Category, string Priority, string Summary);
