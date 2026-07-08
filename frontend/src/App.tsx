import { Routes, Route, Navigate } from 'react-router-dom'
import { AppBootstrap } from '@/features/auth/AppBootstrap'
import { ProtectedRoute } from '@/routes/ProtectedRoute'
import { PublicRoute } from '@/routes/PublicRoute'
import { AppShell } from '@/components/AppShell'
import AuthPage from '@/features/auth/AuthPage'
import { HomePage } from '@/features/home/HomePage'

export default function App() {
  return (
    <AppBootstrap>
      <Routes>
        {/* logged-in হলে এগুলো / তে redirect */}
        <Route element={<PublicRoute />}>
          <Route path="/login" element={<AuthPage initialMode="login" />} />
          <Route path="/register" element={<AuthPage initialMode="register" />} />
        </Route>

        {/* logged-in না হলে /login এ redirect */}
        <Route element={<ProtectedRoute />}>
          <Route element={<AppShell />}>
            <Route path="/" element={<HomePage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppBootstrap>
  )
}
