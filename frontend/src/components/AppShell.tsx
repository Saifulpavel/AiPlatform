import { Outlet, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { LogOut, Sparkles } from 'lucide-react'
import { useAppDispatch } from '@/app/hooks'
import { logOut } from '@/features/auth/authSlice'
import { useLogoutMutation, useMeQuery } from '@/features/auth/authApi'
import { Button } from '@/components/ui/button'
import { ThemeToggle } from './ThemeToggle'

// protected app layout — header (user, theme, logout) + page outlet।
export function AppShell() {
  const { data: user } = useMeQuery()
  const [logout] = useLogoutMutation()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  const handleLogout = async () => {
    try {
      await logout().unwrap()
    } catch {
      /* logout best-effort */
    }
    dispatch(logOut())
    toast.success('লগআউট হয়েছে')
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="flex items-center justify-between border-b px-6 py-3">
        <div className="flex items-center gap-2 font-semibold">
          <Sparkles className="size-5 text-primary" /> AiPlatform
        </div>
        <div className="flex items-center gap-3">
          {user && <span className="hidden text-sm text-muted-foreground sm:inline">{user.fullName}</span>}
          <ThemeToggle />
          <Button variant="ghost" size="sm" onClick={handleLogout} className="gap-2">
            <LogOut className="size-4" /> লগআউট
          </Button>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
