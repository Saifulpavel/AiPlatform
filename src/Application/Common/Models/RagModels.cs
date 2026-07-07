namespace AiPlatform.Application.Common.Models;

// retrieval-এ পাওয়া এক টুকরো (citation-এর ভিত্তি)। Score optional (keyword = match count, vector = similarity)।
public record RetrievedChunk(string Text, double? Score);

// RAG উত্তর + কোন chunk থেকে (source/citation)।
public record RagResponse(string Answer, IReadOnlyList<RetrievedChunk> Sources);

// vector store-এ এক point (UUID id — seed ও uploaded doc আলাদা থাকে + vector + text payload)।
public record VectorPoint(string Id, float[] Vector, string Text);
