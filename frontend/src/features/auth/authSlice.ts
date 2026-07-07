import { createSlice, type PayloadAction } from '@reduxjs/toolkit'

// access token শুধু memory-তে (Redux), localStorage-এ নয় → XSS-safe।
interface AuthState {
  accessToken: string | null
}

const initialState: AuthState = { accessToken: null }

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (state, action: PayloadAction<{ accessToken: string }>) => {
      state.accessToken = action.payload.accessToken
    },
    logOut: (state) => {
      state.accessToken = null
    },
  },
})

export const { setCredentials, logOut } = authSlice.actions
export default authSlice.reducer
