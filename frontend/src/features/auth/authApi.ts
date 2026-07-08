import { baseApi } from '@/features/api/baseApi'

export interface LoginRequest {
  email: string
  password: string
}
export interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
}
export interface RegisterRequest {
  email: string
  password: string
  fullName: string
}
export interface CurrentUser {
  id: string
  email: string
  fullName: string
  roles: string[]
}

export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    login: builder.mutation<LoginResponse, LoginRequest>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
    }),
    register: builder.mutation<{ userId: string }, RegisterRequest>({
      query: (body) => ({ url: '/auth/register', method: 'POST', body }),
    }),
    me: builder.query<CurrentUser, void>({
      query: () => '/auth/me',
    }),
    logout: builder.mutation<void, void>({
      query: () => ({ url: '/auth/logout', method: 'POST' }),
    }),
  }),
})

export const { useLoginMutation, useRegisterMutation, useMeQuery, useLogoutMutation } = authApi
