namespace AiPlatform.Application.Common.Interfaces;

// Local LLM runtime (Ollama) থেকে installed model-এর তালিকা।
// Impl Infrastructure-এ (Ollama /api/tags); ollama configure না থাকলে খালি তালিকা।
public interface ILocalModelCatalog
{
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken);
}
