namespace AiPlatform.Application.Common.Models;

// retrieval-এ পাওয়া এক টুকরো (citation)। Source = কোথা থেকে (doc নাম / "knowledge-base")।
public record RetrievedChunk(string Text, double? Score, string? Source = null);

// RAG উত্তর + কোন chunk থেকে (source/citation)। answer-এ [1],[2] = Sources-এর ক্রম।
public record RagResponse(string Answer, IReadOnlyList<RetrievedChunk> Sources);

// vector store-এ এক point (UUID id + vector + text + কোন doc থেকে)।
public record VectorPoint(string Id, float[] Vector, string Text, string Source);
