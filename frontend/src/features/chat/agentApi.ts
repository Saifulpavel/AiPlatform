import { baseApi } from '@/features/api/baseApi'

// Agent mode — LLM tool call করে (ERP query, knowledge base, current time)।
// stream করে না: tool চালানোর পর দ্বিতীয় turn লাগে, তাই এক-শটে পুরো উত্তর।
export interface AgentResponse {
  answer: string
  toolsUsed: string[]
  latencyMs: number
}
export interface AgentRequest {
  message: string
  provider?: string
}

export const agentApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    agent: builder.mutation<AgentResponse, AgentRequest>({
      query: (body) => ({ url: '/agent', method: 'POST', body }),
    }),
  }),
})

export const { useAgentMutation } = agentApi
