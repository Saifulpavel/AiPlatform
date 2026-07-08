import { baseApi } from '@/features/api/baseApi'

export interface DocumentStatus {
  id: string
  fileName: string
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed'
  totalChunks: number
  processedChunks: number
  error: string | null
}

export const documentsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    uploadDocument: builder.mutation<{ documentId: string; status: string }, FormData>({
      query: (body) => ({ url: '/documents', method: 'POST', body }),
    }),
    documentStatus: builder.query<DocumentStatus, string>({
      query: (id) => `/documents/${id}`,
    }),
  }),
})

export const { useUploadDocumentMutation, useDocumentStatusQuery } = documentsApi
