import { Routes, Route, Navigate } from 'react-router-dom'
import { AppBootstrap } from '@/features/auth/AppBootstrap'
import { ProtectedRoute } from '@/routes/ProtectedRoute'
import { PublicRoute } from '@/routes/PublicRoute'
import { AppShell } from '@/components/AppShell'
import AuthPage from '@/features/auth/AuthPage'
import { HomePage } from '@/features/home/HomePage'
import { ChatPage } from '@/features/chat/ChatPage'
import { RagPage } from '@/features/rag/RagPage'
import { DocumentsPage } from '@/features/documents/DocumentsPage'

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
            <Route path="/chat" element={<ChatPage />} />
            <Route path="/rag" element={<RagPage />} />
            <Route path="/documents" element={<DocumentsPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppBootstrap>
  )
}
