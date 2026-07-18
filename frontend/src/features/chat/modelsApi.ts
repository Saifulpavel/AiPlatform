import { baseApi } from '@/features/api/baseApi'

// ollama-তে installed chat model তালিকা (GET /llm/models) — model-picker dropdown।
export const modelsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    localModels: builder.query<string[], void>({
      query: () => '/llm/models',
    }),
  }),
})

export const { useLocalModelsQuery } = modelsApi
