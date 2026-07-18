using MediatR;

namespace AiPlatform.Application.Features.Llm.ListModels;

// Ollama-তে installed model-এর নাম তালিকা (frontend model-picker dropdown-এর জন্য)।
public record ListLocalModelsQuery : IRequest<IReadOnlyList<string>>;
