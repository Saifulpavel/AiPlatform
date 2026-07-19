import { baseApi } from '@/features/api/baseApi'

export interface RetrievedChunk {
  text: string
  score: number | null
  source: string | null
}
export interface RagResponse {
  answer: string
  sources: RetrievedChunk[]
}
export interface RagRequest {
  question: string
  method: string
  provider: string
  model?: string
}

export const ragApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    rag: builder.mutation<RagResponse, RagRequest>({
      query: (body) => ({ url: '/rag', method: 'POST', body }),
    }),
  }),
})

export const { useRagMutation } = ragApi
