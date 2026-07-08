import { Routes, Route, Navigate } from 'react-router-dom'
import AuthPage from '@/features/auth/AuthPage'

// placeholder — Phase 4c এ protected app shell আসবে।
function Home() {
  return (
    <div className="flex min-h-screen items-center justify-center text-foreground">
      <p className="text-muted-foreground">লগইন সফল — app shell আসছে (Phase 4c)।</p>
    </div>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage initialMode="login" />} />
      <Route path="/register" element={<AuthPage initialMode="register" />} />
      <Route path="/" element={<Home />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  )
}
