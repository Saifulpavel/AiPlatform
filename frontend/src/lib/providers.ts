// LLM provider list — এক জায়গায় (আগে ৪ ফাইলে ডুপ্লিকেট ছিল)।
// backend appsettings "Llm:Providers"-এর সাথে মিল; নতুন provider এখানে যোগ করলেই সব dropdown-এ আসে।
// ollama = local (Ollama, key ছাড়া) — hosted↔local swap একই UI-তে।
export const PROVIDERS = ['groq', 'gemini', 'mistral', 'ollama'] as const
